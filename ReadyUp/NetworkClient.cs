using System.Net;
using System.Net.Sockets;

namespace ReadyUp
{
    public class NetworkClient
    {
        static byte[] globalBuffer = new byte[1024];

        public NetworkConnection clientConnection;
        public bool validConnection;

        Socket clientSocket => clientConnection.socket;
        EndPoint endPoint;

        public NetworkClient(string address, int port = 4117)
        {
            Console.WriteLine("[Client] Starting NetworkClient...");
            clientConnection = new NetworkConnection(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), address, port, 0);
            endPoint = clientConnection.ipEndPoint;

            RegisterDefaultHandlers();
            Connect(endPoint);
        }

        public void Connect(EndPoint endPoint)
        {
            int attempts = 0;
            while (!clientSocket.Connected)
            {
                try
                {
                    attempts++;
                    clientSocket.Connect(endPoint);
                }
                catch (SocketException)
                {
                    Console.WriteLine("[Client] Connection attempts: " + attempts.ToString());
                }
            }

            Console.WriteLine("[Client] NetworkClient Connected to Server!");
            Console.WriteLine("[Client] NetworkClient Listening to: " + clientConnection.address);
            clientSocket.BeginReceiveFrom(globalBuffer, 0, globalBuffer.Length, SocketFlags.None, ref endPoint, new AsyncCallback(ReceiveCallback), clientSocket);
        }

        public void Disconnect()
        {
            if (!clientSocket.Connected)
                return;

            Console.WriteLine("[Client] Disconnecting Client");
            Send(new DisconnectMessage());

            clientSocket.Disconnect(false);
            validConnection = false;
        }

        void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                Socket socket = (Socket)result.AsyncState;
                Console.WriteLine("Result: " + result);
                int received = socket.EndReceiveFrom(result, ref endPoint);

                byte[] dataBuffer = new byte[received];
                Array.Copy(globalBuffer, dataBuffer, received);

                clientConnection.OnReceivedData(dataBuffer);
            }
            catch(Exception e)
            {
                Console.WriteLine("[Client] Receive Exception: " + e);
            }

        }

        void RegisterDefaultHandlers()
        {
            Console.WriteLine("[Client] Registering Default Handlers");

            RegisterHandler<ConnectSuccessMessage>(OnConnectSuccessReceived, false);
        }

        void OnConnectSuccessReceived(ConnectSuccessMessage message, ushort senderID)
        {
            Console.WriteLine("[Client] Connect Success Message Received!");
            clientConnection.identifier = message.identity;
            validConnection = true;
        }

        #region Register/Unregister Handlers

        public void RegisterHandler(int messageType, NetworkMessageDelegate handler) => clientConnection.RegisterHandler(messageType, handler);
        public void RegisterHandler<T>(Action<T, ushort> handler, bool requiredAuthentication = true) where T : struct, INetworkMessage => clientConnection.RegisterHandler<T>(handler, requiredAuthentication);

        public void UnregisterHandler(int messageType) => clientConnection.UnregisterHandler(messageType);
        public void UnregisterHandler<T>() where T : INetworkMessage => clientConnection.UnregisterHandler<T>();

        public void ClearHandlers() => clientConnection.ClearHandlers();

        #endregion

        public void Send<T>(T message) where T : INetworkMessage
        {
            if(clientSocket.Connected && validConnection)
            {
                byte[] toSend = MessagePacker.Pack(message, clientConnection);
                clientSocket.Send(toSend);
            }
        }
    }
}
