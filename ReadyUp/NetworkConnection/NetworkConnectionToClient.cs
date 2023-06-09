using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ReadyUp
{
    public class NetworkConnectionToClient : NetworkConnection
    {
        public NetworkConnectionToClient(Socket socket, IPEndPoint ipEndPoint) : base(socket, ipEndPoint)
        {
        }

        public NetworkConnectionToClient(Socket socket, string networkAddress, int port) : base(socket, networkAddress, port)
        {
        }

        public NetworkConnectionToClient(Socket socket, byte[] networkAddress, int port) : base(socket, networkAddress, port)
        {
        }
    }
}
