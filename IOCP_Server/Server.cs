using System.Buffers;
using System.Net;
using System.Net.Sockets;

namespace IOCP_Server
{
    public class Server
    {
        private int _port;
        private SocketAsyncEventArgsPool _readWritePool;

        private int _connectionSize;
        private Semaphore _acceptClients;
        private Socket _listenSocket;
        public Server(int port,int ConnectionSize)
        {
            _connectionSize = ConnectionSize;
            _port = port;
            _readWritePool = new SocketAsyncEventArgsPool(_connectionSize);
            _acceptClients = new Semaphore(_connectionSize, ConnectionSize);
            init();
        }

        public void init()
        {
            SocketAsyncEventArgs socketEventArgs;
            for(int i = 0; i < _connectionSize; i++)
            {
                socketEventArgs = new SocketAsyncEventArgs();
                socketEventArgs.Completed += ComplateIO;
                _readWritePool.Push(socketEventArgs);
            }
        }

        public void Run()
        {
            _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, _port);
            _listenSocket.Bind(endPoint);
            _listenSocket.Listen(10);
            SocketAsyncEventArgs acceptEventArgs = new SocketAsyncEventArgs();
            acceptEventArgs.Completed += Aceept_Completed;
            RunningSocket(acceptEventArgs);
            CloseClientSocket(acceptEventArgs);
        }

        public void RunningSocket(SocketAsyncEventArgs args)
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
            SocketAsyncEventArgs args = _readWritePool.Pop();
            args.UserToken = e.UserToken;

            bool someEvent = e.AcceptSocket.ReceiveAsync(args);
            if (!someEvent)
            {
                StartReceive(args);
            }
        }

        private void ComplateIO(object? sender, SocketAsyncEventArgs e)
        {

        }

        private void Aceept_Completed(object? sender, SocketAsyncEventArgs e)
        {
            Accept_Start(e);

            RunningSocket(e);
        }
        private void StartReceive(SocketAsyncEventArgs e)
        {
            if(e.BytesTransferred == 0 || e.SocketError != SocketError.Success)
            {
                CloseClientSocket(e);
                return;
            }
            byte[] data = new byte[e.BytesTransferred];
            Console.WriteLine(data);
            e.SetBuffer(data);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        private void StartSend(SocketAsyncEventArgs e)
        {
            if(e.SocketError != SocketError.Success)
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
            Socket socket = (Socket)e.UserToken;

            try
            {
                socket.Shutdown(SocketShutdown.Send);
            }
            catch{ };
            socket.Close();
            _readWritePool.Push(e);
            _acceptClients.Release();
        }
    }
}
