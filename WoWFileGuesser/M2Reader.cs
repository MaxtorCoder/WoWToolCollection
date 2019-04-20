using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WoWFileGuesser.Constants;

namespace WoWFileGuesser
{
    public static class M2Reader
    {
        /// <summary>
        /// Skin file
        /// </summary>
        public static Dictionary<uint, string> SkinFiles    = new Dictionary<uint, string>();
        public static Dictionary<uint, uint> SkinIds        = new Dictionary<uint, uint>();
        public static Dictionary<uint, string> LodSkinFiles = new Dictionary<uint, string>();
        public static Dictionary<uint, uint> LodSkinIds     = new Dictionary<uint, uint>();

        /// <summary>
        /// BLP File
        /// </summary>
        public static Dictionary<uint, string> TexFiles     = new Dictionary<uint, string>();

        /// <summary>
        /// Anim File
        /// </summary>
        public static Dictionary<uint, string> AnimFiles    = new Dictionary<uint, string>();
        public static Dictionary<uint, uint> AnimFileIds    = new Dictionary<uint, uint>();

        /// <summary>
        /// M2 Names
        /// </summary>
        public static Dictionary<uint, string> Names = new Dictionary<uint, string>();
        public static string LastName = string.Empty;

        /// <summary>
        /// Misc
        /// </summary>
        public static LodData lodData = new LodData();
        public static uint nViews = 0;

        public static void ReadMD20(BinaryReader br, uint fileDataId)
        {
            br.ReadBytes(8);        // Skip MD20 and Version
            uint SizeName   = br.ReadUInt32();
            uint OfsName    = br.ReadUInt32();
            br.ReadBytes(52);       // Skip to nViews;
            nViews          = br.ReadUInt32();
            br.BaseStream.Position = OfsName + 8;
            string ModelName = new string(br.ReadChars((int)SizeName - 1));

            Names.Add(fileDataId, ModelName);
        }

        public static void ReadSFID(BinaryReader br, uint fileDataId)
        {
            FileGuesser.IncrementLodSkin = 0;
            FileGuesser.IncrementSkin = 0;

            for (int i = 0; i < nViews; i++)
            {
                uint skinFileId = br.ReadUInt32();

                if (lodData.LodCount > 0)
                {
                    uint lodSkinFileId = br.ReadUInt32();
                    
                    if (fileDataId != 2622502)
                    {
                        LodSkinIds.Add(lodSkinFileId, fileDataId);
                        LodSkinFiles.Add(lodSkinFileId, $"{Names[fileDataId]}_lod0{FileGuesser.IncrementLodSkin}");
                        FileGuesser.IncrementLodSkin++;
                    }
                }

                SkinIds.Add(skinFileId, fileDataId);
                SkinFiles.Add(skinFileId, $"{Names[fileDataId]}0{FileGuesser.IncrementSkin}");
                FileGuesser.IncrementSkin++;
            }
        }

        public static void ReadLDV1(BinaryReader br)
        {
            lodData.Unk0        = br.ReadUInt16();
            lodData.LodCount    = br.ReadUInt16() - 1;
            lodData.Unk2        = br.ReadSingle();
            br.ReadUInt64();
        }

        public static void ReadTXID(BinaryReader br, uint ChunkSize, uint fileDataId)
        {
            FileGuesser.IncrementTex = 0;
            var numTex = ChunkSize / 4;
            for (int i = 0; i < numTex; i++)
            {
                uint Tex = br.ReadUInt32();

                if (!TexFiles.ContainsKey(Tex))
                    TexFiles.Add(Tex, $"{Names[fileDataId]}0{FileGuesser.IncrementTex}");

                FileGuesser.IncrementTex++;
            }
        }
    }
}
