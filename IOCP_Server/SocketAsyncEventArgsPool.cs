using System.Net.Sockets;

namespace IOCP_Server
{
    public class SocketAsyncEventArgsPool
    {
        private Stack<SocketAsyncEventArgs> _pool;
        public int Count => _pool.Count;

        public SocketAsyncEventArgsPool(int capacity)
        {
            _pool=new Stack<SocketAsyncEventArgs>(capacity);
        }

        public void Push(SocketAsyncEventArgs item)
        {
            if (item == null)
            {
                return;
            }
            lock (_pool)
            {
                _pool.Push(item);
            }
        }

        public SocketAsyncEventArgs Pop()
        {
            lock (_pool)
            {
                return _pool.Pop();
            }
        }
        
    }
}
