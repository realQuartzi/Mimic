using System.Net;
using System.Net.Sockets;

namespace Mimic
{
    public class NetworkConnectionToServer : NetworkConnection
    {
        public NetworkConnectionToServer(Socket socket, IPEndPoint ipEndPoint) : base(socket, ipEndPoint)
        {
        }

        public NetworkConnectionToServer(Socket socket, string networkAddress, int port) : base(socket, networkAddress, port)
        {
        }

        public NetworkConnectionToServer(Socket socket, byte[] networkAddress, int port) : base(socket, networkAddress, port)
        {
        }
    }
}
