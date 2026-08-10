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

    public class UnitPlayer : Session
    {
        public struct player_info
        {
            public string nickname;
            public string id;
            public uint uid;
            public uint tipo;
            public byte m_state;
        }
        public override byte getStateLogged() => m_pi.m_state;
        public override uint getUID() => m_pi.uid;
        public override uint getCapability() => m_pi.tipo;
        public override string getNickname() => m_pi.nickname;
        public override string getID() => m_pi.id;
        public ServerInfoEx m_si;
        public player_info m_pi;

        public UnitPlayer(pangya_packet_handle _Packet_Handle, ServerInfoEx serverInfo)
        {
            m_si = serverInfo;
            this._Packet_Handle_Base = _Packet_Handle;
            m_pi = new player_info();
        }


        public async Task ConnectAsync(string ip, int port, CancellationToken cancellationToken)
        {
            m_pi = new player_info();
            m_sock = new TcpClient();
            await m_sock.ConnectAsync(ip, port, cancellationToken).ConfigureAwait(false);
            m_addr = m_sock.Client.RemoteEndPoint as IPEndPoint;
            m_ip = "0.0.0.0";
            m_ip_maked = false;
            setState(true);
            setConnected(true);
        }

        public override bool clear()
        {
            m_pi = new player_info();
            return base.clear();
        }

        public override void requestSendBuffer(byte[] _buff, bool _raw = false)
        {

            if (_buff == null)
            {
                throw new exception("Error _buff is null. Session::requestSendBuffer()", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.SESSION,
                    3, 0));
            }
            int _size = _buff.Length;
            if (_size <= 0)
            {
                throw new exception("Error _size is less or equal the zero. Session::requestSendBuffer()", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.SESSION,
                    4, 0));
            }
            try
            {
                if (isConnectedToSend())
                {

                    var payloadData = _raw ? _buff : Cipher.ServerEncrypt(_buff, m_key, 0);

                    if (!m_sock.Send(payloadData, payloadData.Length))
                    {
                        @lock();
                        setConnectedToSend(false);
                        unlock();

                        try
                        {
                            _Packet_Handle_Base.DisconnectSession(this);
                        }
                        catch (exception e)
                        {
                            PangyaLog.Write("[threadpool::send_new][Error] " + e.getFullMessageError(), LogDestination.GeneralFile | LogDestination.Console);
                        }
                    }
                    else
                    {
                        //new mode
                        _Packet_Handle_Base.dispach_packet_sv_same_thread(this, _raw ? new packet(_buff) : new packet(_buff));
                    }
                }
                else
                {
                    //m_buff_s.releaseWrite();
                    return;
                }
            }
            finally
            {
                // m_buff_s.unlock();
            }
        }

    }
}
