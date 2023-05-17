using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace ReadyUp
{
    public partial class BaseServer
    {
        protected static byte[] globalBuffer = new byte[1024];
        protected int multiListenCount = 1;

        public static Dictionary<Guid, NetworkConnection> clientConnections = new Dictionary<Guid, NetworkConnection>();

        public NetworkConnection serverConnection;
        public Socket serverSocket => serverConnection.socket;

        // Default timeout Time for a client is 30 seconds (30,000 milliseconds)
        /// <summary>
        /// Timeout for clients in milliseconds. Checks time from last message received.
        /// </summary>
        public int clientTimeOut = 30000;

        // Default time for ping to be requested to all clients (20 seconds)
        /// <summary>
        /// Time in milliseconds for frequency of checking client activity. (Ping)
        /// </summary>
        public int pingRequestTime = 20000;
    }
}
