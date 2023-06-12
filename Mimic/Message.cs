using System.Net;

namespace Mimic
{
    public struct Message
    {
        public int messageType;
        public IPEndPoint senderIdentifier;
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
