using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using PangyaAPI.Network.Cryptor;
using PangyaAPI.Network.Models;
using PangyaAPI.Network.PangyaPacket;
using PangyaAPI.Network.PangyaSession;
using PangyaAPI.Network.Configuration;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.BinaryModels;
using PangyaAPI.Utilities.Log;

namespace PangyaAPI.Network.PangyaUnit
{

    public abstract class unit_connect_base : pangya_packet_handle
    {
        public unit_connect_base(ServerInfoEx _si, EndpointOptions authServer)
        {
            funcs = new func_arr();
            funcs_sv = new func_arr();
            m_unit_ctx = new stUnitCtx();
            config_init(authServer);
            // ------------------------------------
            m_session = new UnitPlayer(this, _si);
        }

        protected void config_init(EndpointOptions authServer)
        {
            if (authServer == null)
                throw new ArgumentNullException(nameof(authServer));

            m_unit_ctx.ip = authServer.Host;
            m_unit_ctx.port = authServer.Port;

            // Carregou com sucesso
            m_unit_ctx.state = true;
        }
        public enum STATE : byte { UNINITIALIZED, GOOD, GOOD_WITH_WARNING, INITIALIZED, FAILURE }
        public enum ThreadType { WORKER_IO, WORKER_IO_SEND, WORKER_IO_RECV, WORKER_LOGICAL, WORKER_SEND, TT_CONSOLE, TT_ACCEPT, TT_ACCEPTEX, TT_ACCEPTEX_IO, TT_RECV, TT_SEND, TT_JOB, TT_DB_NORMAL, TT_MONITOR, TT_SEND_MSG_TO_LOBBY }
        public enum OperationType { SEND_RAW_REQUEST, SEND_RAW_COMPLETED, RECV_REQUEST, RECV_COMPLETED, SEND_REQUEST, SEND_COMPLETED, DISPACH_PACKET_SERVER, DISPACH_PACKET_CLIENT, ACCEPT_COMPLETED }

        public struct stUnitCtx
        {
            public bool state;
            public string ip;
            public int port;

            public void Clear()
            {
                ip = string.Empty;
                port = 0;
                state = false;
            }
        }

        public virtual bool isLive()
        {
            return (m_session.getState() && m_session.isConnected());
        }

        protected abstract void onHeartBeat();
        protected abstract void onConnected();
        protected abstract void onDisconnect();

        public bool ConnectAndAssoc()
            => ConnectAndAssocAsync(CancellationToken.None).GetAwaiter().GetResult();

        public async Task<bool> ConnectAndAssocAsync(CancellationToken cancellationToken)
        {
            if (!m_unit_ctx.state)
            {
                throw new Exception("[UnitConnectBase::ConnectAndAssoc][Error] A configuração do unit_connect não foi carregada com sucesso.");
            }

            try
            {
                // Conecta ao IP e Porta fornecidos
                await m_session.ConnectAsync(m_unit_ctx.ip, m_unit_ctx.port, cancellationToken).ConfigureAwait(false);
                _receiveTask = AcceptCompletedAsync(cancellationToken);

            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                m_session.clear();
                return false;
                //throw new Exception("[UnitConnectBase::ConnectAndAssoc][Error] Falha ao conectar.", ex);
            }

            // On Connected
            onConnected();


            return true;
        }

        private async Task ReconnectLoopAsync(CancellationToken cancellationToken)
        {
            PangyaLog.Write("[unit_connect::onMonitor][Log] monitor iniciado com sucesso!", LogDestination.GeneralFile);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (isLive())
                    {
                        onHeartBeat();
                        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    int delaySeconds = Math.Min(30, (int)Math.Pow(2, _retryCount));
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken).ConfigureAwait(false);

                    if (await ConnectAndAssocAsync(cancellationToken).ConfigureAwait(false))
                        _retryCount = 0;
                    else
                        _retryCount++;

                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (exception e) // Exceção específica da aplicação
                {
                    PangyaLog.Write(                        $"[unit.Monitor][ErrorSystem] {e.GetType().Name}: {e.getFullMessageError()}\nStack Trace: {e.getStackTrace()}", LogDestination.GeneralFile | LogDestination.Console);
                }
                catch (Exception ex) // Exceções gerais do .NET
                {
                    PangyaLog.Write(                        $"[unit.Monitor][ErrorSystem] {ex.GetType().Name}: {ex.Message}\nStack Trace: {ex.StackTrace}", LogDestination.GeneralFile | LogDestination.Console);
                    _retryCount++;
                }
            }
        }

