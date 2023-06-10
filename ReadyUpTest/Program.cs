using Mimic;
using ReadyUpTest;
using System;
using System.Net;

namespace ReadySteadyTest
{
    class Program
    {
        static NetworkServer server;
        static NetworkClient client;

        static void Main(string[] args)
        {
            server = new NetworkServer(4117);
            server.RegisterHandler<SendMessage>(OnMessageReceived);

            client = new NetworkClient("127.0.0.1");
            client.RegisterHandler<SendMessage>(OnMessageReceived);

            Console.ReadLine();
        }

        static void OnMessageReceived(SendMessage message, IPEndPoint endPoint)
        {
            Console.WriteLine("[Server] Message Recieved: " + message.message);

            SendMessage sendMessage = new SendMessage("Hello Client! :D");
            server.Send(sendMessage, endPoint);
        }

        static void OnMessageReceived(SendMessage message)
        {
            Console.WriteLine("[Server] Message Recieved: " + message.message);

            SendMessage sendMessage = new SendMessage("Hello Client! :D");
            client.Send(sendMessage);
        }
    }
}