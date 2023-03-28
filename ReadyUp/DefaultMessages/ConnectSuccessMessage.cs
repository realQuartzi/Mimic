
namespace ReadyUp
{
    public struct ConnectSuccessMessage : INetworkMessage
    {
        public Guid identity;

        public void Serialize(NetworkWriter writer)
        {
            writer.WriteGUID(identity);
        }

        public void Deserialize(NetworkReader reader)
        {
            identity = reader.ReadGUID();
        }
    }
}
