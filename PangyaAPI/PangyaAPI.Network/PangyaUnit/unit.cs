using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using PangyaAPI.Network.Repository;
using PangyaAPI.Network.Configuration;
using PangyaAPI.Network.Cryptor;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Network.PangyaServer;
using PangyaAPI.Network.PangyaSession;
using PangyaAPI.Network.PangyaUtil;
using PangyaAPI.Network.Hosting;
using PangyaAPI.SQL;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using Microsoft.Extensions.Logging;
namespace PangyaAPI.Network.PangyaUnit
{
    /// <summary>
    /// Server Auth ;)
    /// </summary>
    public abstract class unit : pangya_packet_handle, IServerRuntime
    {
        public virtual IReadOnlyList<ServerConsoleCommand> ConsoleCommands => Array.Empty<ServerConsoleCommand>();

        #region Fields


        public ServerState m_state;
        //DECRYPT FIELDS 
        private List<string> v_mac_ban_list;
        private List<IPBan> v_ip_ban_list;
        public SessionManager m_session_manager;
        public ServerInfoEx m_si = new ServerInfoEx();
        private int m_Bot_TTL; // Anti-bot Time-to-live
        //private bool m_chatDiscord;
        public bool _isRunning => m_state == ServerState.Initialized;
        protected readonly ServerConfiguration m_configuration;
        protected readonly ILogger m_logger;
        public ServerInfoEx getInfo() => m_si;
        public TcpListener _server;
        public List<ServerInfo> m_server_list { get; set; }
        private CancellationTokenSource _lifetimeCancellation;
        private Task _acceptLoopTask;
        private Task _monitorTask;
        private readonly ConcurrentDictionary<int, Task> _connectionTasks = new();
        private int _nextConnectionTaskId;
        #endregion

        #region Abstract Methods
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

        #endregion

        #region Constructor
        public unit(SessionManager manager, ServerConfiguration configuration, ILoggerFactory loggerFactory)
        {
            try
            {
                m_session_manager = manager;
                m_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
                m_logger = (loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory))).CreateLogger(GetType().FullName ?? GetType().Name);

                ConsoleEx.Log(GetType().Name);

