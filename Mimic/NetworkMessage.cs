namespace Mimic
{
    public interface INetworkMessage
    {

        void Serialize(NetworkWriter writer);
        void Deserialize(NetworkReader reader);
    }

    public abstract class NetworkMessage : INetworkMessage
    {
        public virtual void Deserialize(NetworkReader reader) { }

        public virtual void Serialize(NetworkWriter writer) { }
    }
}