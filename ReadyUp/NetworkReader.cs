using System;
using System.IO;
using System.Text;

namespace ReadyUp
{
    public class NetworkReader
    {
        ArraySegment<byte> buffer;

        public int Position;
        public int Length => buffer.Count;
        public int Remaining => buffer.Count - Position;

        public NetworkReader(byte[] bytes)
        {
            buffer = new ArraySegment<byte>(bytes);
        }

        public NetworkReader(ArraySegment<byte> segment)
        {
            buffer = segment;
        }

        internal unsafe T ReadBlittable<T>() where T : unmanaged
        {
            int size = sizeof(T);

            if(Remaining < size)
            {
                throw new EndOfStreamException($"ReadBlittable<{typeof(T)}> not enough data in buffer to read {size} bytes: {ToString()}");
            }

            T value;
            fixed(byte* ptr = &buffer.Array[buffer.Offset + Position])
            {
                value = *(T*)ptr;
            }

            Position += size;

            return value;
        }

        internal T? ReadBlittableNullable<T>() where T : unmanaged
        {
            return ReadByte() != 0 ? ReadBlittable<T>() : default(T?);
        }

        public byte ReadByte() => ReadBlittable<byte>();

        /*public byte ReadByte()
        {
            if(Position + 1 > buffer.Count)
            {
                throw new EndOfStreamException("ReadByte out of range: " + ToString());
            }
            Console.WriteLine("ReadByte: " + buffer[buffer.Offset + Position]);

            return buffer.Array[buffer.Offset + Position++];
        }*/

        public byte[] ReadBytes(byte[] bytes, int count)
        {
            if(count > bytes.Length)
            {
                throw new EndOfStreamException("ReadBytes can't read " + count + " bytes because the passed byte[] only has a length of: " + bytes.Length);
            }

            ArraySegment<byte> data = ReadBytesSegment(count);
            Array.Copy(data.Array, data.Offset, bytes, 0, count);
            return bytes;
        }

        public byte[] ReadBytes(int count)
        {
            byte[] bytes = new byte[count];
            ReadBytes(bytes, count);
            return bytes;
        }

        public ArraySegment<byte> ReadBytesSegment(int count)
        {
            if(Position + count > buffer.Count)
            {
                throw new EndOfStreamException("ReadBytesSegment can't read " + count + " bytes beacuse it would read past the end of the stream. " + ToString());
            }

            ArraySegment<byte> result = new ArraySegment<byte>(buffer.Array, buffer.Offset + Position, count);
            Position += count;
            return result;
        }

        public short ReadInt16() => (short)ReadUInt16();
        public ushort ReadUInt16()
        {
            ushort value = 0;

            value |= ReadByte();
            value |= (ushort)(ReadByte() << 8);

            return value;
        }

        public int ReadInt32() => (int)ReadUInt32();
        public uint ReadUInt32()
        {
            uint value = 0;

            value |= ReadByte();
            value |= (uint)(ReadByte() << 8);
            value |= (uint)(ReadByte() << 16);
            value |= (uint)(ReadByte() << 24);

            return value;
        }

        public long ReadInt64() => (int)ReadUInt64();
        public ulong ReadUInt64()
        {
            ulong value = 0;

            value |= ReadByte();
            value |= ((ulong)(ReadByte()) << 8);
            value |= ((ulong)(ReadByte()) << 16);
            value |= ((ulong)(ReadByte()) << 24);
            value |= ((ulong)(ReadByte()) << 32);
            value |= ((ulong)(ReadByte()) << 40);
            value |= ((ulong)(ReadByte()) << 48);
            value |= ((ulong)(ReadByte()) << 56);

            return value;
        }

        public override string ToString()
        {
            return "NetworkReader Position=" + Position + " Length=" + Length + " Buffer=" + BitConverter.ToString(buffer.Array, buffer.Offset, buffer.Count);
        }
    }

    public static class NetworkReaderExtensions
    {
        static readonly UTF8Encoding encoding = new UTF8Encoding(false, true);

        public static byte ReadByte(this NetworkReader reader) => reader.ReadBlittable<byte>();
        public static sbyte ReadSByte(this NetworkReader reader) => (sbyte)reader.ReadBlittable<sbyte>();

        public static char ReadChar(this NetworkReader reader) => (char)reader.ReadBlittable<ushort>();
        public static string ReadString(this NetworkReader reader)
        {
            ushort size = reader.ReadUShort();

            if (size == 0)
                return null;

            int realSize = (ushort)(size - 1);

            if(realSize >= NetworkWriter.maxStringLength)
            {
                throw new EndOfStreamException("ReadString too long: " + realSize + ". Limit is: " + NetworkWriter.maxStringLength);
            }

            ArraySegment<byte> data = reader.ReadBytesSegment(realSize);

            return encoding.GetString(data.Array, data.Offset, data.Count);
        }

        public static bool ReadBool(this NetworkReader reader) => reader.ReadByte() != 0;

        public static ushort ReadUShort(this NetworkReader reader) => reader.ReadBlittable<ushort>();
        public static short ReadShort(this NetworkReader reader) => (short)reader.ReadShort();

        public static uint ReadUInt(this NetworkReader reader) => reader.ReadBlittable<uint>();
        public static int ReadInt(this NetworkReader reader) => (int)reader.ReadBlittable<int>();

        public static ulong ReadULong(this NetworkReader reader) => reader.ReadBlittable<ulong>();
        public static long ReadLong(this NetworkReader reader) => (long)reader.ReadBlittable<long>();

        public static float ReadFloat(this NetworkReader reader) => reader.ReadBlittable<float>();
        public static double ReadDouble(this NetworkReader reader) => reader.ReadBlittable<double>();
        public static decimal ReadDecimal(this NetworkReader reader) => reader.ReadBlittable<decimal>();

        public static Guid ReadGUID(this NetworkReader reader) => new Guid(reader.ReadBytes(16));
    }
}
