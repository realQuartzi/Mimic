using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace ReadyUp
{
    public partial class BaseServer
    {
        protected static byte[] globalBuffer = new byte[1024];
        protected int multiListenCount = 10;

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
    }
}
