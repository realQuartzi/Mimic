
namespace ReadyUp
{
    public struct Message
    {
        public int messageType;
        public ushort senderIdentifier;
        public NetworkReader reader;

        public T ReadMessage<T>() where T : INetworkMessage, new()
        {
            T message = typeof(T).IsValueType ? default(T) : new T();

            message.Deserialize(reader);

            return message;
        }

        public void ReadMeesage<T>(T message) where T : INetworkMessage
        {
            message.Deserialize(reader);
        }
    }
}
