using ReadyUp;

namespace ReadySteadyTest
{
    class Program
    {
        static NetworkServer server;
        static NetworkClient client;

        static void Main(string[] args)
        {
            server = new NetworkServer();

            client = new NetworkClient("localhost", 4117);

            Console.ReadLine();
        }
    }
}