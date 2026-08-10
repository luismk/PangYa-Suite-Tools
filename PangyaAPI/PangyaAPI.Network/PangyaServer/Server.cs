using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using PangyaAPI.Network.Repository;
using PangyaAPI.Network.Configuration;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Network.PangyaSession;
using PangyaAPI.Network.PangyaUnit;
using PangyaAPI.Network.PangyaUtil;
using PangyaAPI.Network.Hosting;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.BinaryModels;
using PangyaAPI.Utilities.Log;
using Microsoft.Extensions.Logging;
namespace PangyaAPI.Network.PangyaServer
{

    public abstract class Server : pangya_packet_handle, IServerRuntime
    {
        public virtual IReadOnlyList<ServerConsoleCommand> ConsoleCommands => Array.Empty<ServerConsoleCommand>();

        private IpDdosFilter _ipFilter;

        // Shutdown timer
        public PangyaSyncTimer m_shutdown;

        public ServerState m_state;
        //DECRYPT FIELDS

        private List<string> v_mac_ban_list;
        private List<IPBan> v_ip_ban_list;
        public SessionManager m_session_manager;
        public ServerInfoEx m_si = new ServerInfoEx();
        private int m_Bot_TTL; // Anti-bot Time-to-live
       // private bool m_chatDiscord;
        public bool _isRunning;
        protected readonly ServerConfiguration m_configuration;
        protected readonly ILogger m_logger;
        public List<TableMac> ListBlockMac { get; set; } = new List<TableMac>();
        public List<ServerInfo> m_server_list { get; set; } = new List<ServerInfo>();
        public IntPtr EventMoreAccept { get; private set; }

        public ServerInfoEx getInfo() => m_si;
        public uint getUID() => (uint)(m_si?.Id);
        public TcpListener _server;
        private CancellationTokenSource _lifetimeCancellation;
        private Task _acceptLoopTask;
        private Task _monitorTask;
        private readonly ConcurrentDictionary<int, Task> _connectionTasks = new();
        private int _nextConnectionTaskId;

        public abstract void OnStart();
        /// <summary>
        /// call methods
        /// </summary>
        public abstract void OnHeartBeat();
        /// <summary>
        /// check packet, packet is real
        /// </summary>
        /// <param name="session">client</param>
        /// <param name="_packet">packet read</param>
        /// <param name="opt">0 = server, 1 = client</param>
        /// <returns></returns>
        public abstract bool CheckPacket(Session session, packet _packet, int opt = 0);
        /// <summary>
        /// disconnect players !
        /// </summary>
        /// <param name="_session"></param>
        public abstract void onDisconnected(Session _session);

        /// <summary>
        /// Send Key
        /// </summary>
        /// <param name="_session"></param>
        protected abstract void onAcceptCompleted(Session _session);


        public Server(SessionManager manager, ServerConfiguration configuration, ILoggerFactory loggerFactory)
        {
            try
            {
                m_session_manager = manager;
                m_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
                m_logger = (loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory))).CreateLogger(GetType().FullName ?? GetType().Name);

                ConsoleEx.Log(GetType().Name);

                m_state = ServerState.Uninitialized;

