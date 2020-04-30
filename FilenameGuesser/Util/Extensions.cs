using System.IO;

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
    }
}