
namespace ReadyUp
{
    public static class NetworkDiagnostic
    {
        public readonly struct MessageInfo
        {
            public readonly INetworkMessage message;
            public readonly int bytes;

            internal MessageInfo(INetworkMessage message, int bytes)
            {
                this.message = message;
                this.bytes = bytes;
            }
        }


        public static event Action<MessageInfo> OutMessageEvent;
        internal static void OnSend<T>(T message, int bytes) where T : INetworkMessage
        {
            if(OutMessageEvent != null)
            {
                MessageInfo outMessage = new MessageInfo(message, bytes);
                OutMessageEvent?.Invoke(outMessage);
            }
        }

        public static event Action<MessageInfo> InMessageEvent;
        internal static void OnReceive<T>(T message, int bytes) where T : INetworkMessage
        {
            if(InMessageEvent != null)
            {
                MessageInfo inMessage = new MessageInfo(message, bytes);
                InMessageEvent?.Invoke(inMessage);
            }
        }
    }
}
