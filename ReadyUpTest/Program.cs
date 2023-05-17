using ReadyUp;
using System;

namespace ReadySteadyTest
{
    class Program
    {
        static NetworkServer server;
        static NetworkClient client;

        static void Main(string[] args)
        {
            server = new NetworkServer(4117);

            client = new NetworkClient("127.0.0.1", 4117);

            Console.ReadLine();
        }
    }
}