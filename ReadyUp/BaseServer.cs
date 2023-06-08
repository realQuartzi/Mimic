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

        // Default timeout Time for a client is 5 seconds (5,000 milliseconds)
        /// <summary>
        /// Timeout for clients in milliseconds. Checks time from last message received.
        /// </summary>
        public int clientTimeOut = 10000;

        // Default time for ping to be requested to all clients (5 seconds)
        /// <summary>
        /// Time in milliseconds for frequency of checking client activity. (Ping)
        /// </summary>
        public int pingRequestTime = 5000;
    }
}
