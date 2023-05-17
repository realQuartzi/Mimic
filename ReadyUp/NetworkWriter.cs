using System;
using System.Text;

namespace ReadyUp
{
    public class NetworkWriter
    {
        public const ushort maxStringLength = ushort.MaxValue - 1;
        public const int defaultCapacity = 1024;

        internal byte[] buffer = new byte[defaultCapacity];

        public int Position;
        public int Capacity => buffer.Length;

        public void Reset() => Position = 0;

        void EnsureCapacity(int value)
        {
            if(buffer.Length < value)
            {
                int capacity = Math.Max(value, buffer.Length * 2);
                Array.Resize(ref buffer, capacity);
            }
        }

        public byte[] ToArray()
        {
            byte[] data = new byte[Position];
            Array.ConstrainedCopy(buffer, 0, data, 0, Position);
            return data;
        }

        public ArraySegment<byte> ToArraySegment() => new ArraySegment<byte>(buffer, 0, Position);
        public static implicit operator ArraySegment<byte>(NetworkWriter netWriter) => netWriter.ToArraySegment();

        internal unsafe void WriteBlittable<T>(T value) where T : unmanaged
        {
            int size = sizeof(T);

            EnsureCapacity(size);

            fixed (byte* ptr = &buffer[Position])
            {
                *(T*)ptr = value;
            }

            Position += size;
        }

        internal void WriteBlittableNullable<T>(T? value) where T : unmanaged
        {
            // Bool is not blittable. Write a Byte
            WriteByte((byte)(value.HasValue ? 0x01 : 0x00));

            // Only write value if exists.
            if(value.HasValue)
                WriteBlittable(value.Value);
        }

        public void WriteByte(byte value) => WriteBlittable(value);

        /*public void WriteByte(byte value)
        {
            Console.WriteLine("WriteByte: " + value);
            buffer[Position++] = value;
        }*/

        // For byte arrays with consistent size, where the reader knows how many to read.
        public void WriteBytes(byte[] array, int offset, int size)
        {
            EnsureCapacity(Position + size);
            Array.ConstrainedCopy(array, offset, this.buffer, Position, size);
            Position += size;
        }

        public void WriteUInt16(ushort value)
        {
            WriteByte((byte)(value & 0xFF));
            WriteByte((byte)(value >> 8));
        }
        public void WriteInt16(short value) => WriteUInt16((ushort)value);

        public void WriteUInt32(uint value)
        {
            WriteByte((byte)(value & 0xFF));
            WriteByte((byte)((value >> 8) & 0xFF));
            WriteByte((byte)((value >> 16) & 0xFF));
            WriteByte((byte)((value >> 24) & 0xFF));
        }
        public void WriteInt32(int value) => WriteUInt32((uint)value);

        public void WriteUInt64(ulong value)
        {
            WriteByte((byte)(value & 0xFF));
            WriteByte((byte)((value >> 8) & 0xFF));
            WriteByte((byte)((value >> 16) & 0xFF));
            WriteByte((byte)((value >> 24) & 0xFF));
            WriteByte((byte)((value >> 32) & 0xFF));
            WriteByte((byte)((value >> 40) & 0xFF));
            WriteByte((byte)((value >> 48) & 0xFF));
            WriteByte((byte)((value >> 56) & 0xFF));
        }
        public void WriteInt64(long value) => WriteUInt64((ulong)value);
    }

    public static class NetworkWriterExtensions
    {
        static readonly UTF8Encoding encoding = new UTF8Encoding(false, true);
        static readonly byte[] stringBuffer = new byte[NetworkWriter.maxStringLength];

        public static void WriteByte(this NetworkWriter writer, byte value) => writer.WriteBlittable(value);
        public static void WriteSByte(this NetworkWriter writer, sbyte value) => writer.WriteBlittable(value);

        public static void WriteChar(this NetworkWriter writer, char value) => writer.WriteBlittable((short)value);
        public static void WriteString(this NetworkWriter writer, string value)
        {
            if(value == null)
            {
                writer.WriteUInt16((ushort)0);
                return;
            }

            int size = encoding.GetBytes(value, 0, value.Length, stringBuffer, 0);

            if(size >= NetworkWriter.maxStringLength)
            {
                throw new IndexOutOfRangeException("NetworkWriter.Write(string) too long: " + size + ". Limit " + NetworkWriter.maxStringLength);
            }

            writer.WriteUInt16(checked((ushort)(size + 1)));
            writer.WriteBytes(stringBuffer, 0, size);
        }

        public static void WriteBool(this NetworkWriter writer, bool value) => writer.WriteBlittable((byte)(value ? 1 : 0));

        public static void WriteUShort(this NetworkWriter writer, ushort value) => writer.WriteBlittable(value);
        public static void WriteShort(this NetworkWriter writer, short value) => writer.WriteUInt16((ushort)value);

        public static void WriteUInt(this NetworkWriter writer, uint value) => writer.WriteBlittable(value);
        public static void WriteInt(this NetworkWriter writer, int value) => writer.WriteBlittable(value);

        public static void WriteULong(this NetworkWriter writer, ulong value) => writer.WriteBlittable(value);
        public static void WriteLong(this NetworkWriter writer, long value) => writer.WriteBlittable(value);

        public static void WriteFloat(this NetworkWriter writer, float value) => writer.WriteBlittable(value);
        public static void WriteDouble(this NetworkWriter writer, double value) => writer.WriteBlittable(value);
        public static void WriteDecimal(this NetworkWriter writer, decimal value) => writer.WriteBlittable(value);

        public static void WriteGUID(this NetworkWriter writer, Guid value)
        {
            byte[] data = value.ToByteArray();
            writer.WriteBytes(data, 0, data.Length);
        }
    }
}
