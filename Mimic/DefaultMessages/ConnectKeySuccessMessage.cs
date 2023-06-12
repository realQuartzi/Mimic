
namespace Mimic
{
    public struct ConnectKeySuccessMessage : INetworkMessage
    {
        public int length;
        public byte[] key;

        public void Deserialize(NetworkReader reader) 
        {
            length = reader.ReadInt();
            key = reader.ReadBytes(length);
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.WriteInt(length);
            writer.WriteBytes(key, 0, key.Length);
        }
    }
}
