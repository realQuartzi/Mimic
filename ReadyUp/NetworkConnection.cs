using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace ReadyUp
{
    public class NetworkConnection
    {
        // Useful network Data
        public Socket socket;
        public Guid identifier;
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
        /// <param name="identifier"></param>
        public NetworkConnection(Socket socket, string networkAddress, int port, Guid identifier)
        {
            this.socket = socket;
            this.identifier = identifier;
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

        public NetworkConnection(Socket socket, byte[] networkAddress, int port, Guid identifier)
        {
            this.socket = socket;
            this.identifier = identifier;
            this.authorized = false;

            this.ipEndPoint = new IPEndPoint(new IPAddress(networkAddress), port);

            this.lastMessageTime = DateTime.UtcNow.Ticks;
        }

        public NetworkConnection(Socket socket, IPEndPoint ipEndPoint, Guid identifier)
        {
            this.socket = socket;
            this.identifier = identifier;
            this.authorized = false;

            this.ipEndPoint = ipEndPoint;

            this.lastMessageTime = DateTime.UtcNow.Ticks;
        }

        public void AuthorizeConnection(Guid identifier)
        {
            this.identifier = identifier;
            authorized = true;
        }

        #region Register/Unregister Handlers

        public void RegisterHandler(int messageType, NetworkMessageDelegate handler)
        {
            if (messageHandlers.ContainsKey(messageType))
            {
                Console.WriteLine("NetworkConnection.RegisterHandler replacing " + messageType);
            }
            messageHandlers[messageType] = handler;
        }
        public void RegisterHandler<T>(Action<T, Guid> handler, bool requiredAuthentication = true) where T : struct, INetworkMessage
        {
            int messageType = MessagePacker.GetID<T>();
            if (messageHandlers.ContainsKey(messageType))
            {
                Console.WriteLine("NetworkConnection.RegisterHandler replacing " + messageType);
            }
            messageHandlers[messageType] = MessagePacker.MessageHandler<T>(handler, requiredAuthentication);
        }
        public void UnregisterHandler(int messageType)
        {
            messageHandlers.Remove(messageType);
        }
        public void UnregisterHandler<T>() where T : INetworkMessage
        {
            int messageType = MessagePacker.GetID<T>();
            messageHandlers.Remove(messageType);
        }

        public void ClearHandlers() => messageHandlers.Clear();

        #endregion

        internal bool InvokeHandler(int messageType, Guid senderIdentifier, NetworkReader reader)
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
        public bool InvokeHandler<T>(T message, Guid senderIdentifier) where T : INetworkMessage
        {
            int messageType = MessagePacker.GetID(message.GetType());
            byte[] data = MessagePacker.Pack(message);
            return InvokeHandler(messageType, senderIdentifier, new NetworkReader(data));
        }

        public void OnReceivedData(byte[] buffer)
        {
            NetworkReader reader = new NetworkReader(buffer);

            if (MessagePacker.UnpackMessage(reader, out int messageType, out Guid sendIdentifier))
            {
                if (InvokeHandler(messageType, sendIdentifier, reader))
                {
                    if (isServer)
                    {
                        if (BaseServer.clientConnections.ContainsKey(sendIdentifier))
                        {
                            NetworkConnection conn = null;
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
                Console.WriteLine("[" + identifier.ToString() + "]" + "Invalid Message Received!");
                // Invalid message header.
            }
        }
    }
}
