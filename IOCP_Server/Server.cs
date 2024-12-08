using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace IOCP_Server
{
    public class Server : IDisposable
    {
        private int _port;
        private SocketAsyncEventArgsPool _readWritePool;

        private int _connectionSize;
        private Semaphore _acceptClients;
        private Socket _listenSocket;
        private bool disposedFlag;

        public Server(int port,int ConnectionSize)
        {
            _connectionSize = ConnectionSize;
            _port = port;
            _readWritePool = new SocketAsyncEventArgsPool(_connectionSize);
            _acceptClients = new Semaphore(_connectionSize, ConnectionSize);
            _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            init();
        }

        public static void Main()
        {
            using Server server = new Server(34543,30);
            server.Run();

            Console.WriteLine("server working...");
            Console.ReadKey();
        }

        public void init()
        {
            SocketAsyncEventArgs socketEventArgs;
            for(int i = 0; i < _connectionSize; i++)
            {
                socketEventArgs = new SocketAsyncEventArgs();
                socketEventArgs.Completed += ComplateIO;
                socketEventArgs.SetBuffer(new byte[1024]);
                _readWritePool.Push(socketEventArgs);
            }
        }

        public void Run()
        {
            _listenSocket.Bind(new IPEndPoint(IPAddress.Any, _port));
            _listenSocket.Listen(10);
            SocketAsyncEventArgs acceptEventArgs = new SocketAsyncEventArgs();
            acceptEventArgs.Completed += Aceept_Completed;
            RunningSocket(acceptEventArgs);
        }

        private void RunningSocket(SocketAsyncEventArgs args)
        {
            bool someEvent = false;
            while (!someEvent)
            {
                _acceptClients.WaitOne();
                args.AcceptSocket = null;
                someEvent = _listenSocket.AcceptAsync(args);
                if (!someEvent)
                {
                    Accept_Start(args);
                }
            }

        }

        private void Accept_Start(SocketAsyncEventArgs e)
        {
            Console.WriteLine("accept start");
            if(e.SocketError != SocketError.Success)
            {
                Console.WriteLine(e.SocketError);
            }
            SocketAsyncEventArgs args = _readWritePool.Pop();
            args.UserToken = e.AcceptSocket;
            bool someEvent = e.AcceptSocket.ReceiveAsync(args);
            if (!someEvent)
            {
                StartReceive(args);
            }
        }

        private void ComplateIO(object? sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    StartReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    StartSend(e);
                    break;
            }
        }

        private void Aceept_Completed(object? sender, SocketAsyncEventArgs e)
        {
            Accept_Start(e);
            //추가적인 요청 기다림
            RunningSocket(e);
        }
        private void StartReceive(SocketAsyncEventArgs e)
        {
            if(e.BytesTransferred == 0 || e.SocketError != SocketError.Success)
            {
                Console.WriteLine("receive close");
                CloseClientSocket(e);
                return;
            }
            /*byte[] data = e.Buffer;
            e.SetBuffer(data);*/
            Socket socket = (Socket)e.UserToken;
            //Console.WriteLine(e.Buffer);
            Console.WriteLine(Encoding.UTF8.GetString(e.Buffer));
            if (!socket.SendAsync(e))
            {
                StartSend(e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        private void StartSend(SocketAsyncEventArgs e)
        {
            if(e.SocketError != SocketError.Success || e.UserToken==null)
            {
                CloseClientSocket(e);
                return;
            }
            //Receive another data
            Socket socket = (Socket)e.UserToken;
            bool someEvent = socket.ReceiveAsync(e);
            if (!someEvent)
            {
                StartReceive(e);
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
            _readWritePool.Push(e);
            _acceptClients.Release();
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
                    catch (Exception e) { Debug.WriteLine($"{e} \n {e.StackTrace}"); };
                    _listenSocket.Close();
                }
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
