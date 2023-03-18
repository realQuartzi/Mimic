using System.Net;
using System.Net.Sockets;

namespace ReadyUp
{
    public class NetworkServer
    {
        static byte[] globalBuffer = new byte[1024];
        int multiListenCount = 1;
        int listenPort;

        Dictionary<ushort, NetworkConnection> clientConnections = new Dictionary<ushort, NetworkConnection>();

        public NetworkConnection serverConnection;
        public Socket serverSocket => serverConnection.socket;

        Dictionary<int, NetworkMessageDelegate> handlers = new Dictionary<int, NetworkMessageDelegate>();

        ushort connectID = 1;

        public NetworkServer(int port = 4117)
        {
            Console.WriteLine("[Server] Starting NetworkServer...");
            Console.WriteLine("[Server] Set Listening port to: " + port);
            listenPort = port;

            Console.WriteLine("[Server] Setting up Socket...");
            serverConnection = new NetworkConnection(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), "localhost", 4117, 0);
            serverConnection.SetHandler(handlers);

            RegisterDefaultHandlers();

            SetupServer();
        }

        void SetupServer()
        {
            // Open Listen to all Connections
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, listenPort));
            serverSocket.Listen(multiListenCount); // Only allow one Client accept at a time

            // Start Accepting new Clients
            serverSocket.BeginAccept(new AsyncCallback(AcceptConnectionCallback), null);
        }

        void AcceptConnectionCallback(IAsyncResult result)
        {
            Socket socket = serverSocket.EndAccept(result);

            Console.WriteLine("[Server] Current ConnectID: " + connectID);
            NetworkConnection newClientConnection = new NetworkConnection(socket, "localhost", listenPort, connectID);
            clientConnections.Add(connectID, newClientConnection);
            connectID++;

            socket.BeginReceive(globalBuffer, 0, globalBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);

            ConnectSuccessMessage message = new ConnectSuccessMessage()
            {
                identity = newClientConnection.identifier,
            };

            Send(message, newClientConnection.identifier);

            // Start Accepting new Clients Again
            serverSocket.BeginAccept(new AsyncCallback(AcceptConnectionCallback), null);
        }

        void ReceiveCallback(IAsyncResult result)
        {
            Socket socket = (Socket)result.AsyncState;
            int received = socket.EndReceive(result);

            byte[] dataBuffer = new byte[received];
            Array.Copy(globalBuffer, dataBuffer, received);

            serverConnection.OnReceivedData(dataBuffer);
        }

        void RegisterDefaultHandlers()
        {
            Console.WriteLine("[Server] Registering Default Handlers");

            RegisterHandler<DisconnectMessage>(DisconnectReceived, false);
            RegisterHandler<PingMessage>(PingReceived, false);
        }

        void PingReceived(PingMessage message, ushort senderID)
        {
            Console.WriteLine("[Server] Ping Received from Client");

            Send(new PongMessage(), senderID);
        }

        void DisconnectReceived(DisconnectMessage message, ushort senderID)
        {
            Console.WriteLine("[Server] Removing Client: " + senderID);
            clientConnections.Remove(senderID);
        }

        public void RegisterHandler(int messageType, NetworkMessageDelegate handler)
        {
            if (handlers.ContainsKey(messageType))
            {
                Console.WriteLine("[Server] NetworkServer.RegisterHandler replacing " + messageType);
            }
            handlers[messageType] = handler;
        }
        public void RegisterHandler<T>(Action<T, ushort> handler, bool requiredAuthentication = true) where T : struct, INetworkMessage
        {
            int messageType = MessagePacker.GetID<T>();
            if(handlers.ContainsKey(messageType))
            {
                Console.WriteLine("[Server] NetworkServer.RegisterHandler replacing " + messageType);
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

        public void Send<T>(T message, ushort identifier) where T : INetworkMessage
        {
            NetworkConnection conn = null;
            if(clientConnections.TryGetValue(identifier, out conn))
            {
                byte[] toSend = MessagePacker.Pack(message);
                conn.socket.BeginSend(toSend, 0, toSend.Length, SocketFlags.None, new AsyncCallback(SendCallback), conn.socket);
            }
        }
        public void SendToAll<T>(T message) where T : INetworkMessage
        {
            if (clientConnections.Count <= 0)
                return;

            byte[] toSend = MessagePacker.Pack(message);
            foreach (KeyValuePair<ushort, NetworkConnection> conn in clientConnections)
            {
                conn.Value.socket.BeginSend(toSend, 0, toSend.Length, SocketFlags.None, new AsyncCallback(SendCallback), conn.Value.socket);
            }
        }

        void SendCallback(IAsyncResult result)
        {
            Socket socket = (Socket)result.AsyncState;
            socket.EndSend(result);
        }
    }
}