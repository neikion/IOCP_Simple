using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace IOCP_Server
{
    public class Server : IDisposable
    {

        /*
         SocketAsyncEventArgs를 이용한 xxxAsync 는
        비동기적으로 완료된 경우 true, 동기적으로 완료될 경우 false를 보내며,
        그 결과를 매개변수 SocketAsyncEventArgs에 기록한다.
         
         
         
         */


        private int _port;
        private SocketAsyncEventArgsPool _readWritePool;

        private int _connectionSize;
        private Semaphore _acceptClients;
        private Socket _listenSocket;
        private bool disposedFlag=false;

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
            Console.WriteLine("server working... press any key to close server");
            Console.ReadKey();
        }

        public void init()
        {
            SocketAsyncEventArgs socketEventArgs;
            for(int i = 0; i < _connectionSize; i++)
            {
                socketEventArgs = new SocketAsyncEventArgs();
                socketEventArgs.Completed += ComplateIO;
                socketEventArgs.SetBuffer(new byte[4096], 0,4096);
                _readWritePool.Push(socketEventArgs);
            }
        }

        public void Run()
        {
            _listenSocket.Bind(new IPEndPoint(IPAddress.Any, _port));
            _listenSocket.Listen(10);
            SocketAsyncEventArgs acceptEventArgs = new SocketAsyncEventArgs();
            acceptEventArgs.Completed += RunningSocketEnd;
            WaitingAccept(acceptEventArgs);
        }

        private void RunningSocketEnd(object? sender, SocketAsyncEventArgs e)
        {
            //받은 요청을 처리함.
            AcceptRequest(e);
            //추가적인 요청 기다림
            WaitingAccept(e);
        }

        /// <summary>
        /// 동기적으로 처리시 다음 요청을 기다리고, 비동기적으로 처리 시 루프를 빠져나간다.
        /// 
        /// </summary>
        /// <param name="args"></param>
        private void WaitingAccept(SocketAsyncEventArgs args)
        {
            bool someEvent = false;
            while (!someEvent)
            {
                _acceptClients.WaitOne();
                if (disposedFlag)
                {
                    break;
                }
                args.AcceptSocket = null;
                // 계속해서 새로운 thread에 일을 할당하지만, .net 자체적인 thread pool을 이용하기 떄문에
                // 새로운 thread를 생성하지 않으므로 자원을 보다 효율적으로 사용한다.
                someEvent = _listenSocket.AcceptAsync(args);
                //만약 동기적으로 실행된다면, thread pool에 남은 thread가 없다는 뜻이므로
                //현재 thread를 계속 활용한다.
                if (!someEvent)
                {
                    AcceptRequest(args);
                }
            }
        }

        

        private void AcceptRequest(SocketAsyncEventArgs e)
        {
            if(e.SocketError != SocketError.Success || e.AcceptSocket == null)
            {
                if (e.AcceptSocket != null)
                {
                    DisposeSocket(e.AcceptSocket);
                }
                Recycle(e);
                return;
            }
            //e는 새로운 WaitingAccept을 위해 닫지 않는다.
            SocketAsyncEventArgs args = _readWritePool.Pop();
            args.UserToken = e.AcceptSocket;
                StartReceive(args);
            }

        private void ComplateIO(object? sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                    AcceptRequest(e);
                    break;
                case SocketAsyncOperation.Receive:
                    EndReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    EndSend(e);
                    break;
            }
        }

        private void StartReceive(SocketAsyncEventArgs data)
        {
            if(data.UserToken == null)
            {
                Recycle(data);
                return;
            }
            Socket socket = (Socket)data.UserToken;
            if (!socket.ReceiveAsync(data))
            {
                EndReceive(data);
            }
        }
        private void EndReceive(SocketAsyncEventArgs data)
        {
            if (data.BytesTransferred == 0 || data.SocketError != SocketError.Success || data.Buffer==null)
            {
                Recycle(data);
                return;
            }
            string reciveData = Encoding.UTF8.GetString(data.Buffer, 0, data.BytesTransferred);
#if !BENCHMARK
            Console.WriteLine($"recive data from {((IPEndPoint?)data.RemoteEndPoint)?.Address.ToString()}:{reciveData}");
#endif
            StartSend(data);
        }
        private void StartSend(SocketAsyncEventArgs data)
        {
            if (data.SocketError != SocketError.Success || data.UserToken==null)
            {
                Recycle(data);
                return;
            }
            Socket AccepteSocket = (Socket)data.UserToken;
            //response for request
            byte[] senddata = Encoding.UTF8.GetBytes("Recive ok");
            data.SetBuffer(senddata, 0, senddata.Length);
#if !BENCHMARK
            Console.WriteLine($"send to {((IPEndPoint?)AccepteSocket.RemoteEndPoint)?.Address.ToString()}");
#endif
            if (!AccepteSocket.SendAsync(data))
            {
                EndSend(data);
            }
            
        }
        private void EndSend(SocketAsyncEventArgs data)
        {
#if RELEASE || DEBUG
            //더 보낼 데이터가 없다면
            StartReceive(data);
#elif BENCHMARK
            //jmeter 테스트를 위함
            Recycle(data);
#endif
        }

        private void Recycle(SocketAsyncEventArgs e)
        {
            if (e.UserToken != null)
            {
                DisposeSocket((Socket)e.UserToken);
            }
            _readWritePool.Push(e);
            _acceptClients.Release();
        }
        private void DisposeSocket(Socket socket)
        {
            try
            {
                if (socket.Connected)
                {
                    socket.Shutdown(SocketShutdown.Both);
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
