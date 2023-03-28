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
        EndPoint endPoint;

        public NetworkClient(string address, int port = 4117)
        {
            Console.WriteLine("[Client] Starting NetworkClient...");
            clientConnection = new NetworkConnection(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), address, port, Guid.Empty);
            endPoint = clientConnection.ipEndPoint;

            RegisterDefaultHandlers();
            Connect(endPoint);
        }

        public void Connect(EndPoint endPoint)
        {
            int attempts = 0;
            while (!clientSocket.Connected)
            {
                try
                {
                    attempts++;
                    clientSocket.Connect(endPoint);
                }
                catch (SocketException)
                {
                    Console.WriteLine("[Client] Connection attempts: " + attempts.ToString());
                }
            }

            Console.WriteLine("[Client] NetworkClient Connected to Server!");
            Console.WriteLine("[Client] NetworkClient Listening to: " + clientConnection.address);
            clientSocket.BeginReceiveFrom(globalBuffer, 0, globalBuffer.Length, SocketFlags.None, ref endPoint, new AsyncCallback(ReceiveCallback), clientSocket);
        }

        void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                Socket socket = (Socket)result.AsyncState;
                int received = socket.EndReceiveFrom(result, ref endPoint);

                byte[] dataBuffer = new byte[received];
                Array.Copy(globalBuffer, dataBuffer, received);

                if (dataBuffer.Length > 0)
                {
                    clientConnection.OnReceivedData(dataBuffer);
                    clientSocket.BeginReceiveFrom(globalBuffer, 0, globalBuffer.Length, SocketFlags.None, ref endPoint, new AsyncCallback(ReceiveCallback), clientSocket);

                }
                else
                {
                    Console.WriteLine("[Client] Error: databuffer is 0 size");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[Client] Receive Exception: " + e);
            }

        }

        void RegisterDefaultHandlers()
        {
            Console.WriteLine("[Client] Registering Default Handlers");

            RegisterHandler<ConnectSuccessMessage>(OnConnectSuccessReceived, false);
            RegisterHandler<PingMessage>(OnPingReceived, false);
            RegisterHandler<DisconnectMessage>(OnDisconnectionReceived, false);
        }

        void OnConnectSuccessReceived(ConnectSuccessMessage message, Guid senderID)
        {
            if (validConnection)
                return;

            clientConnection.identifier = message.identity;
            validConnection = true;
        }

        void OnPingReceived(PingMessage message, Guid senderID)
        {
            Send(new PongMessage());
        }

        void OnDisconnectionReceived(DisconnectMessage message, Guid senderID)
        {
            Console.WriteLine("[Client] Server Disconnected the Client!");

            clientSocket.Disconnect(false);
            clientConnection.identifier = Guid.Empty;
            validConnection = false;
        }

        #region Register/Unregister Handlers

        public void RegisterHandler(int messageType, NetworkMessageDelegate handler) => clientConnection.RegisterHandler(messageType, handler);
        public void RegisterHandler<T>(Action<T, Guid> handler, bool requiredAuthentication = true) where T : struct, INetworkMessage => clientConnection.RegisterHandler<T>(handler, requiredAuthentication);

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

        public void Disconnect()
        {
            if (!clientSocket.Connected)
                return;

            Console.WriteLine("[Client] Disconnecting Client");
            Send(new DisconnectMessage());

            clientSocket.Disconnect(false);
            clientConnection.identifier = Guid.Empty;
            validConnection = false;
        }
    }
}
