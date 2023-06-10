using Mimic;
using System;
using System.Net;

namespace MimicTest
{
    class Program
    {
        static NetworkServer server;

        static int totalBytesIn = 0;
        static int totalBytesOut = 0;

        static void Main(string[] args)
        {
            server = new NetworkServer(4117);
            server.RegisterHandler<SendMessage>(OnMessageReceived);

            NetworkDiagnostic.InMessageEvent += InMessage;
            NetworkDiagnostic.OutMessageEvent += OutMessage;

            Console.ReadLine();
        }

        static void InMessage(NetworkDiagnostic.MessageInfo info)
        {
            totalBytesIn += info.bytes;
            Console.WriteLine("[Message In Info] " +  info.bytes  + " | " + totalBytesIn);
        }

        static void OutMessage(NetworkDiagnostic.MessageInfo info)
        {
            totalBytesOut += info.bytes;
            Console.WriteLine("[Message Out Info] " + info.bytes + " | " + totalBytesOut);
        }

        static void OnMessageReceived(SendMessage message, IPEndPoint endPoint)
        {
            SendMessage sendMessage = new SendMessage("Hello Client! :D");
            server.Send(sendMessage, endPoint);
        }
    }
}