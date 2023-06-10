using System.Net.Sockets;
using System.Net;

namespace Mimic
{
    public partial class BaseClient
    {
        protected static byte[] globalBuffer = new byte[1024];

        public NetworkConnectionToServer clientConnection;
        public bool validConnection;

        protected Socket clientSocket => clientConnection.socket;
        protected EndPoint endPoint;

        public bool isConnected => clientSocket.Connected;
        //public bool isConnected => !((clientSocket.Poll(1000, SelectMode.SelectRead) && (clientSocket.Available == 0)) || !clientSocket.Connected);

        ~BaseClient()
        {
            clientSocket.Close();
        }
    }
}
