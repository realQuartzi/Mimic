using System.Collections.Generic;

namespace ReadyUp
{
    public static class NetworkWriterPool
    {
        static readonly Stack<NetworkWriter> pool = new Stack<NetworkWriter>();

        public static NetworkWriter GetWriter()
        {
            if(pool.Count != 0)
            {
                NetworkWriter writer = pool.Pop();

                writer.Position = 0;
                return writer;
            }

            return new NetworkWriter();
        }

        public static void Recycle(NetworkWriter writer)
        {
            pool.Push(writer);
        }
    }
}
