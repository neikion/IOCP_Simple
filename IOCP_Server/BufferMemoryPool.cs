using System.Collections.Concurrent;

namespace IOCP_Server
{
    class BufferMemoryPool
    {
        public byte[] memory;
        private ConcurrentQueue<Memory<byte>> _bucket;
        public BufferMemoryPool(int memorySize=1024, int count=1024)
        {
            memory=GC.AllocateArray<byte>(memorySize*count);
            _bucket = new ConcurrentQueue<Memory<byte>>();
            for(int i = 0; i < count; i++)
            {
                _bucket.Enqueue(memory.AsMemory(i,memorySize));
            }
        }
        public bool Pop(out Memory<byte> result)
        {
            return _bucket.TryDequeue(out result);
        }
        public void Push(Memory<byte> memory)
        {
            _bucket.Enqueue(memory);
        }
    }
}
