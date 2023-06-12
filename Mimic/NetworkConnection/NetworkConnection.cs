using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Mimic
{
    public partial class NetworkConnection
    {
        // Useful network Data
        public Socket socket;
        public bool authorized;
        public long lastMessageTime;

        public IPEndPoint ipEndPoint;
        public IPAddress address => ipEndPoint.Address;
        public int port => ipEndPoint.Port;

        Dictionary<int, NetworkMessageDelegate> messageHandlers = new Dictionary<int, NetworkMessageDelegate>();

        public bool isServer = false;

        /// <summary>
        /// Used to record NetworkConnections to return messages to.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="port"></param>
        public NetworkConnection(Socket socket, string networkAddress, int port)
        {
            this.socket = socket;
            this.authorized = false;

            byte[] address = new byte[4] {127,0,0,1};
            if(networkAddress != null && networkAddress.Contains('.'))
            {
                IPAddress tempAddress = IPAddress.Parse(networkAddress);
                address = tempAddress.GetAddressBytes();
            }

            this.ipEndPoint = new IPEndPoint(new IPAddress(address), port);

            this.lastMessageTime = DateTime.UtcNow.Ticks;
        }

        public NetworkConnection(Socket socket, byte[] networkAddress, int port)
        {
            this.socket = socket;
            this.authorized = false;

            this.ipEndPoint = new IPEndPoint(new IPAddress(networkAddress), port);

            this.lastMessageTime = DateTime.UtcNow.Ticks;
        }

        public NetworkConnection(Socket socket, IPEndPoint ipEndPoint)
        {
            this.socket = socket;
            this.authorized = false;

            this.ipEndPoint = ipEndPoint;

            this.lastMessageTime = DateTime.UtcNow.Ticks;
        }

        public void AuthorizeConnection() => authorized = true;

        #region Register/Unregister Handlers

        public void RegisterHandler(int messageType, NetworkMessageDelegate handler)
        {
            if (messageHandlers.ContainsKey(messageType))
            {
                Console.WriteLine("NetworkConnection.RegisterHandler replacing " + messageType);
            }
            messageHandlers[messageType] = handler;
        }
        public void RegisterHandler<T>(Action<T, IPEndPoint> handler, bool requiredAuthentication = true) where T : struct, INetworkMessage
        {
            int messageType = MessagePacker.GetID<T>();
            if (messageHandlers.ContainsKey(messageType))
            {
                Console.WriteLine("NetworkConnection.RegisterHandler replacing " + messageType);
            }
            messageHandlers[messageType] = MessagePacker.MessageHandler<T>(handler, requiredAuthentication);
        }

        /// <summary>
        /// Unregister the specified Handler
        /// </summary>
        /// <param name="messageType"></param>
        public void UnregisterHandler(int messageType)
        {
            messageHandlers.Remove(messageType);
        }
        /// <summary>
        /// Unregister the specified Handler
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void UnregisterHandler<T>() where T : INetworkMessage
        {
            int messageType = MessagePacker.GetID<T>();
            messageHandlers.Remove(messageType);
        }

        /// <summary>
        /// Unregister all Handlers
        /// </summary>
        public void ClearHandlers() => messageHandlers.Clear();

        #endregion

        #region Invoke Handler

        internal bool InvokeHandler(int messageType, IPEndPoint senderIdentifier, NetworkReader reader)
        {
            if (messageHandlers.TryGetValue(messageType, out NetworkMessageDelegate messageDelegate))
            {
                Message message = new Message
                {
                    messageType = messageType,
                    senderIdentifier = senderIdentifier,
                    reader = reader
                };

                messageDelegate(message);
                return true;
            }

            // Unkown Message ID
            return false;
        }
        public bool InvokeHandler<T>(T message, IPEndPoint senderIdentifier) where T : INetworkMessage
        {
            int messageType = MessagePacker.GetID(message.GetType());
            byte[] data = MessagePacker.Pack(message);
            return InvokeHandler(messageType, senderIdentifier, NetworkReaderPool.GetReader(data));
        }

        #endregion

        /// <summary>
        /// Read and Unpack the received data.
        /// </summary>
        /// <param name="buffer">Received byte[] data</param>
        /// <param name="sendIdentifier">IPEndPoint of the sender of the data</param>
        public void OnReceivedData(byte[] buffer, IPEndPoint sendIdentifier = null)
        {
            NetworkReader reader = NetworkReaderPool.GetReader(buffer);

            if (MessagePacker.UnpackMessage(reader, out int messageType))
            {
                if (InvokeHandler(messageType, sendIdentifier, reader))
                {
                    if (isServer)
                    {
                        if (BaseServer.clientConnections.ContainsKey(sendIdentifier))
                        {
                            NetworkConnectionToClient conn = null;
                            if (BaseServer.clientConnections.TryGetValue(sendIdentifier, out conn))
                            {
                                conn.lastMessageTime = DateTime.UtcNow.Ticks;
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Invalid Message Received!");
                // Invalid message header.
            }

            NetworkReaderPool.Recycle(reader);
        }

        public void Disconnect()
        {
            socket.Close();

#if DEBUG
            Console.WriteLine("DEBUG: Socket was Disconnected");
#endif
        }
    }
}
