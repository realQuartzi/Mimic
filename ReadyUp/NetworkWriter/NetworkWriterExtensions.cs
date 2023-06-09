using System;
using System.Text;

namespace ReadyUp
{
    public static class NetworkWriterExtensions
    {
        static readonly UTF8Encoding encoding = new UTF8Encoding(false, true);
        static readonly byte[] stringBuffer = new byte[NetworkWriter.maxStringLength];

        public static void WriteByte(this NetworkWriter writer, byte value) => writer.WriteBlittable(value);
        public static void WriteSByte(this NetworkWriter writer, sbyte value) => writer.WriteBlittable(value);

        public static void WriteChar(this NetworkWriter writer, char value) => writer.WriteBlittable((short)value);
        public static void WriteString(this NetworkWriter writer, string value)
        {
            if (value == null)
            {
                writer.WriteUInt16((ushort)0);
                return;
            }

            int size = encoding.GetBytes(value, 0, value.Length, stringBuffer, 0);

            if (size >= NetworkWriter.maxStringLength)
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
