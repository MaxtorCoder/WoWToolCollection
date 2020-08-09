using FilenameGuesser.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FilenameGuesser.Readers
{
    public class M2Reader
    {
        private M2 M2;
        private uint lodCount = 0;
        private uint skinCount = 0;

        public void Process(Stream stream)
        {
            M2 = new M2
            {
                AnimationFileDataIds    = new List<AFID>(),
                TextureFileDataIds      = new List<uint>(),
                SkinFileDataIds         = new List<uint>(),
                LodSkinFileDataIds      = new List<uint>()
            };

            var currentPos = 0L;
            using (var reader = new BinaryReader(stream))
            {
                while (currentPos < reader.BaseStream.Length)
                {
                    var chunkId   = new string(reader.ReadChars(4));
                    var chunkSize = reader.ReadUInt32();

                    currentPos = reader.BaseStream.Position + chunkSize;
                    switch (chunkId)
                    {
                        case "MD21":
                            skinCount = ReadMD21(reader);

                            reader.BaseStream.Position = 8;
                            reader.Skip(chunkSize);
                            break;
                        case "LDV1":
                            reader.BaseStream.Position += 2;
                            lodCount = reader.ReadUInt16() - 1u;
                            reader.BaseStream.Position -= 4;
                            reader.Skip(chunkSize);

                            break;
                        case "TXID":
                            var textureCount = chunkSize / 4;
                            for (var i = 0; i < textureCount; ++i)
                                M2.TextureFileDataIds.Add(reader.ReadUInt32());

                            break;
                        case "SFID":
                            for (var i = 0; i < skinCount; ++i)
                                M2.SkinFileDataIds.Add(reader.ReadUInt32());

                            for (var i = 0; i < lodCount; ++i)
                                M2.LodSkinFileDataIds.Add(reader.ReadUInt32());

                            break;
                        case "AFID":
                            var animCount = chunkSize / 8;
                            for (var i = 0; i < animCount; ++i)
                            {
                                var afid = new AFID
                                {
                                    AnimId = reader.ReadUInt16(),
                                    SubAnimId = reader.ReadUInt16(),
                                    AnimFileId = reader.ReadUInt32()
                                };
                                M2.AnimationFileDataIds.Add(afid);
                            }

                            break;
                        default:
                            reader.Skip(chunkSize);
                            break;
                    }
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
            M2.Name = Encoding.UTF8.GetString(reader.ReadBytes((int)m2Name.Size)).Replace("\0", "");

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

                Program.AddToListfile(texture, $"{pathName}/{m2Name}_{texture}.blp");
            }
        }

        private void NameSkins()
        {
            var skinList = GetSkins();
            foreach (var skin in skinList)
            {
                var skinCount = skinList.IndexOf(skin);
                var pathName = Names.GetPathFromName(GetName());

                Program.AddToListfile(skin, $"{pathName}/{GetName()}{skinCount:00}.skin");
            }
        }

        private void NameLodSkins()
        {
            var lodSkinList = GetLodSkins();
            foreach (var lodksin in lodSkinList)
            {
                var skinCount = lodSkinList.IndexOf(lodksin);
                var pathName = Names.GetPathFromName(GetName());

                Program.AddToListfile(lodksin, $"{pathName}/{GetName()}_lod{skinCount:00}.skin");
            }
        }

        private void NameAnims()
        {
            var animList = GetAnims();
            foreach (var anim in animList)
            {
                var pathName = Names.GetPathFromName(GetName());

                Program.AddToListfile(anim.AnimFileId, $"{pathName}/{GetName()}{anim.AnimId:0000}_{anim.SubAnimId:00}.anim");
            }
        }
        #endregion

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
