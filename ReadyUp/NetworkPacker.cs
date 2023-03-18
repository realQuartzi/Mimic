namespace ReadyUp
{
    public static class MessagePacker
    {
        public static string GetName<T>() where T : INetworkMessage
        {
            return typeof(T).FullName;
        }

        public static int GetID<T>() where T : INetworkMessage
        {
            return typeof(T).FullName.GetStableHashCode() & 0xFFFF;
        }

        public static int GetID(Type type)
        {
            return type.FullName.GetStableHashCode() & 0xFFFF;
        }

        public static byte[] PackMessage(int messageType, NetworkMessage message)
        {
            NetworkWriter writer = NetworkWriterPool.GetWriter();
            try
            {
                writer.WriteInt16((short)messageType);
                writer.WriteUInt16(0);

                message.Serialize(writer);

                return writer.ToArray();
            }
            finally
            {
                NetworkWriterPool.Recycle(writer);
            }
        }
        public static byte[] PackMessage(int messageType, NetworkMessage message, ushort senderID)
        {
            NetworkWriter writer = NetworkWriterPool.GetWriter();
            try
            {
                writer.WriteInt16((short)messageType);
                writer.WriteUInt16(senderID);

                message.Serialize(writer);

                return writer.ToArray();
            }
            finally
            {
                NetworkWriterPool.Recycle(writer);
            }
        }

        public static void Pack<T>(T message, NetworkWriter writer) where T : INetworkMessage
        {
            int messageType = GetID(typeof(T).IsValueType ? typeof(T) : message.GetType());
            writer.WriteUInt16((ushort)messageType);
            writer.WriteUInt16(0);

            message.Serialize(writer);
        }

        public static byte[] Pack<T>(T message) where T : INetworkMessage
        {
            NetworkWriter writer = NetworkWriterPool.GetWriter();

            Pack(message, writer);
            byte[] data = writer.ToArray();

            NetworkWriterPool.Recycle(writer);

            return data;
        }

        public static void Pack<T>(T message, NetworkConnection sender, NetworkWriter writer) where T : INetworkMessage
        {
            int messageType = GetID(typeof(T).IsValueType ? typeof(T) : message.GetType());
            writer.WriteUInt16((ushort)messageType);
            writer.WriteUInt16(sender.identifier);

            message.Serialize(writer);
        }

        public static byte[] Pack<T>(T message, NetworkConnection sender) where T : INetworkMessage
        {
            NetworkWriter writer = NetworkWriterPool.GetWriter();

            Pack(message, sender, writer);
            byte[] data = writer.ToArray();

            NetworkWriterPool.Recycle(writer);

            return data;
        }

        public static T Unpack<T>(byte[] data) where T : INetworkMessage, new()
        {
            NetworkReader reader = new NetworkReader(data);

            int messageType = GetID<T>();

            int id = reader.ReadUInt16();
            if(id != messageType)
            {
                throw new FormatException("Invalid Message, could not unpack " + typeof(T).FullName);
            }

            ushort sendIdentifier = reader.ReadUInt16();

            T message = new T();
            message.Deserialize(reader);
            return message;
        }

        public static bool UnpackMessage(NetworkReader reader, out int messageType, out ushort sendIdentifier)
        {
            try
            {
                messageType = reader.ReadUInt16();
                sendIdentifier = reader.ReadUInt16();
                return true;
            }
            catch(System.IO.EndOfStreamException)
            {
                messageType = 0;
                sendIdentifier = 0;
                return false;
            }
        }

        public static NetworkMessageDelegate MessageHandler<T>(Action<T, ushort> handler, bool requireAuthentication) where T : INetworkMessage, new() => networkMessage =>
        {
            T message = default;
            try
            {
                message = networkMessage.ReadMessage<T>();
            }
            catch (Exception e)
            {
                Console.WriteLine("Invalid Data Received: " + e.Message);
                return;
            }
            finally
            {
                NetworkDiagnostic.OnReceive(message, networkMessage.reader.Length);
            }

            try
            {
                // User Implemented Handler
                handler(message, networkMessage.senderIdentifier);
            }
            catch(Exception e)
            {
                Console.WriteLine("Message Handler was not setup properly: " + e.GetType().Name + " | " + e.Message);
            }


        };
    }

    public delegate void NetworkMessageDelegate(Message message);
}
