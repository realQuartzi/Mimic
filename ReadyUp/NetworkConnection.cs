using System.Net.Sockets;

namespace ReadyUp
{
    public class NetworkConnection
    {
        // Useful network Data
        public Socket socket;
        public ushort identifier;
        public bool authorized;

        public string address;
        public int port;

        Dictionary<int, NetworkMessageDelegate> messageHandlers = new Dictionary<int, NetworkMessageDelegate>();
        public long lastMessageTime;


        /// <summary>
        /// Used to record NetworkConnections to return messages to.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="port"></param>
        /// <param name="identifier"></param>
        public NetworkConnection(Socket socket, string networkAddress, int port, ushort identifier = 0)
        {
            this.socket = socket;
            this.identifier = identifier;
            this.authorized = false;

            this.address = networkAddress;
            this.port = port;
        }

        public void AuthorizeConnection(ushort identifier)
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
        public void RegisterHandler<T>(Action<T, ushort> handler, bool requiredAuthentication = true) where T : struct, INetworkMessage
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

        internal bool InvokeHandler(int messageType, ushort senderIdentifier, NetworkReader reader)
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
        public bool InvokeHandler<T>(T message, ushort senderIdentifier) where T : INetworkMessage
        {
            int messageType = MessagePacker.GetID(message.GetType());
            byte[] data = MessagePacker.Pack(message);
            return InvokeHandler(messageType, senderIdentifier, new NetworkReader(data));
        }

        public void OnReceivedData(byte[] buffer)
        {
            NetworkReader reader = new NetworkReader(buffer);

            if (MessagePacker.UnpackMessage(reader, out int messageType, out ushort sendIdentifier))
            {
                if (InvokeHandler(messageType, sendIdentifier, reader))
                {
                    lastMessageTime = DateTime.UtcNow.Ticks;
                }
            }
            else
            {
                Console.WriteLine("Invalid Message Received!");
                // Invalid message header.
            }
        }
    }
}
