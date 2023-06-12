using Mimic;
using System;
using System.Net;

namespace MimicTest
{
    class Program
    {
        static NetworkKeyServer keyServer;
        static NetworkKeyClient keyClient;

        static int totalBytesIn = 0;
        static int totalBytesOut = 0;

        static void Main(string[] args)
        {
            keyServer = new NetworkKeyServer(4117);
            keyServer.RegisterHandler<SendMessage>(OnMessageReceived);

            keyClient = new NetworkKeyClient("127.0.0.1");

            NetworkDiagnostic.InMessageEvent += InMessage;
            NetworkDiagnostic.OutMessageEvent += OutMessage;

            Console.ReadLine();
        }

        static void InMessage(NetworkDiagnostic.MessageInfo info)
        {
            totalBytesIn += info.bytes;
        }

        static void OutMessage(NetworkDiagnostic.MessageInfo info)
        {
            totalBytesOut += info.bytes;
        }

        static void OnMessageReceived(SendMessage message, IPEndPoint endPoint)
        {
            SendMessage sendMessage = new SendMessage("Hello Client! :D");
            keyServer.Send(sendMessage, endPoint);
        }
    }
}