using System;
using System.Net.Sockets;

namespace Mimic
{
    public class NetworkClient : BaseClient
    {
        public NetworkClient(string address, int port = 4117)
        {
            Console.WriteLine("[Client] Starting NetworkClient...");

            clientConnection = new NetworkConnectionToServer(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), address, port);

            RegisterDefaultHandlers();

            Connect(serverEndPoint);
        }

        protected override void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                Socket socket = (Socket)result.AsyncState;
                int received = socket.EndReceive(result);

                byte[] dataBuffer = new byte[received];
                Array.Copy(globalBuffer, dataBuffer, received);

                if (dataBuffer.Length > 0)
                {
                    clientConnection.OnReceivedData(dataBuffer);
                    clientSocket.BeginReceive(globalBuffer, 0, globalBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), clientSocket);
                }
                else
                {
                    Console.WriteLine("[Client] Error: Received databuffer with a size of 0 | Client is disconnecting!");

                    clientConnection.Disconnect();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[Client] Receive Exception: " + e);
            }
        }

        void RegisterDefaultHandlers()
        {
#if DEBUG
            Console.WriteLine("DEBUG: [Client] Registering Default Handlers");
#endif

            RegisterHandler<ConnectSuccessMessage>(OnConnectSuccessReceived, false);
            RegisterHandler<PingMessage>(OnPingReceived, false);
            RegisterHandler<DisconnectMessage>(OnDisconnectionReceived, false);
        }

        void OnConnectSuccessReceived(ConnectSuccessMessage message)
        {
            if (validConnection)
                return;

            validConnection = true;
        }

        void OnPingReceived(PingMessage message)
        {
            Send(new PongMessage());
        }

        void OnDisconnectionReceived(DisconnectMessage message)
        {
#if DEBUG
            Console.WriteLine("DEBUG: [Client] Server Disconnected the Client!");
#endif

            clientConnection.Disconnect();
            validConnection = false;
        }

        public override void Send<T>(T message)
        {
            if(clientSocket.Connected && validConnection)
            {
                byte[] toSend = MessagePacker.Pack(message, clientConnection);

                NetworkDiagnostic.OnSend(message, toSend.Length);

                clientSocket.Send(toSend);
            }
            else
            {
                Console.WriteLine("[Client] Tried to send a message without being connected to a server! Check if the server is available if this happens");
            }
        }

        ~NetworkClient()
        {
            Disconnect();
        }
    }
}
