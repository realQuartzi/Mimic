using System;
using System.Collections.Generic;

namespace Mimic
{
    public static class NetworkReaderPool
    {
        static readonly Stack<NetworkReader> pool = new Stack<NetworkReader>();

        public static NetworkReader GetReader(byte[] data)
        {
            if (pool.Count != 0)
            {
                NetworkReader reader = pool.Pop();

                reader.SetBuffer(data);
                return reader;
            }

            return new NetworkReader(data);
        }

        public static NetworkReader GetReader(ArraySegment<byte> segment)
        {
            if(pool.Count != 0)
            {
                NetworkReader reader = pool.Pop();

                reader.SetBuffer(segment);
                return reader;
            }

            return new NetworkReader(segment);
        }

        public static void Recycle(NetworkReader reader)
        {
            pool.Push(reader);
        }
    }
}
