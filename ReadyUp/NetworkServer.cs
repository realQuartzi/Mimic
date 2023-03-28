using System.Net;
using System.Net.Sockets;
using System.Timers;

namespace ReadyUp
{
    public class NetworkServer
    {
        static byte[] globalBuffer = new byte[1024];
        int multiListenCount = 1;

        public static Dictionary<Guid, NetworkConnection> clientConnections = new Dictionary<Guid, NetworkConnection>();

        public NetworkConnection serverConnection;
        public Socket serverSocket => serverConnection.socket;

        // Default timeout Time for a client is 30 seconds (30,000 milliseconds)
        public int clientTimeOut = 30000;
        // Default time for ping to be requested to all clients (20 seconds)
        public int pingRequestTime = 20000;

        public NetworkServer(int port = 4117)
        {
            Console.WriteLine("[Server] Starting NetworkServer...");
            Console.WriteLine("[Server] Set Listening port to: " + port);

            Console.WriteLine("[Server] Setting up Socket...");
            serverConnection = new NetworkConnection(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), "localhost", 4117, Guid.Empty);
            serverConnection.isServer = true;

            RegisterDefaultHandlers();

            SetupServer();
        }

        void SetupServer()
        {
            // Open Listen to all Connections
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, serverConnection.port));
            serverSocket.Listen(multiListenCount); // Only allow one Client accept at a time

            // Start Accepting new Clients
            serverSocket.BeginAccept(new AsyncCallback(AcceptConnectionCallback), null);

            System.Timers.Timer pingTimer = new System.Timers.Timer();
            pingTimer.Interval = pingRequestTime;
            pingTimer.Elapsed += SendGlobalPing;
            pingTimer.Start();

            System.Timers.Timer timeOutTimer = new System.Timers.Timer();
            timeOutTimer.Interval = clientTimeOut;
            timeOutTimer.Elapsed += CheckClientTimeOut;
            timeOutTimer.Start();
        }

        void AcceptConnectionCallback(IAsyncResult result)
        {
            Socket socket = serverSocket.EndAccept(result);

            Console.WriteLine("[Server] New Client Connected!");
            Guid newGUID = Guid.NewGuid();
            EndPoint endPoint = socket.RemoteEndPoint;
            NetworkConnection newClientConnection = new NetworkConnection(socket, endPoint as IPEndPoint, newGUID);
            clientConnections.Add(newGUID, newClientConnection);

            socket.BeginReceiveFrom(globalBuffer, 0, globalBuffer.Length, SocketFlags.None,ref endPoint, new AsyncCallback(ReceiveCallback), socket);

            // Send Identification before adding to connected List
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
            EndPoint endPoint = socket.RemoteEndPoint;
            int received = socket.EndReceiveFrom(result, ref endPoint);

            byte[] dataBuffer = new byte[received];
            Array.Copy(globalBuffer, dataBuffer, received);

            if(dataBuffer.Length > 0 )
            {
                serverConnection.OnReceivedData(dataBuffer);
                socket.BeginReceiveFrom(globalBuffer, 0, globalBuffer.Length, SocketFlags.None, ref endPoint, new AsyncCallback(ReceiveCallback), socket);
            }
        }

        void RegisterDefaultHandlers()
        {
            Console.WriteLine("[Server] Registering Default Handlers");

            RegisterHandler<DisconnectMessage>(DisconnectReceived, false);
            RegisterHandler<PongMessage>(PongReceived, false);
        }

        void PongReceived(PongMessage message, Guid senderID) {}

        void DisconnectReceived(DisconnectMessage message, Guid senderID)
        {
            Console.WriteLine("[Server] Removing Disconnected Client: " + senderID);
            clientConnections.Remove(senderID);
        }

        #region Register/Unregister Handlers

        public void RegisterHandler(int messageType, NetworkMessageDelegate handler) => serverConnection.RegisterHandler(messageType, handler);
        public void RegisterHandler<T>(Action<T, Guid> handler, bool requiredAuthentication = true) where T : struct, INetworkMessage => serverConnection.RegisterHandler<T>(handler, requiredAuthentication);

        public void UnregisterHandler(int messageType) => serverConnection.UnregisterHandler(messageType);
        public void UnregisterHandler<T>() where T : INetworkMessage => serverConnection.UnregisterHandler<T>();

        public void ClearHandlers() => serverConnection.ClearHandlers();

        #endregion

        public void Send<T>(T message, Guid identifier) where T : INetworkMessage
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
            foreach (KeyValuePair<Guid, NetworkConnection> conn in clientConnections)
            {
                Console.WriteLine("Sending to: " + conn.Key.ToString() + " | " + conn.Value.ipEndPoint.ToString());
                conn.Value.socket.BeginSend(toSend, 0, toSend.Length, SocketFlags.None, new AsyncCallback(SendCallback), conn.Value.socket);
            }
        }

        void SendCallback(IAsyncResult result)
        {
            Socket socket = (Socket)result.AsyncState;
            socket.EndSend(result);
        }

        void SendGlobalPing(object? sender, ElapsedEventArgs e)
        {
            SendToAll(new PingMessage());
        }
        void CheckClientTimeOut(object? sender, ElapsedEventArgs e)
        {
            if (clientConnections.Count <= 0)
                return;

            foreach (KeyValuePair<Guid, NetworkConnection> conn in clientConnections)
            {
                // Compare ticks with Milliseconds (Multiply Milliseconds by 10,000 to get ticks compare)
                if (conn.Value.lastMessageTime + (clientTimeOut * 10000) <= DateTime.UtcNow.Ticks)
                {
                    DisconnectConnection(conn.Key);
                }
            }
        }
        void DisconnectConnection(Guid connectionID)
        {
            Send(new DisconnectMessage(), connectionID);
            if(clientConnections.ContainsKey(connectionID))
            {
                clientConnections.Remove(connectionID);
            }
        }
    }
}