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
            M2.AnimationFileDataIds = new List<AFID>();
            M2.TextureFileDataIds = new List<uint>();
            M2.SkinFileDataIds = new List<uint>();
            M2.LodSkinFileDataIds = new List<uint>();
            
            while (Reader.BaseStream.Position < Reader.BaseStream.Length)
            {
                var chunk = (Chunk) Program.FlipUInt(Reader.ReadUInt32());
                var size = Reader.ReadUInt32();

                var lodCount = 0u;
                var skinCount = 0u;
                switch (chunk)
                {
                    case Chunk.MD21:
                        Reader.ReadBytes(8);

                        var m2Name = Reader.ReadM2Array();
                        var currPos = Reader.BaseStream.Position;
                        Reader.BaseStream.Position = m2Name.Offset + 8;
                        M2.Name = Encoding.UTF8.GetString(Reader.ReadBytes((int) m2Name.Size)).Replace("\0", "");

                        Reader.BaseStream.Position = currPos;

                        Skip(4 + 8 + 8 + 8 + 8 + 8 + 8);
                        skinCount = Reader.ReadUInt32();

                        Reader.BaseStream.Position = 8;
                        Skip(size);
                        break;
                    case Chunk.LDV1:
                        Reader.BaseStream.Position += 4;
                        lodCount = Reader.ReadUInt16() - 1u;
                        Reader.BaseStream.Position -= 4;
                        break;
                    case Chunk.TXID:
                        var textureCount = size / 4;
                        for (var i = 0; i < textureCount; ++i)
                            M2.TextureFileDataIds.Add(Reader.ReadUInt32());
                        break;
                    case Chunk.SFID:
                        for (var i = 0; i < skinCount; ++i)
                            M2.SkinFileDataIds.Add(Reader.ReadUInt32());

                        if (lodCount > 0)
                            for (var i = 0; i < lodCount; ++i)
                                M2.LodSkinFileDataIds.Add(Reader.ReadUInt32());

                        break;
                    case Chunk.AFID:
                        var animCount = size / 8;
                        for (var i = 0; i < animCount; ++i)
                        {
                            var afid = new AFID
                            {
                                AnimId = Reader.ReadUInt16(),
                                SubAnimId = Reader.ReadUInt16(),
                                AnimFileId = Reader.ReadUInt32()
                            };
                            M2.AnimationFileDataIds.Add(afid);
                        }
                        break;
                }
            }
        }

        private void Skip(uint size) => Reader.BaseStream.Seek(size, SeekOrigin.Current);

        public string GetName() => M2.Name;
        public List<uint> GetTextures() => M2.TextureFileDataIds;
        public List<uint> GetSkins() => M2.SkinFileDataIds;
        public List<uint> GetLodSkins() => M2.LodSkinFileDataIds;
        public List<AFID> GetAnims() => M2.AnimationFileDataIds;
    }

    public struct M2
    {
        public string Name;
        public List<uint> SkinFileDataIds;
        public List<uint> LodSkinFileDataIds;
        public List<uint> TextureFileDataIds;
        public List<AFID> AnimationFileDataIds;
    }

    public struct AFID
    {
        public ushort AnimId;
        public ushort SubAnimId;
        public uint AnimFileId;
    }
}