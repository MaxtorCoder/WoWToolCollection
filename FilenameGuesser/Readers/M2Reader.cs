using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FilenameGuesser.Util;

namespace FilenameGuesser.Readers
{
    public class M2Reader
    {
        private BinaryReader Reader;
        private M2 M2;
        
        public M2Reader(BinaryReader reader)
        {
            Reader = reader;
            Process();
        }
        
        private void Process()
        {
            M2 = new M2();
            M2.AnimFileDataIds = new List<uint>();
            M2.TextureFileDataIds = new List<uint>();
            M2.SkinFileDataIds = new List<uint>();
            
            while (Reader.BaseStream.Position < Reader.BaseStream.Length)
            {
                var chunk = (Chunk) Program.FlipUInt(Reader.ReadUInt32());
                var size = Reader.ReadUInt32();
                
                switch (chunk)
                {
                    case Chunk.MD21:
                        Reader.ReadBytes(8);

                        var m2Name = Reader.ReadM2Array();
                        Reader.BaseStream.Position = m2Name.Offset + 8;
                        M2.Name = Encoding.UTF8.GetString(Reader.ReadBytes((int) m2Name.Size)).Replace("\0", "");

                        Reader.BaseStream.Position = 8;
                        Skip(size);
                        break;
                    case Chunk.TXID:
                        var textureCount = size / 4;
                        for (var i = 0; i < textureCount; ++i)
                            M2.TextureFileDataIds.Add(Reader.ReadUInt32());
                        break;
                    case Chunk.SFID:
                        var skinCount = size / 4;
                        for (var i = 0; i < skinCount; ++i)
                            M2.SkinFileDataIds.Add(Reader.ReadUInt32());
                        break;
                }
            }
        }

        private void Skip(uint size) => Reader.BaseStream.Seek(size, SeekOrigin.Current);

        public string GetName() => M2.Name;
        public List<uint> GetTextures() => M2.TextureFileDataIds;
        public List<uint> GetSkins() => M2.SkinFileDataIds;
    }

    public struct M2
    {
        public string Name;
        public List<uint> SkinFileDataIds;
        public List<uint> TextureFileDataIds;
        public List<uint> AnimFileDataIds;
    }
}