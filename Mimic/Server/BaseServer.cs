using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mimic
{
    public abstract partial class BaseServer
    {
        protected static byte[] globalBuffer = new byte[1024];
        protected int multiListenCount = 10;

        /// <summary>
        /// Dictionary containing all currently active connections.
        /// </summary>
        public static ConcurrentDictionary<IPEndPoint, NetworkConnectionToClient> clientConnections = new ConcurrentDictionary<IPEndPoint, NetworkConnectionToClient>();

        public NetworkConnection serverConnection;
        public Socket serverSocket => serverConnection.socket;

        // Default timeout Time for a client is 5 seconds (5,000 milliseconds)
        /// <summary>
        /// Timeout for clients in milliseconds. Checks time from last message received.
        /// </summary>
        public int clientTimeOut = 10000;
        protected Timer timeoutTimer;

        // Default time for ping to be requested to all clients (5 seconds)
        /// <summary>
        /// Time in milliseconds for frequency of checking client activity. (Ping)
        /// </summary>
        public int pingRequestTime = 5000;
        protected Timer pingTimer;

        // Setup the Server
        // Associate the socket with a local endpoint
        // Set the socket in a listening state
        // Start accepting incoming connection attempts
        // Start the Timers for Ping and Timeouts
        /// <summary>
        /// Start the Server by associating with a local endpoint.
        /// </summary>
        public virtual void StartServer()
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

        /// <summary>
        /// Accept and handle the incomming connection request.
        /// </summary>
        /// <param name="result"></param>
        protected abstract void AcceptConnectionCallback(IAsyncResult result);

        /// <summary>
        /// Send the message type T to a specified connection IPEndPoint
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <param name="ipEndPoint"></param>
        public abstract void Send<T>(T message, IPEndPoint ipEndPoint) where T : INetworkMessage;

        /// <summary>
        /// Send the message type T to all active connections.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        public abstract void SendToAll<T>(T message) where T : INetworkMessage;

        protected void SendCallback(IAsyncResult result)
        {
            Socket socket = (Socket)result.AsyncState;
            socket.EndSend(result);
        }

        /// <summary>
        /// Sends a ping to all active connections.
        /// </summary>
        /// <param name="sender"></param>
        protected void SendGlobalPing(object sender) => SendToAll(new PingMessage());

        /// <summary>
        /// Check if any connected clients have not returned a message in the given time out time.
        /// </summary>
        /// <param name="sender"></param>
        protected virtual void CheckClientTimeOut(object sender)
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

        /// <summary>
        /// Disconnect a specified connection given its IPEndPoint.
        /// </summary>
        /// <param name="connectionIP"></param>
        protected virtual void DisconnectConnection(IPEndPoint connectionIP)
        {
#if DEBUG
            Console.WriteLine($"DEBUG: [Server] Connection: {connectionIP.Address.ToString()} Disconnected");
#endif

            Send(new DisconnectMessage(), connectionIP);

            if (clientConnections.ContainsKey(connectionIP))
            {
                NetworkConnectionToClient conn;
                clientConnections.Remove(connectionIP, out conn);
                conn.Disconnect();
            }
        }

        /// <summary>
        /// Disconnect all connections.
        /// </summary>
        protected virtual void DisconnectAll()
        {
#if DEBUG
            Console.WriteLine($"DEBUG: [Server] Disconnected all Connections!");
#endif

            foreach(NetworkConnectionToClient conn in clientConnections.Values.ToList())
            {
                DisconnectConnection(conn.ipEndPoint);
            }

            clientConnections.Clear();
        }

        #region Register/Unregister Handlers [Getter Extension]

        /// <summary>
        /// Register a handler for message type using its stable hash.
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="handler"></param>
        public void RegisterHandler(int messageType, NetworkMessageDelegate handler) => serverConnection.RegisterHandler(messageType, handler);

        /// <summary>
        /// Register a handler for a message type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        /// <param name="requiredAuthentication"></param>
        public void RegisterHandler<T>(Action<T, IPEndPoint> handler, bool requiredAuthentication = true) where T : struct, INetworkMessage => serverConnection.RegisterHandler<T>(handler, requiredAuthentication);

        /// <summary>
        /// Unregister a handler for a message type using its stable hash ID.
        /// </summary>
        /// <param name="messageType"></param>
        public void UnregisterHandler(int messageType) => serverConnection.UnregisterHandler(messageType);

        /// <summary>
        /// Unregister a handler for a message type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void UnregisterHandler<T>() where T : INetworkMessage => serverConnection.UnregisterHandler<T>();

        /// <summary>
        /// Clears all registered message handlers.
        /// </summary>
        public void ClearHandlers() => serverConnection.ClearHandlers();

        #endregion

        /// <summary>
        /// Get the NetworkConnectionToClient of the specified IPEndPoint.
        /// </summary>
        /// <param name="endPoint"></param>
        /// <param name="conn"></param>
        /// <returns></returns>
        internal bool GetNetworkConnectionToClient(IPEndPoint endPoint, out NetworkConnectionToClient conn)
        {
            if(!clientConnections.TryGetValue(endPoint, out conn))
            {
                Console.WriteLine("Error: [Server] IPEndPoint does not have an assigned Connection.");
                return false;
            }

            return true;
        }
    }
}
