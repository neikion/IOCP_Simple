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
        }

        Socket _listenSocket;
        private bool disposedFlag;
        private bool _oneSAEA = false;

        public Client()
        {
            _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void Run()
        {
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Loopback, 34543);
            SocketAsyncEventArgs socketEventArgs = new SocketAsyncEventArgs();
            socketEventArgs.RemoteEndPoint = remoteEP;
            socketEventArgs.Completed+=ComplateIO;
            RunningSocketOneSAEA(socketEventArgs);
            Console.WriteLine("Client working... press any key to close Client");
            Console.ReadKey();
            socketEventArgs.Dispose();
            Dispose();
        }

        // TODO : Need more Implementing
        private void RunningSocket(SocketAsyncEventArgs args)
        {
            bool someEvent = false;
            while (!someEvent)
            {
                args.AcceptSocket = null;
                if (!_listenSocket.ConnectAsync(args))
                {
                    StartSend(args);
                }
            }
        }

        private void RunningSocketOneSAEA(SocketAsyncEventArgs args)
        {
            _oneSAEA = true;
            if (!_listenSocket.ConnectAsync(args))
            {
                StartSend(args);
            }
            }

        private void ComplateIO(object? sender, SocketAsyncEventArgs e)
        {
            //Console.WriteLine(e.SocketError);
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    StartSend(e);
                    break;
                case SocketAsyncOperation.Receive:
                    EndReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    StartReceive(e);
                    break;
            }
        }
        private void StartSend(SocketAsyncEventArgs e)
        {
            Console.WriteLine($"socket send to {(e.RemoteEndPoint as IPEndPoint)?.Address}");
            if (e.SocketError != SocketError.Success)
            {
                Recycle(e);
                return;
            }
            Console.WriteLine($"socket send to {(e.RemoteEndPoint as IPEndPoint)?.Address}");
            byte[] data = Encoding.UTF8.GetBytes("test");
            e.SetBuffer(data,0,data.Length);
            bool someEvent = e.ConnectSocket.SendAsync(e);
            if (!someEvent)
            {
                StartReceive(e);
            }
        }

        private void StartReceive(SocketAsyncEventArgs e)
        {
            if (!e.ConnectSocket.ReceiveAsync(e))
            {
                EndReceive(e);
            }
        }

        private void EndReceive(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred == 0 || e.SocketError != SocketError.Success)
            {
                Recycle(e);
                return;
            }
            Console.WriteLine(Encoding.UTF8.GetString(e.Buffer));
        }

        
        private void Recycle(SocketAsyncEventArgs e)
        {
            if (e.UserToken != null)
            {
                DisposeSocket((Socket)e.UserToken);
            }
            if (!_oneSAEA)
            {
                _acessLimit.Release();
            }
        }
        private void DisposeSocket(Socket socket)
        {
            try
            {
                if (socket.Connected)
                {
                    _listenSocket.Shutdown(SocketShutdown.Both);
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
                    DisposeSocket(_listenSocket);
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
