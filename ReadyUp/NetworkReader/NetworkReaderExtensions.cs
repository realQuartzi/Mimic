using System;
using System.IO;
using System.Text;

namespace Mimic
{
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

            if (realSize >= NetworkWriter.maxStringLength)
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