                _ipFilter = new IpDdosFilter(configuration.AntiDdos);
            }
            catch (exception e)
            {
                m_logger.Write("[Server::construtor][Error] " + e.getFullMessageError(), LogDestination.Console);
            }
        }



        public virtual void config_init()
        {
            try
            {
                var options = m_configuration.Server;
                m_si = new ServerInfoEx
                {
                    version = options.Version,
                    version_client = options.ClientVersion,
                    Name = options.Name,
                    Id = options.Uid,
                    Port = options.Port,
                    IpAddress = options.Address,
                    MaxUsers = options.MaxUsers,
                    propriedade = (PropertyType)options.Property,
                    rate = new RateConfigInfo(),
                    flagEvent = EventType.NONE,
                    flag = new uFlag(0)
                };
            }
            catch (exception e)
            {
                m_logger.Write("[Server::config_init][Error] " + e.getFullMessageError(), LogDestination.Console);
            }

            try
            {
                m_Bot_TTL = m_configuration.Options.AntiBotTtl;
                m_si.packet_version = m_configuration.Server.PacketVersion;
            }
            catch (exception e)
            {
                m_logger.Write("[Server::config_init][Error] " + e.getFullMessageError(), LogDestination.Console);
                m_Bot_TTL = 1000; // Usa o valor padrão do anti bot TTL
            }
        }

        /// <summary>
        /// Manuseia Comunicação do Cliente
        /// </summary>
        private async Task AcceptCompletedAsync(TcpClient client, CancellationToken cancellationToken)
        {
            Session _session;
            lock (_sessionsLock)
            {
                // Add player
                _session = m_session_manager.AddSession(this, client, client.Client.RemoteEndPoint as IPEndPoint, (byte)(new Random().Next() % 16));

                m_logger.Write($"[Server::accept_completed][Warning] New Player Connected [IP: {_session.getIP()}, Key: {_session.m_key}]", LogDestination.GeneralFile | LogDestination.Console);

                //time out packet
                _session.m_sock.ReceiveTimeout = 5000;
            }

            onAcceptCompleted(_session);

            while (_session.isConnected())
            {
                try
                {
                    if (!_session.isConnected())
                    {
                        DisconnectSession(_session);
                        break;
                    }

                    // Processa o pacote recebido
                    if (!await recv_server_new_async(_session, cancellationToken).ConfigureAwait(false))
                    {

                        DisconnectSession(_session);
                        break;
                    }
                    _session.LastPacketReceived = DateTime.Now;
                    await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    DisconnectSession(_session);
                    break;
                }
                catch (IOException ioEx)
                {
                    m_logger.Write("[Server::Handle_session][IOError] " + ioEx.Message, LogDestination.GeneralFile | LogDestination.Console);
                    DisconnectSession(_session);
                    break;
                }
                catch (exception ex)
                {
                    m_logger.Write("[Server::Handle_session][ErrorSystem] " + ex.getFullMessageError(), LogDestination.GeneralFile | LogDestination.Console);
                    DisconnectSession(_session);
                    break;
                }
            }
        }

        protected async Task OnMonitorAsync(CancellationToken cancellationToken)
        {
            m_logger.Write("[Server::onMonitor][Info] monitor iniciado com sucesso!", LogDestination.GeneralFile);
            await Task.Yield();

            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    try
                    {
                        // Atualiza o número de sessões conectadas
                        m_si.ConnectedUsers = (int)m_session_manager.NumSessionConnected();
                        snmdb.NormalManagerDB.getInstance().add(0, new CmdRegisterServer(m_si), SQLDBResponse, this);
                    }
                    catch (exception e) // Exceção específica da aplicação
                    {
                        m_logger.Write(                             $"[Server::Monitor][ErrorSystem] {e.GetType().Name}: {e.getFullMessageError()}\nStack Trace: {e.getStackTrace()}", LogDestination.GeneralFile | LogDestination.Console);
                    }
                    // Atualiza o título da janela do console conforme o tipo do servidor
                    switch (m_si.tipo)
                    {
                        case 0:
                            Console.Title = $"Login Server - P: {m_si.ConnectedUsers}";
                            break;
                        case 1:
                            Console.Title = $"Game Server - P: {m_si.ConnectedUsers}";
                            break;
                        case 2:
                            Console.Title = $"Bird Server - P: {m_si.ConnectedUsers}";
                            break;
                        case 3:
                            Console.Title = $"Messenger Server - P: {m_si.ConnectedUsers}";
                            break;
                        case 4:
                            Console.Title = $"Rank Server - P: {m_si.ConnectedUsers}";
                            break;
                        case 5:
                            Console.Title = $"Auth Server - P: {m_si.ConnectedUsers}";
                            break;
                        case 6:
                            Console.Title = $"GG Auth Server - P: {m_si.ConnectedUsers}";
                            break;
                        default:
                            Console.Title = $"Unknown Server - P: {m_si.ConnectedUsers}";
                            break;
                    }
                    // pega a lista de servidores online
                    cmdUpdateServerList();
                    // Atualiza a lista de bloqueios de IP/MAC
                    cmdUpdateListBlock_IP_MAC();
                    // Evento de heartbeat
                    OnHeartBeat();

                }
                catch (exception e) // Exceção específica da aplicação
                {
                    m_logger.Write(                         $"[Server::Monitor][ErrorSystem] {e.GetType().Name}: {e.getFullMessageError()}\nStack Trace: {e.getStackTrace()}", LogDestination.GeneralFile | LogDestination.Console);
                }
                catch (Exception ex) // Exceções gerais do .NET
                {
                    m_logger.Write(                         $"[Server::Monitor][ErrorSystem] {ex.GetType().Name}: {ex.Message}\nStack Trace: {ex.StackTrace}", LogDestination.GeneralFile | LogDestination.Console);
                }

                await Task.Delay(TimeSpan.FromMilliseconds(2100), cancellationToken).ConfigureAwait(false);
            }
        }

        protected void cmdUpdateServerList()
        {
            snmdb.NormalManagerDB.getInstance().add(1, new CmdServerList(TYPE_SERVER.GAME), SQLDBResponse, this);
        }

        protected void cmdUpdateListBlock_IP_MAC()
        {
            // List de IP Address Ban
            var cmd_lib = new CmdListIpBan();     // Waiter

            snmdb.NormalManagerDB.getInstance().add(0, cmd_lib, null, null);

            if (cmd_lib.getException().getCodeError() != 0)
                throw cmd_lib.getException();

            v_ip_ban_list = cmd_lib.getListIPBan();

            // List de Mac Address Ban
            var cmd_lmb = new CmdListMacBan();    // Waiter

            snmdb.NormalManagerDB.getInstance().add(0, cmd_lmb, null, null);

            if (cmd_lmb.getException().getCodeError() != 0)
                throw cmd_lmb.getException();

            v_mac_ban_list = cmd_lmb.getList();
        }


        public override void dispach_packet_sv_same_thread(Session session, packet _packet)
        {
            if (session == null || session.isConnected() == false || _packet == null)
            {
                return;//nao esta mais conectado!
            }

            func_arr.func_arr_ex func = null;

            try
            {
                // Obtém a função correspondente ao tipo de pacote
                func = packet_func_base.funcs_sv.getPacketCall(_packet.getTipo());
            }
            catch (exception e)
            {
                m_logger.Write($"[Server::dispach_packet_sv_same_thread][ErrorSystem] {e.Message}, {e.getStackTrace()}", LogDestination.GeneralFile | LogDestination.Console);
                // Desconecta a sessão
                DisconnectSession(session);
            }

            try
            {
                // Atualiza o tick do cliente
                session.m_tick = Environment.TickCount;

                var pd = new ParamDispatch
                {
                    _session = session,
                    _packet = _packet
                };

                if (CheckPacket(session, _packet))
                {
                    try
                    {
                        if (func != null && func.ExecCmd(pd) != 0)
                        {
                            m_logger.Write($"[Server::dispach_packet_sv_same_thread][Error][MY] Ao tratar o pacote. ID: {_packet.getTipo()}(0x{_packet.getTipo():X})," + pd._packet.Log(), LogDestination.GeneralFile | LogDestination.Console);
                            //DisconnectSession(session);
                        }
                    }

                    catch (exception e)
                    {
                        m_logger.Write($"[Server::dispach_packet_sv_same_thread][Error][MY] {e.getFullMessageError()}", LogDestination.GeneralFile | LogDestination.Console);

                        // DisconnectSession(session);
                    }
                }
            }
            catch (exception e)
            {
                m_logger.Write($"[Server::dispach_packet_sv_same_thread][Error][MY] {e.Message}", LogDestination.GeneralFile | LogDestination.Console);

                // DisconnectSession(session);
            }
        }

        protected override void dispach_packet_same_thread(Session session, packet _packet)
        {
            if (session == null || session.isConnected() == false || _packet == null)
            {
                return;//nao esta mais conectado!
            }

            func_arr.func_arr_ex func = null;

            try
            {
                // Obtém a função correspondente ao tipo de pacote
                func = packet_func_base.funcs.getPacketCall(_packet.getTipo());
            }
            catch (exception e)
            {
                m_logger.Write($"[Server::dispach_packet_same_thread][ErrorSystem] {e.Message}, {e.getStackTrace()}", LogDestination.GeneralFile | LogDestination.Console);
                // Desconecta a sessão
                session.m_sock.Client.Shutdown(how: SocketShutdown.Both);
            }

            try
            {
                // Atualiza o tick do cliente
                session.m_tick = Environment.TickCount;

                var pd = new ParamDispatch
                {
                    _session = session,
                    _packet = _packet
                };

                if (CheckPacket(session, _packet, 1))
                {
                    try
                    {
                        if (func != null && func.ExecCmd(pd) != 0)
                        {
                            m_logger.Write($"[Server::dispach_packet_same_thread][Error][MY] Ao tratar o pacote. ID: {_packet.getTipo()}(0x{_packet.getTipo():X})," + pd._packet.Log(), LogDestination.GeneralFile | LogDestination.Console);
                            //block ip now
                            //snmdb.NormalManagerDB.getInstance().add(0, new CmdInsertBlockIp(session.getIP(), "255.255.255.255"), SQLDBResponse, this);

                            //session.m_sock.Client.Shutdown(how: SocketShutdown.Both);
                        }
                    }

                    catch (exception e)
                    {
                        m_logger.Write($"[Server::dispach_packet_same_thread][Error][MY] {e.getFullMessageError()}", LogDestination.GeneralFile | LogDestination.Console);
                        session.m_sock.Client.Shutdown(how: SocketShutdown.Both);
                    }
                }
            }
            catch (exception e)
            {
                m_logger.Write($"[Server::dispach_packet_same_thread][Error][MY] {e.Message}", LogDestination.GeneralFile | LogDestination.Console);

                session.m_sock.Client.Shutdown(how: SocketShutdown.Both);
            }
        }



        // Shutdown With Time
        public virtual void shutdown_time(int timeSec)
        {
        }

        public void shutdown()
        {
            Stop();
        }

        public void end_time_shutdown(object _arg1, object _arg2)
        {

            var s = (Server)(_arg1);
            int time_sec = (int)_arg2;

            try
            {

                s.shutdown_time(time_sec);

            }
            catch (exception e)
            {

                m_logger.Write("[Server::end_time_shutdown][ErrorSystem] " + e.getFullMessageError(), LogDestination.GeneralFile | LogDestination.Console);
            }
        }

        public void Start()
            => StartAsync(CancellationToken.None).GetAwaiter().GetResult();



        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_isRunning)
                return;

            _sessionsLock = new object();
            try
            {
                _server = new TcpListener(IPAddress.Any, m_si.Port);
                m_state = ServerState.Good;

                if (m_state != ServerState.Failure)
                {

                    try
                    {
                        _server.Start(m_si.MaxUsers);

                        m_logger.Write("[Server::Start][Sucess] Running in Port: " + m_si.Port, LogDestination.GeneralFile | LogDestination.Console);


                        
                        // Inicializa o Unit_Connect, que conecta com o Auth Server
                        m_unit_connect = new unit_auth_server_connect(this, m_configuration.AuthServer);//interno

                        _isRunning = true;
                        _lifetimeCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                        _acceptLoopTask = AcceptLoopAsync(_lifetimeCancellation.Token);
                        _monitorTask = OnMonitorAsync(_lifetimeCancellation.Token);

                        // On Start
                        OnStart();

                        // Start Unit Connect for Try Connection with Auth Server
                        if (m_unit_connect != null)
                            await m_unit_connect.StartAsync(_lifetimeCancellation.Token).ConfigureAwait(false);
                    }
                    catch (exception e)
                    {
                        m_logger.Write(e.getFullMessageError(), LogDestination.GeneralFile | LogDestination.Console);
                    }

                }
                else
                {
                    m_logger.Write("[Server::start][Error] Server Inicializado com falha, fechando o Server::", LogDestination.GeneralFile | LogDestination.Console);
                }
            }
            catch (exception e)
            {
                m_logger.Write(e.getFullMessageError(), LogDestination.GeneralFile | LogDestination.Console);
            }
        }

        private async Task AcceptLoopAsync(CancellationToken cancellationToken)
        {
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                TcpClient newClient = null;
                try
                {
                    newClient = await _server.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);

                    var remoteEndPoint = newClient.Client.RemoteEndPoint as IPEndPoint;
                    string ipAddress = remoteEndPoint?.Address.ToString();

                    if (_ipFilter != null && _ipFilter.IsBlocked(ipAddress) && haveBanList(ipAddress, "", false))
                    {
                        newClient.Close();
                        m_logger.Write($"[Server] Conexão de IP bloqueado: {ipAddress}", LogDestination.GeneralFile | LogDestination.Console);
                        continue;
                    }

                    _ipFilter?.OnConnect(ipAddress);

                    int taskId = Interlocked.Increment(ref _nextConnectionTaskId);
                    var connectionTask = RunClientAsync(taskId, newClient, cancellationToken);
                    _connectionTasks[taskId] = connectionTask;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    newClient?.Dispose();
                    break;
                }
                catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested || !_isRunning)
                {
                    newClient?.Dispose();
                    break;
                }
                catch (Exception e)
                {
                    m_logger.Write($"[Server::AcceptLoopAsync][ErrorSystem] {e.Message}", LogDestination.GeneralFile | LogDestination.Console);
                    newClient?.Dispose();
                }
            }
        }

        private async Task RunClientAsync(int taskId, TcpClient client, CancellationToken cancellationToken)
        {
            try
            {
                await AcceptCompletedAsync(client, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                m_logger.Write($"[Server::RunClientAsync][ErrorSystem] {exception.Message}", LogDestination.GeneralFile | LogDestination.Console);
            }
            finally
            {
                client.Dispose();
                _connectionTasks.TryRemove(taskId, out _);
            }
        }

        public void Stop()
            => StopAsync(CancellationToken.None).GetAwaiter().GetResult();

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (!_isRunning && m_state == ServerState.Failure)
                return;

            _isRunning = false;
            m_state = ServerState.Failure;
            _lifetimeCancellation?.Cancel();
            _server?.Stop();
            m_session_manager?.Clear();

            if (m_unit_connect != null)
                await m_unit_connect.StopAsync(cancellationToken).ConfigureAwait(false);

            var backgroundTasks = new List<Task>();
            if (_acceptLoopTask != null)
                backgroundTasks.Add(_acceptLoopTask);
            if (_monitorTask != null)
                backgroundTasks.Add(_monitorTask);
            backgroundTasks.AddRange(_connectionTasks.Values);

            try
            {
                await Task.WhenAll(backgroundTasks).WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_lifetimeCancellation?.IsCancellationRequested == true || cancellationToken.IsCancellationRequested)
            {
                // Expected during shutdown.
            }
            catch (Exception exception)
            {
                m_logger.Write($"[Server::StopAsync][ErrorSystem] {exception}", LogDestination.GeneralFile | LogDestination.Console);
            }

            _connectionTasks.Clear();
            _acceptLoopTask = null;
            _monitorTask = null;
            _lifetimeCancellation?.Dispose();
            _lifetimeCancellation = null;
            Console.WriteLine("Server is stopping...");
        }


        public virtual Session HasLoggedWithOuterSocket(Session _session)
        {
            var s = m_session_manager.FindAllSessionByUid(_session.getUID());
            foreach (var el in s)
            {
                if (el.m_oid != _session.m_oid && el.isConnected())
                    return el;
            }

            return null;
        }

        protected virtual void init_option_accepted_socket(in Socket _accepted)
        {
            bool tcp_nodelay = true;

            // ---------- DESEMPENHO COM OS SOCKOPT -----------  
            // COM NO_TCPDELAY                 AVG(MEDIA) 0.552
            // COM SO_SNDBUF 0                AVG(MEDIA) 0.560
            // COM SO_RCVBUF 0                AVG(MEDIA) 0.570
            // COM NO_TCPDELAY e SO_SNDBUF 0  AVG(MEDIA) 0.569
            // COM NO_TCPDELAY e SO_RCVBUF 0  AVG(MEDIA) 0.566
            // SEM NENHUM SOCKOPT             AVG(MEDIA) 0.569
            // Não tem muita diferença, vou deixar só o NO_TCPDELAY mesmo

            try
            {
                // Ativa TCP_NODELAY (desabilita Nagle)
                _accepted.NoDelay = tcp_nodelay;
            }
            catch (SocketException ex)
            {
                throw new Exception("[Server::init_option_accepted_socket][Error] não conseguiu desabilitar tcp delay (nagle algorithm).", ex);
            }

            try
            {
                // KEEPALIVE: habilita + configura tempo
                byte[] keepAlive = new byte[12];
                BitConverter.GetBytes((uint)1).CopyTo(keepAlive, 0);     // onoff
                BitConverter.GetBytes((uint)10000).CopyTo(keepAlive, 4); // keepalivetime (10s)
                BitConverter.GetBytes((uint)1000).CopyTo(keepAlive, 8);  // keepaliveinterval (1s)

                _accepted.IOControl(IOControlCode.KeepAliveValues, keepAlive, null);

                //_smp.legacy logging queue.getInstance().push(new legacy_log_entry(
                //    $"[Server::init_option_accepted_socket][Info] socket[ID={_accepted.Handle}] KEEPALIVE[ONOFF=1, TIME=20000, INTERVAL=2000] foi ativado para esse",
                //    legacy_log_destination.CL_FILE_LOG_AND_CONSOLE
                //));
            }
            catch (SocketException ex)
            {
                throw new Exception("[Server::init_option_accepted_socket][Error] não conseguiu setar o socket option KEEPALIVE.", ex);
            }
        }

        public bool haveBanList(string _ip_address, string _mac_address, bool _check_mac = true)
        {
            if (_check_mac)
            {
                // Verifica primeiro se o MAC Address foi bloqueado

                // Cliente não enviou um MAC Address válido, bloquea essa conexão que é hacker que mudou o ProjectG
                if (string.IsNullOrEmpty(_mac_address))
                    return true;    // Cliente não enviou um MAC Address válido, bloquea essa conexão que é hacker que mudou o ProjectG

                foreach (var el in v_mac_ban_list)
                {
                    if (!string.IsNullOrEmpty(el) && string.Compare(el, _mac_address, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        return true;
                    }
                }
            }
            // IP Address inválido, bloquea essa conexão que é Hacker ou Bug
            if (string.IsNullOrEmpty(_ip_address))
            {
                return true;
            }
            uint ip = 0;
            if (IPAddress.TryParse(_ip_address, out IPAddress ipAddress))
            {
                byte[] ipBytes = ipAddress.GetAddressBytes();
                ip = BitConverter.ToUInt32(ipBytes, 0);
                ip = (uint)IPAddress.NetworkToHostOrder((int)ip);
            }
            foreach (IPBan el in v_ip_ban_list)
            {
                if (el.type == IPBan._TYPE.IP_BLOCK_NORMAL)
                {
                    if ((ip & el.mask) == (el.ip & el.mask))
                    {
                        return true;
                    }
                }
                else if (el.type == IPBan._TYPE.IP_BLOCK_RANGE)
                {
                    if (el.ip <= ip && ip <= el.mask)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void Shutdown(int timeSec)
        {
            Console.WriteLine("Shutting down Server::..");
            Stop();
        }

        public virtual uint GetUID()
        {
            return (uint)m_si.Id;
        }
        protected void _disconnect_session()
        {
            try
            {
                if (m_session_manager.IsInit())
                {

                    var s = m_session_manager.GetSessionToDelete(1000/*1 second para a liberar o while se não tiver sessions para disconectar*/);

                    if (s != null)
                        DisconnectSession(s);

                }
                else
                    Thread.Sleep(300/*espera 300 miliseconds até o session_manager ser inicializado*/);

            }
            catch (exception e)
            {
                m_logger.Write("[Server::disconnect_session][ErrorSystem] " + e.getFullMessageError(), LogDestination.GeneralFile | LogDestination.Console);
            }
        }

        public virtual List<Session> FindAllGM()
        {
            return m_session_manager.findAllGM();
        }

        public virtual Session FindSessionByOid(uint oid)
        {
            return m_session_manager.FindSessionByOid(oid);
        }

        public virtual Session FindSessionByUid(uint uid)
        {
            return m_session_manager.findSessionByUID(uid);
        }

        public virtual List<Session> FindAllSessionByUid(uint uid)
        {
            return m_session_manager.FindAllSessionByUid(uid);
        }

        public virtual Session FindSessionByNickname(string nickname)
        {
            return m_session_manager.FindSessionByNickname(nickname);
        }

        public override bool DisconnectSession(Session _session)
        {
            if (_session == null)
            {
                Console.WriteLine("[Server::DisconnectSession][Warning] Tentativa de desconectar uma sessão nula.");
                return false;
            }

            m_logger.Write($"[Server::DisconnectSession][Warning] PLAYER[IP: {_session.getIP()}, Key: {_session.m_key}, Time: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}]", LogDestination.GeneralFile);

            // Notifica que a desconexão ocorreu       
            onDisconnected(_session);
            bool result;
            try
            {
                _ipFilter?.OnDisconnect(_session.getIP());

                // Remove a sessão do gerenciador        
                result = m_session_manager.DeleteSession(_session);

            }
            catch (Exception ex)
            {
                result = false;
                Console.WriteLine($"[Server::DisconnectSession][Error] Erro ao deletar sessão: {ex.Message}");
            }
            return result;
        }

        public void SQLDBResponse(int _msg_id, Pangya_DB _pangya_db, object _arg)
        {
            if (_arg == null)
            {
                m_logger.Write("[Server::SQLDBResponse][Warning] _arg is null, na msg_id = " + _msg_id, LogDestination.GeneralFile | LogDestination.Console);
                return;
            }
            switch (_msg_id)
            {
                case 1:
                    {
                        m_server_list = ((CmdServerList)_pangya_db).getServerList();
                    }
                    break;
                default:
                    break;
            }
        }


        public abstract bool CheckCommand(Queue<string> _command);

        public int getBotTTL() => m_Bot_TTL;

        public unit_auth_server_connect m_unit_connect;        // Ponteiro Connecta com o Auth Server                  
        private object _sessionsLock;


        //sao do unit
        public override void authCmdInfoPlayerOnline(uint _req_server_uid, uint _player_uid)
        {
            try
            {

                var s = m_session_manager.findSessionByUID(_player_uid);

                if (s != null)
                {
                    var aspi = new AuthServerPlayerInfo(s.getUID(), s.getID(), s.getIP());

                    // UPDATE ON Auth Server
                    m_unit_connect.sendInfoPlayerOnline(_req_server_uid, aspi);

                }
                else
                {
                    // UPDATE ON Auth Server
                    m_unit_connect.sendInfoPlayerOnline(_req_server_uid, new AuthServerPlayerInfo(_player_uid));
                }

            }
            catch (exception e)
            {

                // UPDATE ON Auth Server - Error reply
                m_unit_connect.sendInfoPlayerOnline(_req_server_uid, new AuthServerPlayerInfo(_player_uid));

                m_logger.Write("[Server::authCmdInfoPlayerOnline][ErrorSystem] " + e.getFullMessageError(), LogDestination.GeneralFile | LogDestination.Console);
            }
        }

        public override void authCmdSendCommandToOtherServer(packet _packet)
        {

            try
            {

                func_arr.func_arr_ex func = null;

                uint req_server_uid = _packet.ReadUInt32();
                var command_id = _packet.ReadInt16();

                try
                {

                    func = packet_func_base.funcs_as.getPacketCall(command_id);

                    if (func != null && func.ExecCmd(new ParamDispatch(m_unit_connect.m_session, _packet)) == 1)
                        throw new exception("[Server::authCmdSendCommandToOtherServer][Error] Ao tratar o Comando. ID: " + (command_id)
                                + "(0x" + (command_id) + ").", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER, 5000, 0));

                }
                catch (exception e)
                {

                    if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.FUNC_ARR/*packet_func Erro, Warning e etc*/)
                    {

                        m_logger.Write("[Server::authCmdSendCommandToOtherServer][ErrorSystem] " + e.getFullMessageError(), LogDestination.GeneralFile | LogDestination.Console);

                    }
                    else
                        throw;
                }

            }
            catch (exception e)
            {

                m_logger.Write("[Server::authCmdSendCommandToOtherServer][ErrorSystem] " + e.getFullMessageError(), LogDestination.GeneralFile | LogDestination.Console);
            }
        }

        public override void authCmdSendReplyToOtherServer(packet _packet)
        {
            try
            {

                func_arr.func_arr_ex func = null;

                uint req_server_uid = _packet.ReadUInt32();
                var command_id = _packet.ReadInt16();

                try
                {

                    func = packet_func_base.funcs_as.getPacketCall(command_id);

                    if (func != null && func.ExecCmd(new ParamDispatch(m_unit_connect.m_session, _packet)) == 1)
                    {
                        throw new exception("[Server::authCmdSendReplyToOtherServer][Error] Ao tratar o Comando. ID: " + Convert.ToString(command_id) + "(0x" + (command_id) + ").", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.GAME_SERVER,
                            5001, 0));
                    }
                }
                catch (exception e)
                {

                    if (ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.FUNC_ARR/*packet_func Erro, Warning e etc*/)
                    {

                        m_logger.Write("[Server::authCmdSendCommandToOtherServer][ErrorSystem] " + e.getFullMessageError(), LogDestination.GeneralFile | LogDestination.Console);

                    }
                    else
                        throw;
                }

            }
            catch (exception e)
            {

                m_logger.Write("[Server::authCmdSendCommandToOtherServer][ErrorSystem] " + e.getFullMessageError(), LogDestination.GeneralFile | LogDestination.Console);
            }
        }

        public override void sendCommandToOtherServerWithAuthServer(PangyaBinaryWriter _packet, uint _send_server_uid_or_type)
        {
            try
            {

                // Envia o comando para o outro server com o Auth Server
                m_unit_connect.sendCommandToOtherServer(_send_server_uid_or_type, new packet(_packet.GetBytes));

            }
            catch (exception e)
            {

                m_logger.Write("[Server::sendCommandToOtherServerWithAuthServer][ErrorSystem] " + e.getFullMessageError(), LogDestination.GeneralFile | LogDestination.Console);
            }
        }

        public override void sendReplyToOtherServerWithAuthServer(PangyaBinaryWriter _packet, uint _send_server_uid_or_type)
        {
            try
            {

                // Envia a resposta para o outro server com o Auth Server
                m_unit_connect.sendReplyToOtherServer(_send_server_uid_or_type, new packet(_packet.GetBytes));

            }
            catch (exception e)
            {

                m_logger.Write("[Server::sendReplyToOtherServerWithAuthServer][ErrorSystem] " + e.getFullMessageError(), LogDestination.GeneralFile | LogDestination.Console);
            }
        }

    }

    // Server Static
    //namespace ssv
    //{
    //    public abstract partial class sv : Singleton<Server>
    //    {
    //    }
    //}
}
