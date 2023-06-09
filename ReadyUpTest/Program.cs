using ReadyUp;
using ReadyUpTest;
using System;
using System.Net;

namespace ReadySteadyTest
{
    class Program
    {
        static NetworkServer server;

        static void Main(string[] args)
        {
            server = new NetworkServer(4117);
            server.RegisterHandler<SendMessage>(OnMessageReceived);

            Console.ReadLine();
        }

        static void OnMessageReceived(SendMessage message, IPEndPoint endPoint)
        {
            Console.WriteLine("[Client] Message Recieved: " + message.message);

            SendMessage sendMessage = new SendMessage("Hello Client! :D");
            server.Send(sendMessage, endPoint);
        }
    }
}