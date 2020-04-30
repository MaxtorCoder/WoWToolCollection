using System;
using System.IO;

namespace DB2Dumper
{
    public static class DB2Reader
    {
        private const uint WDC3fmt = 860046423;

        public static (uint TableHash, uint LayoutHash) ReadDB2(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var reader = new BinaryReader(stream))
            {
                uint Magic = reader.ReadUInt32();

                if (Magic != WDC3fmt)
                {
                    Console.WriteLine($"File is corrupt! {Magic}");
                    return (0, 0);
                }

                reader.ReadBytes(16);
                uint TableHash = reader.ReadUInt32();
                uint LayoutHash = reader.ReadUInt32();

                // Return LayoutHash
                return (TableHash, LayoutHash);
            }
        }
    }
}
