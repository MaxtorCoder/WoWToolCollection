using BuildMonitor.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static BuildMonitor.FilenameGuesser;

namespace BuildMonitor.IO
{
    public class M2Reader
    {
        private BinaryReader Reader;
        private M2 M2;

        private uint lodCount = 0;
        private uint skinCount = 0;

        /// <summary>
        /// Create a new instance of <see cref="M2Reader"/>
        /// </summary>
        /// <param name="reader"></param>
        public M2Reader(BinaryReader reader)
        {
            Reader = reader;
            Process();
        }

        /// <summary>
        /// Process the M2 Files
        /// </summary>
        private void Process()
        {
            var currentPos = 0L;

            M2 = new M2
            {
                AnimationFileDataIds    = new List<AFID>(),
                TextureFileDataIds      = new List<uint>(),
                SkinFileDataIds         = new List<uint>(),
                LodSkinFileDataIds      = new List<uint>()
            };

            while (currentPos < Reader.BaseStream.Length)
            {
                var chunkId     = (Chunk)Reader.ReadUInt32().FlipUInt();
                var chunkSize   = Reader.ReadUInt32();

                currentPos = Reader.BaseStream.Position + chunkSize;
                switch (chunkId)
                {
                    case Chunk.MD21:
                        skinCount = ReadMD21(Reader);

                        Reader.BaseStream.Position = 8;
                        Skip(chunkSize);
                        break;
                    case Chunk.LDV1:
                        Reader.BaseStream.Position += 2;
                        lodCount = Reader.ReadUInt16() - 1u;
                        Reader.BaseStream.Position -= 4;
                        Skip(chunkSize);

                        break;
                    case Chunk.TXID:
                        var textureCount = chunkSize / 4;
                        for (var i = 0; i < textureCount; ++i)
                            M2.TextureFileDataIds.Add(Reader.ReadUInt32());

                        break;
                    case Chunk.SFID:
                        for (var i = 0; i < skinCount; ++i)
                            M2.SkinFileDataIds.Add(Reader.ReadUInt32());

                        for (var i = 0; i < lodCount; ++i)
                            M2.LodSkinFileDataIds.Add(Reader.ReadUInt32());

                        break;
                    case Chunk.AFID:
                        var animCount = chunkSize / 8;
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
                    default:
                        Skip(chunkSize);
                        break;
                }
            }
        }

        /// <summary>
        /// Read MD21 header.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private uint ReadMD21(BinaryReader reader)
        {
            reader.ReadBytes(8);

            var m2Name = reader.ReadM2Array();

            // Junk Code
            reader.ReadUInt32();
            for (var i = 0; i < 6; ++i)
                reader.ReadM2Array();

            var skinCount = reader.ReadUInt32();

            reader.BaseStream.Position = m2Name.Offset + 8;
            M2.Name = System.Text.Encoding.UTF8.GetString(reader.ReadBytes((int)m2Name.Size)).Replace("\0", "");

            return skinCount;
        }

        #region Name Functions
        /// <summary>
        /// Name all files corresponding to this M2
        /// </summary>
        public void NameAllFiles()
        {
            NameTextures();
            NameSkins();
            NameLodSkins();
            NameAnims();
        }

        private void NameTextures()
        {
            foreach (var texture in GetTextures())
            {
                if (texture == 0)
                    continue;

                var pathName = Names.GetPathFromName(GetName());
                string m2Name = GetName();

                AddToListfile(texture, $"{pathName}/{m2Name}_{texture}.blp");
            }
        }

        private void NameSkins()
        {
            var skinList = GetSkins();
            foreach (var skin in skinList)
            {
                var skinCount = skinList.IndexOf(skin);
                var pathName = Names.GetPathFromName(GetName());

                AddToListfile(skin, $"{pathName}/{GetName()}{skinCount:00}.skin");
            }
        }

        private void NameLodSkins()
        {
            var lodSkinList = GetLodSkins();
            foreach (var lodksin in lodSkinList)
            {
                var skinCount = lodSkinList.IndexOf(lodksin);
                var pathName = Names.GetPathFromName(GetName());

                AddToListfile(lodksin, $"{pathName}/{GetName()}_lod{skinCount:00}.skin");
            }
        }

        private void NameAnims()
        {
            var animList = GetAnims();
            foreach (var anim in animList)
            {
                var pathName = Names.GetPathFromName(GetName());

                AddToListfile(anim.AnimFileId, $"{pathName}/{GetName()}{anim.AnimId:0000}_{anim.SubAnimId:00}.anim");
            }
        }
        #endregion

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
