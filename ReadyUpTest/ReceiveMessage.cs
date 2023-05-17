using ReadyUp;

namespace ReadyUpTest
{
    public struct ReceiveMessage : INetworkMessage
    {
        public string message;

        public ReceiveMessage(string message)
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
