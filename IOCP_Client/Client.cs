using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace IOCP_Client
{
    public class Client : IDisposable
    {
        public static void Main()
        {
            using Client client = new Client();
            client.Run();
            Console.ReadKey();
        }

        Socket _listenSocket;
        private bool disposedFlag;

        public Client()
        {
            _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void Run()
        {
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Loopback, 34543);
            SocketAsyncEventArgs socketEventArgs = new SocketAsyncEventArgs();
            socketEventArgs.RemoteEndPoint = remoteEP;
            socketEventArgs.Completed+=Accept_Complated;
            _listenSocket.ConnectAsync(socketEventArgs);
        }

        private void Accept_Start(SocketAsyncEventArgs e)
        {
            if (e.AcceptSocket == null)
            {
                return;
            }
            SocketAsyncEventArgs ske = new SocketAsyncEventArgs();
            ske.UserToken = e.AcceptSocket;
            bool someEvent = e.AcceptSocket.ReceiveAsync(ske);
            if (!someEvent)
            {
                StartReceive(ske);
            }
        }
        private void Accept_Complated(object? sender, SocketAsyncEventArgs e)
        {
            if (e.LastOperation == SocketAsyncOperation.Receive)
            {
                Accept_Start(e);
            }
            else if(e.LastOperation == SocketAsyncOperation.Connect)
            {
                Console.WriteLine("connected");
                StartSend(e);
            }
            
        }
        private void StartSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                CloseClientSocket(e);
                return;
            }
            //Receive another data
            byte[] data = Encoding.UTF8.GetBytes("test");
            e.SetBuffer(data,0,data.Length);
            bool someEvent = _listenSocket.SendAsync(e);
            if (!someEvent)
            {
                StartReceive(e);
            }
        }

        private void StartReceive(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred == 0 || e.SocketError != SocketError.Success)
            {
                CloseClientSocket(e);
                return;
            }

            Socket socket = (Socket)e.UserToken;
            Console.WriteLine(Encoding.UTF8.GetString(e.Buffer));
            CloseClientSocket(e);
        }

        public void RunningSocket(SocketAsyncEventArgs args)
        {
            bool someEvent = false;
            while (!someEvent)
            {
                args.AcceptSocket = null;
                someEvent = _listenSocket.AcceptAsync(args);
                if (!someEvent)
                {
                    Accept_Start(args);
                }
            }
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            if (e.UserToken != null)
            {
                Socket socket = (Socket)e.UserToken;
                try
                {
                    socket.Shutdown(SocketShutdown.Send);
                }
                catch { };
                socket.Close();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedFlag)
            {
                if (disposing)
                {
                    try
                    {
                        _listenSocket.Shutdown(SocketShutdown.Send);
                    }
                    catch { }
                    _listenSocket.Close();
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
