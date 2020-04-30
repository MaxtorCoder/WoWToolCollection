using System;
using System.IO;

namespace BuildMonitor.Util
{
    public static class BinaryReaderExtensions
    {
        public static double ReadDouble(this BinaryReader reader, bool invertEndian = false)
        {
            if (invertEndian)
                return BitConverter.ToDouble(reader.ReadInvertedBytes(8), 0);

            return reader.ReadDouble();
        }

        public static short ReadInt16(this BinaryReader reader, bool invertEndian = false)
        {
            if (invertEndian)
                return BitConverter.ToInt16(reader.ReadInvertedBytes(2), 0);

            return reader.ReadInt16();
        }

        public static int ReadInt32(this BinaryReader reader, bool invertEndian = false)
        {
            if (invertEndian)
                return BitConverter.ToInt32(reader.ReadInvertedBytes(4), 0);

            return reader.ReadInt32();
        }

        public static long ReadInt64(this BinaryReader reader, bool invertEndian = false)
        {
            if (invertEndian)
                return BitConverter.ToInt64(reader.ReadInvertedBytes(8), 0);

            return reader.ReadInt64();
        }

        public static float ReadSingle(this BinaryReader reader, bool invertEndian = false)
        {
            if (invertEndian)
                return BitConverter.ToSingle(reader.ReadInvertedBytes(4), 0);

            return reader.ReadSingle();
        }

        public static ushort ReadUInt16(this BinaryReader reader, bool invertEndian = false)
        {
            if (invertEndian)
                return BitConverter.ToUInt16(reader.ReadInvertedBytes(2), 0);

            return reader.ReadUInt16();
        }

        public static uint ReadUInt32(this BinaryReader reader, bool invertEndian = false)
        {
            if (invertEndian)
                return BitConverter.ToUInt32(reader.ReadInvertedBytes(4), 0);

            return reader.ReadUInt32();
        }

        public static ulong ReadUInt64(this BinaryReader reader, bool invertEndian = false)
        {
            if (invertEndian)
                return BitConverter.ToUInt64(reader.ReadInvertedBytes(8), 0);

            return reader.ReadUInt64();
        }

        public static ulong ReadUInt40(this BinaryReader reader, bool invertEndian = false)
        {
            ulong b1 = reader.ReadByte();
            ulong b2 = reader.ReadByte();
            ulong b3 = reader.ReadByte();
            ulong b4 = reader.ReadByte();
            ulong b5 = reader.ReadByte();

            if (invertEndian)
                return b1 << 32 | b2 << 24 | b3 << 16 | b4 << 8 | b5;
            else
                return b5 << 32 | b4 << 24 | b3 << 16 | b2 << 8 | b1;
        }

        private static byte[] ReadInvertedBytes(this BinaryReader reader, int byteCount)
        {
            byte[] byteArray = reader.ReadBytes(byteCount);
            Array.Reverse(byteArray);

            return byteArray;
        }
    }
}
