#nullable disable
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using PangyaAPI.Utilities.Resources;
namespace PangyaAPI.Utilities
{
    public static class TcpClientEx
    {
        public static (bool check, byte[] _buffer, int len) Read(this TcpClient client)
        {
            if (SocketConnected(client.Client))
                return client.Client.Read();
            else
                return (false, new byte[0], 0);
        } 

        public static ValueTask<(bool check, byte[] _buffer, int len)> ReadAsync(
            this TcpClient client,
            CancellationToken cancellationToken = default)
        {
            if (client == null || !client.Connected)
                return ValueTask.FromResult((false, Array.Empty<byte>(), 0));

            return client.Client.ReadAsync(cancellationToken);
        }

        public static bool Send(this TcpClient client, byte[] buffer, int len = 0)
        {
            if (client == null || buffer == null || len < 0 || len > buffer.Length || !client.Connected)
                return false;

            return client.GetStream().Send(buffer, 0, len);
        }

        public static bool Send(this NetworkStream stream, byte[] buffer, int offset, int len)
        {
            try
            {
                if (stream.CanWrite)
                {
                    stream.Write(buffer, offset, len);
                    return true;
                }

                return false;
            }
            catch (IOException ioEx)
            {
                Debug.WriteLine(UtilityMessages.Format("SendReadError", ioEx.Message));
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(UtilityMessages.Format("UnexpectedError", "Send", ex.Message));
                return false;
            }
        }

        public static TcpState GetState(this TcpClient tcpClient)
        {
            var foo = IPGlobalProperties.GetIPGlobalProperties()
              .GetActiveTcpConnections()
              .SingleOrDefault(x => x.LocalEndPoint.Equals(tcpClient.Client.LocalEndPoint)
                                 && x.RemoteEndPoint.Equals(tcpClient.Client.RemoteEndPoint)
              );

            return foo != null ? foo.State : TcpState.Unknown;
        }

        public static bool Shutdown(this TcpClient _sock, SocketShutdown how)
        { Shutdown(_sock.Client, how); return true; }

        public static bool Shutdown(this Socket _sock, SocketShutdown how)
        { _sock.Shutdown(how); return true; }
        public static bool SocketConnected(Socket s)
        {
            bool part1 = s.Poll(1000, SelectMode.SelectRead);
            bool part2 = (s.Available == 0);
            if (part1 & part2)
            {//connection is closed
                return false;
            }
            return true;
        }

        public static (bool Success, byte[] Buffer, int Length) Read(this Socket stream)
        {
            if (!stream.Connected)
            {
                Debug.WriteLine(UtilityMessages.Get("SocketDisconnected"));
                return (false, Array.Empty<byte>(), 0);
            }

            byte[] buffer = new byte[8192]; // 8 KB é suficiente na maioria dos casos
            try
            {
                int bytesRead = stream.Receive(buffer, 0, buffer.Length, SocketFlags.None);

                if (bytesRead == 0)
                {
                    Debug.WriteLine(UtilityMessages.Get("ClientDisconnected"));
                    return (false, Array.Empty<byte>(), 0);
                }

                byte[] result = new byte[bytesRead];
                Array.Copy(buffer, result, bytesRead);

                return (true, result, bytesRead);
            }
            catch (SocketException sockEx)
            {
                Debug.WriteLine(UtilityMessages.Format("SocketClosed", "Read", sockEx.SocketErrorCode, sockEx.Message));
                return (false, Array.Empty<byte>(), 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(UtilityMessages.Format("UnexpectedError", "Read", ex.Message));
                return (false, Array.Empty<byte>(), 0);
            }
        }

        public static async ValueTask<(bool check, byte[] _buffer, int len)> ReadAsync(
            this Socket stream,
            CancellationToken cancellationToken = default)
        {
            if (stream == null || !stream.Connected)
                return (false, Array.Empty<byte>(), 0);

            byte[] buffer = new byte[8192];
            try
            {
                int bytesRead = await stream.ReceiveAsync(
                    buffer.AsMemory(),
                    SocketFlags.None,
                    cancellationToken).ConfigureAwait(false);

                if (bytesRead == 0)
                    return (false, Array.Empty<byte>(), 0);

                if (bytesRead == buffer.Length)
                    return (true, buffer, bytesRead);

                return (true, buffer[..bytesRead], bytesRead);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (SocketException sockEx)
            {
                Debug.WriteLine(UtilityMessages.Format("SocketClosed", "ReadAsync", sockEx.SocketErrorCode, sockEx.Message));
                return (false, Array.Empty<byte>(), 0);
            }
            catch (ObjectDisposedException)
            {
                return (false, Array.Empty<byte>(), 0);
            }
        }

        public static (bool Success, byte[] Buffer, int Length) Read(this NetworkStream stream)
        {
            if (stream == null || !stream.CanRead)
            {
                Debug.WriteLine(UtilityMessages.Get("StreamUnreadable"));
                return (false, Array.Empty<byte>(), 0);
            }

            byte[] buffer = new byte[8192]; // 8 KB é suficiente na maioria dos casos
            try
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);

                if (bytesRead == 0)
                {
                    // Cliente fechou a conexão (EOF)
                    Debug.WriteLine(UtilityMessages.Get("ClientDisconnected"));
                    return (false, Array.Empty<byte>(), 0);
                }

                byte[] result = new byte[bytesRead];
                Array.Copy(buffer, result, bytesRead);

                return (true, result, bytesRead);
            }
            catch (IOException ioEx) when (ioEx.InnerException is SocketException sockEx)
            {
                Debug.WriteLine(UtilityMessages.Format("SocketClosed", "Read", sockEx.SocketErrorCode, sockEx.Message));
                return (false, Array.Empty<byte>(), 0);
            }
            catch (IOException ioEx)
            {
                Debug.WriteLine(UtilityMessages.Format("StreamReadError", ioEx.Message));
                return (false, Array.Empty<byte>(), 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(UtilityMessages.Format("UnexpectedError", "Read", ex.Message));
                return (false, Array.Empty<byte>(), 0);
            }
        }
    }
}
