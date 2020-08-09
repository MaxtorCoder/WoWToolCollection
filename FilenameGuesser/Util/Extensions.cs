using System.IO;
using System.Text;

namespace FilenameGuesser.Util
{
    public static class Extensions
    {
        public static M2Array ReadM2Array(this BinaryReader reader)
        {
            return new M2Array
            {
                Size = reader.ReadUInt32(),
                Offset = reader.ReadUInt32()
            };
        }

        public static uint FlipUInt(this uint n)
        {
            return (n << 24) | (((n >> 16) << 24) >> 16) | (((n << 16) >> 24) << 16) | (n >> 24);
        }

        public static bool IsModel(this Stream stream)
        {
            if (stream.Length > 8)
            {
                var data = new byte[4];
                stream.Read(data, 0, 4);
                stream.Position -= 4;

                if (Encoding.UTF8.GetString(data) == "MD21")
                    return true;
            }

            return false;
        }

        public static void Skip(this BinaryReader reader, uint size) => reader.BaseStream.Position += size;
    }
}