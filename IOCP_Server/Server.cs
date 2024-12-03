using System.Net;
using System.Net.Sockets;

namespace IOCP_Server
{
    public class Server
    {
        private int _port;
        private SocketAsyncEventArgsPool _readWritePool;
        public Server(int port)
        {
            _port = port;
            _readWritePool = new SocketAsyncEventArgsPool(30);
        }
        public void Run()
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, _port);
            socket.Bind(endPoint);
            socket.Listen(10);
            while (true)
            {

            }
        }
    }
}