                m_state = ServerState.Uninitialized;
                 
            }
            catch (exception e)
            {
                m_logger.Write("[unit::construtor][Error] " + e.getFullMessageError(), LogDestination.Console);
            }
        }


        #endregion

        #region Private Methods    

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
                m_logger.Write("[unit::config_init][Error] " + e.getFullMessageError(), LogDestination.Console);
            }

            try
            {
                m_Bot_TTL = m_configuration.Options.AntiBotTtl;
                m_si.packet_version = m_configuration.Server.PacketVersion;
            }
            catch (exception e)
            {
                m_logger.Write("[unit::config_init][Error] " + e.getFullMessageError(), LogDestination.Console);
                m_Bot_TTL = 1000; // Usa o valor padrão do anti bot TTL
            }
        }

        /// <summary>
        /// Aguarda Conexões
        /// </summary>
        private async Task HandleWaitConnectionsAsync(CancellationToken cancellationToken)
        {
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var newClient = await _server.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                    var remoteEndPoint = newClient.Client.RemoteEndPoint as IPEndPoint;
                    string ipAddress = remoteEndPoint?.Address.ToString();

                    init_option_accepted_socket(newClient.Client);

                    int taskId = Interlocked.Increment(ref _nextConnectionTaskId);
                    var connectionTask = RunClientAsync(taskId, newClient, cancellationToken);
                    _connectionTasks[taskId] = connectionTask;

                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested || !_isRunning)
                {
                    break;
                }
                catch (exception e) // Exceção específica da aplicação
                {
                    m_logger.Write(                         $"[Server.HandleWaitConnections][ErrorSystem] {e.getFullMessageError()}", LogDestination.GeneralFile | LogDestination.Console);
                }
            }
        }

        /// <summary>
        /// Manuseia Comunicação do Cliente
        /// </summary>
        private async Task AcceptCompletedAsync(TcpClient client, CancellationToken cancellationToken)
        {
            //add player
            var _session = m_session_manager.AddSession(this, client, client.Client.RemoteEndPoint as IPEndPoint, (byte)(new Random().Next() % 16));

            //send key
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

                    if (await ReceiveAsync(_session, cancellationToken).ConfigureAwait(false))
                    {
                    }

                    else
                    {
                        DisconnectSession(_session);
                        break;
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    DisconnectSession(_session);
                    break;
                }
                catch (IOException ioEx)
                {
                    m_logger.Write("[unit::Handle_session][IOError] " + ioEx.Message, LogDestination.GeneralFile | LogDestination.Console);
                    DisconnectSession(_session);
                    break;
                }
                catch (exception ex)
                {
                    m_logger.Write("[unit::Handle_session][ErrorSystem] " + ex.getFullMessageError(), LogDestination.GeneralFile | LogDestination.Console);
                    DisconnectSession(_session);
                    break;
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
                m_logger.Write($"[unit::RunClientAsync][ErrorSystem] {exception.Message}", LogDestination.GeneralFile | LogDestination.Console);
            }
            finally
            {
                client.Dispose();
                _connectionTasks.TryRemove(taskId, out _);
            }
        }

        protected async Task<bool> ReceiveAsync(Session _session, CancellationToken cancellationToken)
        {
            try
            {
                if (!_session.isConnected() || !_session.m_sock.Connected)
                    return false;//falso pq deu errado

                var result = await _session.m_sock.ReadAsync(cancellationToken).ConfigureAwait(false);


                if (result.check)
                {
                    if (_session.isCreated() && result.len >= 5)
                    {
                        var decryptedPacket = new ToClientBuffer().getPackets(result._buffer, _session.m_key);
                        if (decryptedPacket.Count > 0)
                        {
                            foreach (var packet in decryptedPacket)
                            {
                                Debug.WriteLine("[pangya_packet_handle::recv_new] [Log] " + packet.Id);
                                dispach_packet_same_thread(_session, packet);//ler e cuida com packets                         {
                            }
                        }

                        return true; //true se caso deu certo
                    }
                    else
                    {
                        Debug.WriteLine("[pangya_packet_handle::recv_new] [Log] " + result.len);
                        return false;//falso pq deu errado
                    }
                }

            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (SocketException se)
            {
                Debug.WriteLine("[pangya_packet_handle::recv_new] SocketException: " + se.Message);
                DisconnectSession(_session);
            }
            catch (ObjectDisposedException ode)
            {
                Debug.WriteLine("[pangya_packet_handle::recv_new] Socket fechado: " + ode.Message);
                DisconnectSession(_session);
            }
            catch (Exception e)
            {
                Debug.WriteLine("[pangya_packet_handle::recv_new] Exception: " + e.Message);
                DisconnectSession(_session);
            }
            return false;//falso pq deu errado
        }

        protected async Task OnMonitorAsync(CancellationToken cancellationToken)
        {
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
                        m_logger.Write(                             $"[Server.Monitor][ErrorSystem] {e.GetType().Name}: {e.getFullMessageError()}\nStack Trace: {e.getStackTrace()}", LogDestination.GeneralFile | LogDestination.Console);
                    }
                    // Atualiza o título da janela do console conforme o tipo do servidor
                    Console.Title = $"Auth Server - P: {m_si.ConnectedUsers}";

                    // Atualiza a lista de servidores online e bloqueios de IP/MAC
                    cmdUpdateServerList();
                    //cmdUpdateListBlock_IP_MAC();

                    // Evento de heartbeat
                    OnHeartBeat();

                }
                catch (exception e) // Exceção específica da aplicação
                {
                    m_logger.Write(                         $"[Server.Monitor][ErrorSystem] {e.GetType().Name}: {e.getFullMessageError()}\nStack Trace: {e.getStackTrace()}", LogDestination.GeneralFile | LogDestination.Console);
                }
                catch (Exception ex) // Exceções gerais do .NET
                {
                    m_logger.Write(                         $"[Server.Monitor][ErrorSystem] {ex.GetType().Name}: {ex.Message}\nStack Trace: {ex.StackTrace}", LogDestination.GeneralFile | LogDestination.Console);
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
                m_logger.Write($"[Server.DispatchpacketSameThread][ErrorSystem] {e.Message}, {e.getStackTrace()}", LogDestination.GeneralFile | LogDestination.Console);
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
                            //_smp.legacy logging queue.getInstance().push(new legacy_log_entry($"[Server.DispatchpacketSameThread][Error][MY] Ao tratar o pacote. ID: {_packet.getTipo()}(0x{_packet.getTipo():X})," + pd._packet.Log(), legacy_log_destination.CL_FILE_LOG_AND_CONSOLE));
                            //DisconnectSession(session);
                        }
                    }

                    catch (exception e)
                    {
                        m_logger.Write($"[Server.DispatchpacketSameThread][Error][MY] {e.getFullMessageError()}", LogDestination.GeneralFile | LogDestination.Console);

                        DisconnectSession(session);
                    }
                }
            }
            catch (exception e)
            {
                m_logger.Write($"[Server.DispatchpacketSameThread][Error][MY] {e.Message}", LogDestination.GeneralFile | LogDestination.Console);

                DisconnectSession(session);
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
                m_logger.Write($"[Server.DispatchpacketSameThread][ErrorSystem] {e.Message}, {e.getStackTrace()}", LogDestination.GeneralFile | LogDestination.Console);
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

                if (CheckPacket(session, _packet, 1))
                {
                    try
                    {
                        if (func != null && func.ExecCmd(pd) != 0)
                        {
                            // _smp.legacy logging queue.getInstance().push(new legacy_log_entry($"[Server.DispatchpacketSameThread][Error][MY] Ao tratar o pacote. ID: {_packet.getTipo()}(0x{_packet.getTipo():X})," + pd._packet.Log(), legacy_log_destination.CL_FILE_LOG_AND_CONSOLE));
                            DisconnectSession(session);
                        }
                    }

                    catch (exception e)
                    {
                        m_logger.Write($"[Server.DispatchpacketSameThread][Error][MY] {e.getFullMessageError()}", LogDestination.GeneralFile | LogDestination.Console);

                        DisconnectSession(session);
                    }
                }
            }
            catch (exception e)
            {
                m_logger.Write($"[Server.DispatchpacketSameThread][Error][MY] {e.Message}", LogDestination.GeneralFile | LogDestination.Console);

                DisconnectSession(session);
            }
        }

        #endregion

        #region Public Methods

        public void Start()
            => StartAsync(CancellationToken.None).GetAwaiter().GetResult();

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_isRunning)
                return Task.CompletedTask;

            try
            {
                _server = new TcpListener(IPAddress.Any, m_si.Port);
                m_state = ServerState.Initialized;

                if (m_state != ServerState.Failure)
                {

                    try
                    {
                        _server.Start(m_si.MaxUsers);

                        m_logger.Write("[unit::Start][Log] Running in Port: " + m_si.Port, LogDestination.GeneralFile | LogDestination.Console);

                        _lifetimeCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        _monitorTask = OnMonitorAsync(_lifetimeCancellation.Token);
                        _acceptLoopTask = HandleWaitConnectionsAsync(_lifetimeCancellation.Token);

                        // On Start
                        OnStart();
                    }
                    catch (exception e)
                    {
                        m_logger.Write(e.getFullMessageError(), LogDestination.GeneralFile | LogDestination.Console);
                    }

                }
                else
                {
                    m_logger.Write("[unit::start][Error] Server Inicializado com falha, fechando o server.", LogDestination.GeneralFile | LogDestination.Console);
                }
            }
            catch (exception e)
            {
                m_logger.Write(e.getFullMessageError(), LogDestination.GeneralFile | LogDestination.Console);
            }

            return Task.CompletedTask;
        }

        public void Stop()
            => StopAsync(CancellationToken.None).GetAwaiter().GetResult();

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (m_state == ServerState.Failure)
                return;

            m_state = ServerState.Failure;
            _lifetimeCancellation?.Cancel();
            _server?.Stop();
            m_session_manager?.Clear();

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
                m_logger.Write($"[unit::StopAsync][ErrorSystem] {exception}", LogDestination.GeneralFile | LogDestination.Console);
            }

            _connectionTasks.Clear();
            _acceptLoopTask = null;
            _monitorTask = null;
            _lifetimeCancellation?.Dispose();
            _lifetimeCancellation = null;
            Console.WriteLine("Server is stopping...");
        }

        public virtual bool CheckCommand(Queue<string> command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            RunCommand(command.ToArray());
            return true;
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
                throw new Exception("[unit::init_option_accepted_socket][Error] não conseguiu desabilitar tcp delay (nagle algorithm).", ex);
            }

            try
            {
                // KEEPALIVE: habilita + configura tempo
                byte[] keepAlive = new byte[12];
                BitConverter.GetBytes((uint)1).CopyTo(keepAlive, 0);     // onoff
                BitConverter.GetBytes((uint)20000).CopyTo(keepAlive, 4); // keepalivetime (20s)
                BitConverter.GetBytes((uint)2000).CopyTo(keepAlive, 8);  // keepaliveinterval (2s)

                _accepted.IOControl(IOControlCode.KeepAliveValues, keepAlive, null);
                 
            }
            catch (SocketException ex)
            {
                throw new Exception("[unit::init_option_accepted_socket][Error] não conseguiu setar o socket option KEEPALIVE.", ex);
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
            Console.WriteLine("Shutting down server...");
            Stop();
        }

        public virtual uint GetUID()
        {
            return (uint)m_si.Id;
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
                Console.WriteLine("[unit::DisconnectSession][Warning] Tentativa de desconectar uma sessão nula.");
                return false;
            }

            m_logger.Write($"[unit::DisconnectSession][Log] PLAYER[IP: {_session.getIP()}, Key: {_session.m_key}, Time: {DateTime.Now}]", LogDestination.GeneralFile | LogDestination.Console);

            // Notifica que a desconexão ocorreu       
            onDisconnected(_session);

            bool result;
            try
            {
                // Remove a sessão do gerenciador        
                result = m_session_manager.DeleteSession(_session);
            }
            catch (Exception ex)
            {
                result = false;
                Console.WriteLine($"[unit::DisconnectSession][Error] Erro ao deletar sessão: {ex.Message}");
            }
            return result;
        }


        public virtual void SQLDBResponse(int _msg_id, Pangya_DB _pangya_db, object _arg)
        {
            if (_arg == null)
            {
                m_logger.Write("[Server.SQLDBResponse][Warning] _arg is null, na msg_id = " + _msg_id, LogDestination.GeneralFile | LogDestination.Console);
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


        public virtual void RunCommand(string[] comando)
        {

        }

        public int getBotTTL() => m_Bot_TTL;
        #endregion  
    }
}
