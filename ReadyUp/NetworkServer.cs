using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ReadyUp
{
    public class NetworkServer : BaseServer
    {
        /// <summary>
        /// Create and Start a new NetworkServer which will listen to the given port
        /// </summary>
        /// <param name="port"></param>
        public NetworkServer(int port = 4117)
        {
            Console.WriteLine("[Server] Starting NetworkServer...");
            Console.WriteLine("[Server] Set Listening port to: " + port);

#if DEBUG
            Console.WriteLine("DEBUG: [Server] Setting up Socket...");
#endif

            serverConnection = new NetworkConnection(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), "localhost", port);
            serverConnection.isServer = true;

            RegisterDefaultHandlers();

            SetupServer();
        }

        // Setup the Server
        // Associate the socket with a local endpoint
        // Set the socket in a listening state
        // Start accepting incoming connection attempts
        // Start the Timers for Ping and Timeouts
        void SetupServer()
        {
            // Open Listen to all Connections
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, serverConnection.port));
            serverSocket.Listen(multiListenCount); // Only allow one Client accept at a time

            Console.WriteLine("[Server] Started listening on port: " + serverConnection.port);

            // Start Accepting new Clients
            serverSocket.BeginAccept(new AsyncCallback(AcceptConnectionCallback), null);

            pingTimer = new Timer(SendGlobalPing, null, 0, pingRequestTime);
            Console.WriteLine("[Server] Ping Interval set to: " + pingRequestTime.ToString("0ms"));

            timeoutTimer = new Timer(CheckClientTimeOut, null, 0, clientTimeOut);
            Console.WriteLine("[Server] Connection Timeout Interval set to: " + clientTimeOut.ToString("0ms"));
        }

        // Accept the incomming connection
        // Return a Succesful Connection message
        void AcceptConnectionCallback(IAsyncResult result)
        {
            Socket clientSocket = serverSocket.EndAccept(result);

#if DEBUG
            Console.WriteLine("DEBUG: [Server] New Client Connected!");
#endif

            IPEndPoint clientIPEndPoint = (IPEndPoint)clientSocket.RemoteEndPoint;
            string clientIP = clientIPEndPoint.Address.ToString();
            int clientPort = clientIPEndPoint.Port;

            NetworkConnectionToClient clientConnection = new NetworkConnectionToClient(clientSocket, clientIPEndPoint);
            clientConnections.TryAdd(clientIPEndPoint, clientConnection);

            clientSocket.BeginReceive(globalBuffer, 0, globalBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), clientSocket);

            ConnectSuccessMessage message = new ConnectSuccessMessage();

            Send(message, clientIPEndPoint);

            // Start Accepting new Clients Again
            serverSocket.BeginAccept(new AsyncCallback(AcceptConnectionCallback), null);
        }

        // Handle the received data sent by a connection
        void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                Socket clientSocket = (Socket)result.AsyncState;
                IPEndPoint clientIPEndPoint = (IPEndPoint)clientSocket.RemoteEndPoint;

                int received = clientSocket.EndReceive(result);

                byte[] dataBuffer = new byte[received];
                Array.Copy(globalBuffer, dataBuffer, received);

                if (dataBuffer.Length > 0)
                {
                    serverConnection.OnReceivedData(dataBuffer, clientIPEndPoint);
                    if(clientSocket.Connected)
                    {
                        clientSocket.BeginReceive(globalBuffer, 0, globalBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), clientSocket);
                    }
                }
                else
                {
                    Console.WriteLine("[Server] Error: Received databuffer with a size of 0 | Client is being disonnected!");

                    NetworkConnectionToClient conn;
                    clientConnections.TryRemove(clientIPEndPoint, out conn);

                    conn.Disconnect();
                    return;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[Server] Receive Exception: " + e);
            }

        }

        void RegisterDefaultHandlers()
        {
#if DEBUG
            Console.WriteLine("DEBUG: [Server] Registering Default Handlers");
#endif

            RegisterHandler<DisconnectMessage>(DisconnectReceived, false);
            RegisterHandler<PongMessage>(PongReceived, false);
        }

        void PongReceived(PongMessage message, IPEndPoint endpoint)
        {
#if DEBUG
            Console.WriteLine("DEBUG: [Server] Pong Recieved: " + endpoint.Address.ToString());
#endif
        }

        void DisconnectReceived(DisconnectMessage message, IPEndPoint endpoint)
        {
#if DEBUG
            Console.WriteLine("DEBUG: [Server] Removing Disconnected Client: " + endpoint);
#endif

            NetworkConnectionToClient conn;
            clientConnections.TryRemove(endpoint, out conn);

            conn.Disconnect();
        }

        public void Send<T>(T message, IPEndPoint ipEndPoint) where T : INetworkMessage
        {
            NetworkConnectionToClient conn = null;
            if(clientConnections.TryGetValue(ipEndPoint, out conn))
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
            foreach (KeyValuePair<IPEndPoint, NetworkConnectionToClient> conn in clientConnections)
            {
#if DEBUG
                Console.WriteLine("DEBUG: [Server] Sending to: " + conn.Key.ToString());
#endif
                conn.Value.socket.BeginSend(toSend, 0, toSend.Length, SocketFlags.None, new AsyncCallback(SendCallback), conn.Value.socket);
            }
        }

        void SendCallback(IAsyncResult result)
        {
            Socket socket = (Socket)result.AsyncState;
            socket.EndSend(result);
        }

        void SendGlobalPing(object sender) => SendToAll(new PingMessage());

        void CheckClientTimeOut(object sender)
        {
            if (clientConnections.Count <= 0)
                return;

            foreach (KeyValuePair<IPEndPoint, NetworkConnectionToClient> conn in clientConnections)
            {
                // Compare ticks with Milliseconds (Multiply Milliseconds by 10,000 to get ticks compare)
                if (conn.Value.lastMessageTime + (clientTimeOut * 10000) <= DateTime.UtcNow.Ticks)
                {
                    Console.WriteLine($"Connection ID: {conn.Key} Disconnecting... Timeout");
                    DisconnectConnection(conn.Key);
                }
            }
        }

        void DisconnectConnection(IPEndPoint connectionIP)
        {
#if DEBUG
            Console.WriteLine($"DEBUG: [Server] Connection: {connectionIP.Address.ToString()} Disconnected");
#endif
            Send(new DisconnectMessage(), connectionIP);

            if(clientConnections.ContainsKey(connectionIP))
            {
                NetworkConnectionToClient conn;
                clientConnections.Remove(connectionIP, out conn);
                conn.Disconnect();
            }
        }

        #region Register/Unregister Handlers [Getter Extension]

        public void RegisterHandler(int messageType, NetworkMessageDelegate handler) => serverConnection.RegisterHandler(messageType, handler);
        public void RegisterHandler<T>(Action<T, IPEndPoint> handler, bool requiredAuthentication = true) where T : struct, INetworkMessage => serverConnection.RegisterHandler<T>(handler, requiredAuthentication);

        public void UnregisterHandler(int messageType) => serverConnection.UnregisterHandler(messageType);
        public void UnregisterHandler<T>() where T : INetworkMessage => serverConnection.UnregisterHandler<T>();

        public void ClearHandlers() => serverConnection.ClearHandlers();

        #endregion
    }
}