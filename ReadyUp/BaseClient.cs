using System.Net.Sockets;
using System.Net;

namespace ReadyUp
{
    public partial class BaseClient
    {
        protected static byte[] globalBuffer = new byte[1024];

        public NetworkConnection clientConnection;
        public bool validConnection;

        protected Socket clientSocket => clientConnection.socket;
        protected EndPoint endPoint;
    }
}
