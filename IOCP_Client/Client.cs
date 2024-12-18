using System.Net;
using System.Net.Sockets;
using System.Text;

namespace IOCP_Client
{
    public class Client : IDisposable
    {
        public static void Main()
        {
            using Client client = new Client();
            client.Run();
        }

        private Socket connectSocket;
        private bool disposedFlag;
        SocketAsyncEventArgs socketEventArgs = new SocketAsyncEventArgs();
        public Client()
        {
            connectSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Loopback, 34543);
            socketEventArgs.RemoteEndPoint = remoteEP;
            socketEventArgs.Completed += ComplateIO;
        }

        public void Run()
        {
            StartConnectOneSAEA(socketEventArgs);
            /*
            Console.WriteLine("Client working... press any key to close Client");
            Console.ReadKey();
            */
            Recycle(socketEventArgs);
        }

        private void StartConnectOneSAEA(SocketAsyncEventArgs e)
        {
            if (!connectSocket.ConnectAsync(e))
            {
                EndConnect(e);
            }
        }

        private void ComplateIO(object? sender, SocketAsyncEventArgs e)
        {
            Console.WriteLine(e.SocketError);
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    EndConnect(e);
                    break;
                case SocketAsyncOperation.Receive:
                    EndReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    EndSend(e);
                    break;
            }
        }

        private void EndConnect(SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                Console.WriteLine($"error {e.SocketError}");
                Recycle(e);
                return;
            }
            e.UserToken = e.ConnectSocket;
            StartSend(e);
        }

        private void StartSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success || e.UserToken == null)
            {
                Console.WriteLine($"error {e.SocketError}");
                Recycle(e);
                return;
            }
            Socket ConnectSocket = (Socket)e.UserToken;
            Console.WriteLine($"socket send to {(ConnectSocket.RemoteEndPoint as IPEndPoint)?.Address}");
            byte[] data = Encoding.UTF8.GetBytes("test");
            e.SetBuffer(data, 0, data.Length);
            if (!ConnectSocket.SendAsync(e))
            {
                EndSend(e);
            }
        }

        private void EndSend(SocketAsyncEventArgs e)
        {
            StartReceive(e);
        }

        private void StartReceive(SocketAsyncEventArgs e)
        {
            if (e.UserToken == null)
            {
                Console.WriteLine("Connected Socket Missing");
                Recycle(e);
                return;
            }
            Console.WriteLine($"Start receive ");
            Socket ConnectSocket = (Socket)e.UserToken;
            byte[] data = new byte[1024];
            e.SetBuffer(data, 0, data.Length);
            if (!ConnectSocket.ReceiveAsync(e))
            {
                EndReceive(e);
            }
        }

        private void EndReceive(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred == 0 || e.SocketError != SocketError.Success)
            {
                Console.WriteLine($"Error {e.SocketError}");
                return;
            }
            Console.WriteLine("recieve data : " + Encoding.UTF8.GetString(e.Buffer));
        }
        

        private void Recycle(SocketAsyncEventArgs e)
        {
            if (e.UserToken != null)
            {
                DisposeSocket((Socket)e.UserToken);
            }
        }
        private void DisposeSocket(Socket socket)
        {
            try
            {
                if (socket.Connected)
                {
                    connectSocket.Shutdown(SocketShutdown.Both);
                }
            }
            catch { }
            socket.Close();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedFlag)
            {
                if (disposing)
                {
                    DisposeSocket(connectSocket);
                }
                
                // TODO: 비관리형 리소스(비관리형 개체)를 해제하고 종료자를 재정의합니다.
                // TODO: 큰 필드를 null로 설정합니다.
                disposedFlag = true;
            }
        }

        public void Dispose()
        {
            // 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
