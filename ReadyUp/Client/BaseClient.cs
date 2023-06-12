using System.Net.Sockets;
using System.Net;
using System;
using System.Threading.Tasks;

namespace Mimic
{
    public abstract partial class BaseClient
    {
        protected static byte[] globalBuffer = new byte[1024];

        public NetworkConnectionToServer clientConnection;

        /// <summary>
        /// True if the server has accepted the connection.
        /// </summary>
        public bool validConnection;

        public IPEndPoint serverEndPoint => clientConnection.ipEndPoint;
        protected Socket clientSocket => clientConnection.socket;

        public bool isConnected => clientSocket.Connected;
        //public bool isConnected => !((clientSocket.Poll(1000, SelectMode.SelectRead) && (clientSocket.Available == 0)) || !clientSocket.Connected);

        ~BaseClient()
        {
            clientSocket.Close();
        }

        /// <summary>
        /// Attempt to connect to the server
        /// </summary>
        /// <param name="endPoint">Server EndPoint</param>
        /// <param name="maxAttempts">Total attempts</param>
        /// <param name="retryDelay">Delay between attempts [milliseconds]</param>
        public virtual async void Connect(EndPoint endPoint, int maxAttempts = 10, int retryDelay = 1000)
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

            if (clientSocket.Connected)
            {
                Console.WriteLine("[Client] NetworkClient connected to Server!");
                Console.WriteLine("[Client] NetworkClient listening to: " + clientConnection.address);

                clientSocket.BeginReceive(globalBuffer, 0, globalBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), clientSocket);
            }
            else
            {
                Console.WriteLine("[Client] Failed to connect to the server.");
            }
        }

        /// <summary>
        /// Receive and handle the incomming message / data.
        /// </summary>
        /// <param name="result"></param>
        protected abstract void ReceiveCallback(IAsyncResult result);

        /// <summary>
        /// Send the message type T to the server.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        public abstract void Send<T>(T message) where T : INetworkMessage;

        /// <summary>
        /// Disconnect from the server.
        /// </summary>
        public virtual void Disconnect()
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

        #region Register/Unregister Handlers [Getters]

        /// <summary>
        /// Register a handler for message type using its stable hash.
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="handler"></param>
        public void RegisterHandler(int messageType, NetworkMessageDelegate handler) => clientConnection.RegisterHandler(messageType, handler);

        /// <summary>
        /// Register a handler for a message type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        /// <param name="requiredAuthentication"></param>
        public void RegisterHandler<T>(Action<T> handler, bool requiredAuthentication = true) where T : struct, INetworkMessage
        {
            Action<T, IPEndPoint> internalHandler = (message, endPoint) => handler(message);
            clientConnection.RegisterHandler<T>(internalHandler, requiredAuthentication);
        }

        /// <summary>
        /// Unregister a handler for a message type using its stable hash ID.
        /// </summary>
        /// <param name="messageType"></param>
        public void UnregisterHandler(int messageType) => clientConnection.UnregisterHandler(messageType);

        /// <summary>
        /// Unregister a handler for a message type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void UnregisterHandler<T>() where T : INetworkMessage => clientConnection.UnregisterHandler<T>();

        /// <summary>
        /// Clears all registered message handlers.
        /// </summary>
        public void ClearHandlers() => clientConnection.ClearHandlers();

        #endregion
    }
}
