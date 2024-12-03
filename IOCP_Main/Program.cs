namespace IOCP_Main
{
    using IOCP_Server;
    using IOCP_Client;
    internal class Program
    {
        static void Main(string[] args)
        {
            Server server = new Server(34543);
            server.Run();
        }
    }
}
