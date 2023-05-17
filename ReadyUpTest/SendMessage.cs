using ReadyUp;

namespace ReadyUpTest
{
    public struct SendMessage : INetworkMessage
    {
        public string message;

        public SendMessage(string message)
        {
            this.message = message;
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.WriteString(message);
        }

        public void Deserialize(NetworkReader reader)
        {
            message = reader.ReadString();
        }
    }
}
