using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Mimic
{
    public class NetworkClient : BaseClient
    {
        public NetworkClient(string address, int port = 4117)
        {
            Console.WriteLine("[Client] Starting NetworkClient...");

            clientConnection = new NetworkConnectionToServer(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), address, port);
            endPoint = clientConnection.ipEndPoint;

            RegisterDefaultHandlers();
            Connect(endPoint);
        }

        public async void Connect(EndPoint endPoint, int maxAttempts = 10, int retryDelay = 1000)
        {
            int attempts = 0;

            while (attempts < maxAttempts)
            {
                try
                {
                    attempts++;
                    await clientSocket.ConnectAsync(endPoint);
                    break;
                }
                catch (SocketException)
                {
#if DEBUG
                    Console.WriteLine("DEBUG: [Client] Connection attempts: " + attempts.ToString());
#endif
                    await Task.Delay(retryDelay);
                }
            }

            if(clientSocket.Connected)
            {
                Console.WriteLine("[Client] NetworkClient connected to Server!");
                Console.WriteLine("[Client] NetworkClient listening to: " + clientConnection.address);

                clientSocket.BeginReceiveFrom(globalBuffer, 0, globalBuffer.Length, SocketFlags.None, ref endPoint, new AsyncCallback(ReceiveCallback), clientSocket);
            }
            else
            {
                Console.WriteLine("[Client] Failed to connect to the server.");
            }
        }

        void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                Socket socket = (Socket)result.AsyncState;
                int received = socket.EndReceiveFrom(result, ref endPoint);

                byte[] dataBuffer = new byte[received];
                Array.Copy(globalBuffer, dataBuffer, received);

                if (dataBuffer.Length > 0)
                {
                    clientConnection.OnReceivedData(dataBuffer);
                    clientSocket.BeginReceiveFrom(globalBuffer, 0, globalBuffer.Length, SocketFlags.None, ref endPoint, new AsyncCallback(ReceiveCallback), clientSocket);
                }
                else
                {
                    Console.WriteLine("[Client] Error: Received databuffer with a size of 0 | Client is disconnecting!");

                    clientConnection.Disconnect();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[Client] Receive Exception: " + e);
            }

        }

        void RegisterDefaultHandlers()
        {
#if DEBUG
            Console.WriteLine("DEBUG: [Client] Registering Default Handlers");
#endif

            RegisterHandler<ConnectSuccessMessage>(OnConnectSuccessReceived, false);
            RegisterHandler<PingMessage>(OnPingReceived, false);
            RegisterHandler<DisconnectMessage>(OnDisconnectionReceived, false);
        }

        void OnConnectSuccessReceived(ConnectSuccessMessage message)
        {
            if (validConnection)
                return;

            validConnection = true;
        }

        void OnPingReceived(PingMessage message)
        {
            Send(new PongMessage());
        }

        void OnDisconnectionReceived(DisconnectMessage message)
        {
#if DEBUG
            Console.WriteLine("DEBUG: [Client] Server Disconnected the Client!");
#endif

            clientConnection.Disconnect();
            validConnection = false;
        }

        public void Send<T>(T message) where T : INetworkMessage
        {
            if(clientSocket.Connected && validConnection)
            {
                byte[] toSend = MessagePacker.Pack(message, clientConnection);

                NetworkDiagnostic.OnSend(message, toSend.Length);

                clientSocket.Send(toSend);
            }
            else
            {
                Console.WriteLine("[Client] Tried to send a message without being connected to a server! Check if the server is available if this happens");
            }
        }

        public void Disconnect()
        {
            if (!clientSocket.Connected)
                return;

#if DEBUG
            Console.WriteLine("DEBUG: [Client] Disconnecting Client");
#endif

            Send(new DisconnectMessage());

            clientConnection.Disconnect();
            validConnection = false;
        }

        ~NetworkClient()
        {
            Disconnect();
        }

        #region Register/Unregister Handlers [Getters]

        public void RegisterHandler(int messageType, NetworkMessageDelegate handler) => clientConnection.RegisterHandler(messageType, handler);
        public void RegisterHandler<T>(Action<T> handler, bool requiredAuthentication = true) where T : struct, INetworkMessage
        {
            Action<T, IPEndPoint> internalHandler = (message, endPoint) => handler(message);
            clientConnection.RegisterHandler<T>(internalHandler, requiredAuthentication);
        }

        public void UnregisterHandler(int messageType) => clientConnection.UnregisterHandler(messageType);
        public void UnregisterHandler<T>() where T : INetworkMessage => clientConnection.UnregisterHandler<T>();

        public void ClearHandlers() => clientConnection.ClearHandlers();

        #endregion
    }
}
