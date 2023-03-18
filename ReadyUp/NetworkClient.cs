using System.Net;
using System.Net.Sockets;


namespace ReadyUp
{
    public class NetworkClient
    {
        static byte[] globalBuffer = new byte[1024];

        public NetworkConnection clientConnection;
        public bool validConnection;

        Socket clientSocket => clientConnection.socket;

        public NetworkClient(string address, int port = 4117)
        {
            Console.WriteLine("[Client] Starting NetworkClient...");
            clientConnection = new NetworkConnection(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), address, port, 0);

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

            clientSocket.BeginReceive(globalBuffer, 0, globalBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), clientSocket);
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

        void ReceiveCallback(IAsyncResult result)
        {
            Socket socket = (Socket)result.AsyncState;
            int received = socket.EndReceive(result);

            byte[] dataBuffer = new byte[received];
            Array.Copy(globalBuffer, dataBuffer, received);

            clientConnection.OnReceivedData(dataBuffer);
        }

        void RegisterDefaultHandlers()
        {
            Console.WriteLine("[Client] Registering Default Handlers");

            RegisterHandler<ConnectSuccessMessage>(OnConnectSuccessReceived, false);
        }

        void OnConnectSuccessReceived(ConnectSuccessMessage message, ushort senderID)
        {
            Console.WriteLine("[Client] Connect Success Message Received!");
            clientConnection.identifier = message.identity;
            validConnection = true;
        }

        #region Register/Unregister Handlers

        public void RegisterHandler(int messageType, NetworkMessageDelegate handler) => clientConnection.RegisterHandler(messageType, handler);
        public void RegisterHandler<T>(Action<T, ushort> handler, bool requiredAuthentication = true) where T : struct, INetworkMessage => clientConnection.RegisterHandler<T>(handler, requiredAuthentication);

        public void UnregisterHandler(int messageType) => clientConnection.UnregisterHandler(messageType);
        public void UnregisterHandler<T>() where T : INetworkMessage => clientConnection.UnregisterHandler<T>();

        public void ClearHandlers() => clientConnection.ClearHandlers();

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