        private async Task AcceptCompletedAsync(CancellationToken cancellationToken)
        {
            //send key
            bool raw = true;
            while (m_session.isConnected())
            {
                try
                {
                    if (!m_session.isConnected())
                    {
                        DisconnectSession(m_session);
                        break;
                    }

                    if (await recv_client_new_async(m_session, raw, cancellationToken).ConfigureAwait(false))
                    {
                        // Processa o pacote recebido
                        raw = false;//ja leu packet ket
                    }
                    else
                    {
                        DisconnectSession(m_session);
                        break;
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    DisconnectSession(m_session);
                    break;
                }
                catch (IOException ioEx)
                {
                    PangyaLog.Write("[unit_connect::accept_completed][IOError] " + ioEx.Message, LogDestination.GeneralFile | LogDestination.Console);
                    DisconnectSession(m_session);
                    break;
                }
                catch (exception ex)
                {
                    PangyaLog.Write("[unit_connect::accept_completed][ErrorSystem] " + ex.getFullMessageError(), LogDestination.GeneralFile | LogDestination.Console);
                    DisconnectSession(m_session);
                    break;
                }
            }
        }

        protected override void dispach_packet_same_thread(Session _session, packet _packet)
        {
            if (_session == null || _session.isConnected() == false || _packet == null)
            {
                return;//nao esta mais conectado!
            }

            func_arr.func_arr_ex func = null;

            try
            {
                // Obtém a função correspondente ao tipo de pacote
                func = funcs.getPacketCall(_packet.getTipo());
            }
            catch (exception e)
            {
                PangyaLog.Write($"[Server.DispatchpacketSameThread][ErrorSystem] {e.Message}, {e.getStackTrace()}", LogDestination.GeneralFile | LogDestination.Console);
                // Desconecta a sessão
                DisconnectSession(_session);
            }

            try
            {
                // Atualiza o tick do cliente
                _session.m_tick = Environment.TickCount;

                var pd = new ParamDispatch
                {
                    _session = _session,
                    _packet = _packet
                };
                try
                {
                    if (func != null && func.ExecCmd(pd) != 0)
                    {
                        // _smp.legacy logging queue.getInstance().push(new legacy_log_entry($"[Server.DispatchpacketSameThread][Error][MY] Ao tratar o pacote. ID: {_packet.getTipo()}(0x{_packet.getTipo():X})," + pd._packet.Log(), legacy_log_destination.CL_FILE_LOG_AND_CONSOLE));
                        DisconnectSession(_session);
                    }
                }

                catch (exception e)
                {
                    PangyaLog.Write($"[Server.DispatchpacketSameThread][Error][MY] {e.getFullMessageError()}", LogDestination.GeneralFile | LogDestination.Console);

                    DisconnectSession(_session);
                }
            }
            catch (exception e)
            {
                PangyaLog.Write($"[Server.DispatchpacketSameThread][Error][MY] {e.Message}", LogDestination.GeneralFile | LogDestination.Console);

                DisconnectSession(_session);
            }
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
                func = funcs_sv.getPacketCall(_packet.getTipo());
            }
            catch (exception e)
            {
                PangyaLog.Write($"[Server.DispatchpacketSameThread][ErrorSystem] {e.Message}, {e.getStackTrace()}", LogDestination.GeneralFile | LogDestination.Console);
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
                    PangyaLog.Write($"[Server.DispatchpacketSameThread][Error][MY] {e.getFullMessageError()}", LogDestination.GeneralFile | LogDestination.Console);

                    DisconnectSession(session);
                }
            }
            catch (exception e)
            {
                PangyaLog.Write($"[Server.DispatchpacketSameThread][Error][MY] {e.Message}", LogDestination.GeneralFile | LogDestination.Console);

                DisconnectSession(session);
            }
        }


        public override bool DisconnectSession(Session _session)
        {
            return _session.clear();//
        }

        private int _retryCount = 0;

        public void start()
            => StartAsync(CancellationToken.None).GetAwaiter().GetResult();

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_lifetimeCancellation != null && !_lifetimeCancellation.IsCancellationRequested)
                return Task.CompletedTask;

            _eventTryConnect.Set();
            _lifetimeCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _reconnectTask = ReconnectLoopAsync(_lifetimeCancellation.Token);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _lifetimeCancellation?.Cancel();
            m_session?.clear();

            var tasks = new[] { _reconnectTask, _receiveTask };
            foreach (var task in tasks)
            {
                if (task == null)
                    continue;

                try
                {
                    await task.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (_lifetimeCancellation?.IsCancellationRequested == true || cancellationToken.IsCancellationRequested)
                {
                    // Expected during shutdown.
                }
                catch (Exception exception)
                {
                    PangyaLog.Write($"[unit_connect::StopAsync][ErrorSystem] {exception}", LogDestination.GeneralFile | LogDestination.Console);
                }
            }

            _reconnectTask = null;
            _receiveTask = null;
            _lifetimeCancellation?.Dispose();
            _lifetimeCancellation = null;
        }

        public func_arr funcs;
        public func_arr funcs_sv;
        public UnitPlayer m_session;
        public STATE m_state;
        public stUnitCtx m_unit_ctx;
        private AutoResetEvent _eventTryConnect = new AutoResetEvent(false);
        private CancellationTokenSource _lifetimeCancellation;
        private Task _reconnectTask;
        private Task _receiveTask;

        public class packet_func_as
        {
            public static void session_send(PangyaBinaryWriter p, UnitPlayer s, byte _debug)
            {
                s.requestSendBuffer(p.GetBytes);
            }
            public static void session_send(List<PangyaBinaryWriter> v_p, UnitPlayer s, byte _debug)
            {
                foreach (var writer in v_p)
                {
                    s.requestSendBuffer(writer.GetBytes);
                }

            }
        }

    }
}
