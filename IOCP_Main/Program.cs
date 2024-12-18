namespace IOCP_Main
{
    using IOCP_Server;
    using IOCP_Client;
    internal class Program
    {
        static void Main(string[] args)
        {
            using Server server = new Server(34543,30);
            using Client client = new Client();
            using Client client2 = new Client();

            server.Run();
            client.Run();
            client2.Run();
            Console.ReadKey();
        }
    }
}
