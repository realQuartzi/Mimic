using System;

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
}
