using System.Net;
using System.Net.Sockets;


namespace ReadyUp
{
    public class NetworkClient
    {
        static byte[] bufferSize = new byte[1024];

        public NetworkConnection clientConnection;
        public bool validConnection;

        Socket clientSocket => clientConnection.socket;

        internal static Dictionary<int, NetworkMessageDelegate> handlers = new Dictionary<int, NetworkMessageDelegate>();

        public NetworkClient(string address, int port = 4117)
        {
            Console.WriteLine("[Client] Starting NetworkClient...");
            clientConnection = new NetworkConnection(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), address, port, 0);
            clientConnection.SetHandler(handlers);

            RegisterDefaultHandlers();
            Connect();
        }

        public void Connect()
        {
            int attempts = 0;
            while (!clientSocket.Connected)
            {
                try
                {
                    attempts++;
                    clientSocket.Connect(IPAddress.Loopback, clientConnection.port);
                }
                catch (SocketException)
                {
                    Console.WriteLine("[Client] Connection attempts: " + attempts.ToString());
                }
            }

            Console.WriteLine("[Client] NetworkClient Connected to Server!");

            ReceiveLoop();
        }

        public void Disconnect()
        {
            if (!clientSocket.Connected)
                return;

            Console.WriteLine("[Client] Disconnecting Client");
            Send(new DisconnectMessage());

            clientSocket.Disconnect(false);
            validConnection = false;
        }

        void ReceiveLoop()
        {
            Console.WriteLine("[Client] Beginning Receive Loop!");

            while (clientSocket.Connected)
            {
                byte[] receiveBuffer = new byte[bufferSize.Length];
                int received = clientSocket.Receive(receiveBuffer);
                byte[] dataBuffer = new byte[received];
                Array.Copy(receiveBuffer, dataBuffer, received);

                clientConnection.OnReceivedData(dataBuffer);
            }
        }

        void RegisterDefaultHandlers()
        {
            Console.WriteLine("[Client] Registering Default Handlers");

            RegisterHandler<ConnectSuccessMessage>(OnConnectSuccessReceived, false);
        }

        void OnConnectSuccessReceived(ConnectSuccessMessage message, ushort senderID)
        {
            clientConnection.identifier = message.identity;
            validConnection = true;
        }

        #region Register/Unregister Handlers

        public void RegisterHandler(int messageType, NetworkMessageDelegate handler)
        {
            if (handlers.ContainsKey(messageType))
            {
                Console.WriteLine("[Client] NetworkClient.RegisterHandler replacing " + messageType);
            }
            handlers[messageType] = handler;
        }
        public void RegisterHandler<T>(Action<T, ushort> handler, bool requiredAuthentication = true) where T : struct, INetworkMessage
        {
            int messageType = MessagePacker.GetID<T>();
            if(handlers.ContainsKey(messageType))
            {
                Console.WriteLine("[Client] NetworkClient.RegisterHandler replacing " + messageType);
            }
            handlers[messageType] = MessagePacker.MessageHandler<T>(handler, requiredAuthentication);
        }

        public void UnregisterHandler(int messageType)
        {
            handlers.Remove(messageType);
        }
        public void UnregisterHandler<T>() where T : INetworkMessage
        {
            int messageType = MessagePacker.GetID<T>();
            handlers.Remove(messageType);
        }

        public void ClearHandlers() => handlers.Clear();

        #endregion

        public void Send<T>(T message) where T : INetworkMessage
        {
            if(clientSocket.Connected && validConnection)
            {
                byte[] toSend = MessagePacker.Pack(message, clientConnection);
                clientSocket.Send(toSend);
            }
        }
    }
}
