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
        /*
         * SemaphoreSlim은 일정 spin lock후 다른 Thread로 context switching함. 그러므로 금방 lock을 얻을 수 있는 경우 적합함.
         * Semaphore는 OS 커널의 의해서 얻으므로 lock을 획득 시 많은 cpu clock이 소모되나 필요한 만큼만 대기함. 그러므로 오래 대기하는 경우 적합함.
        */
        private Semaphore _acceptClients;
        private Socket _listenSocket;
        private bool disposedFlag=false;
        private BufferMemoryPool _pool;

        public Server(int port,int ConnectionSize)
        {
            _connectionSize = ConnectionSize;
            _pool = new BufferMemoryPool(4096, ConnectionSize);
            _port = port;
            _readWritePool = new SocketAsyncEventArgsPool(_connectionSize);
            _acceptClients = new Semaphore(_connectionSize, ConnectionSize);
            _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            init();
        }

        public static void Main()
        {
            using Server server = new Server(34543,10000);
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
                _pool.Pop(out Memory<byte> block);
                socketEventArgs.SetBuffer(block);
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
            data.MemoryBuffer.Span.Fill(0);
            //offset reset
            data.SetBuffer(data.MemoryBuffer);
            if (!socket.ReceiveAsync(data))
            {
                EndReceive(data);
            }
        }
        private void EndReceive(SocketAsyncEventArgs data)
        {
            if (data.SocketError != SocketError.Success || data.MemoryBuffer.IsEmpty)
            {
                Recycle(data);
                return;
            }
            if (data.BytesTransferred > 0)
            {
                string reciveData = Encoding.UTF8.GetString(data.MemoryBuffer.Span);
#if !BENCHMARK
                IPEndPoint? endPoint= (IPEndPoint?)((Socket)data.UserToken).RemoteEndPoint;
                Console.WriteLine($"recive data from {endPoint?.Address.ToString()}:{endPoint?.Port}   :  {reciveData}");
#endif   
                if (data.BytesTransferred == data.MemoryBuffer.Length)
                {
                    StartReceive(data);
                    return;
                }
            }
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
            int copyNum=Encoding.UTF8.GetBytes("Recive ok",data.MemoryBuffer.Span);
            data.MemoryBuffer.Slice(copyNum, data.MemoryBuffer.Length-copyNum).Span.Fill(0);
            data.SetBuffer(data.MemoryBuffer);
#if !BENCHMARK
            Console.WriteLine($"send to {((IPEndPoint?)AccepteSocket.RemoteEndPoint)?.Address.ToString()} : {Encoding.UTF8.GetString(data.MemoryBuffer.Span)}");
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
