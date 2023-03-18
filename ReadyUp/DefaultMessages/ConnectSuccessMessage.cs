
namespace ReadyUp
{
    public struct ConnectSuccessMessage : INetworkMessage
    {
        public ushort identity;

        public void Serialize(NetworkWriter writer)
        {
            writer.WriteUShort(identity);
        }

        public void Deserialize(NetworkReader reader)
        {
            identity = reader.ReadUShort();
        }
    }
}
