using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WoWFileGuesser.Constants;

namespace WoWFileGuesser
{
    public static class FileGuesser
    {
        #region Variables
        #region Dictionaries
        public static Dictionary<uint, string> ListfileEntry = new Dictionary<uint, string>();
        public static Dictionary<string, string> ContainDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        #endregion
        public static int IncrementName = 1, IncrementSkin = 0, IncrementTex = 0, IncrementLodSkin = 0;
        public static string[] splitM2 = null;
        #endregion

        public static void ProcessFile(string path)
        {
            byte[] data         = File.ReadAllBytes(path);
            string extension    = string.Empty;
            string fileName     = Path.GetFileName(path);
            string[] split      = fileName.Split('_');
            uint filedataId     = uint.Parse(split[0]);

            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                ChunkId ChunkId = (ChunkId)reader.ReadUInt32();
                switch (ChunkId)
                {
                    case ChunkId.MD21:
                        ReadM2(path, filedataId);
                        extension = "m2";
                        break;
                    case ChunkId.BLP2:
                        extension = "blp";
                        break;
                    case ChunkId.SKIN:
                        extension = "skin";
                        break;
                    case ChunkId.PHYS:
                        extension = "phys";
                        break;
                    case ChunkId.OGGS:
                        extension = "ogg";
                        break;
                    case ChunkId.AUD1:
                    case ChunkId.AUD2:
                    case ChunkId.MVER:
                    case ChunkId.TXVR:
                        break;
                }
                ms.Close();

                if (extension == "m2")
                    Console.WriteLine($"Hash: {split[1]} FileDataId: {filedataId} ChunkId: {ChunkId} Name: {M2Reader.Names[filedataId]}");
                else
                    Console.WriteLine($"Hash: {split[1]} FileDataId: {filedataId} ChunkId: {ChunkId}");

                NameFiles(path, extension, filedataId);
            }
        }

        private static void ReadM2(string path, uint fileDataId)
        {
            using (FileStream ms = File.OpenRead(path))
            using (BinaryReader br = new BinaryReader(ms))
            {
                while (ms.Position < ms.Length)
                {
                    M2Chunk ChunkId = (M2Chunk)br.ReadUInt32();
                    uint ChunkSize = br.ReadUInt32();
                    switch (ChunkId)
                    {
                        case M2Chunk.MD21:
                            M2Reader.ReadMD20(br, fileDataId);

                            br.BaseStream.Position = 0;
                            br.ReadBytes((int)ChunkSize + 8);
                            break;
                        case M2Chunk.LDV1:
                            M2Reader.ReadLDV1(br);
                            break;
                        case M2Chunk.SFID:
                            M2Reader.ReadSFID(br, fileDataId);
                            break;
                        case M2Chunk.TXID:
                            M2Reader.ReadTXID(br, ChunkSize, fileDataId);
                            break;
                        default:
                            SkipUnknownChunk(ms, ChunkSize);
                            break;
                    }
                }
            }
        }

        public static void NameFiles(string path, string extension, uint fileDataId)
        {
            if (extension == "m2")
            {
                splitM2 = M2Reader.Names[fileDataId].Split('_');

                if (ContainDict.ContainsKey(splitM2[0]))
                    ListfileEntry.Add(fileDataId, $@"{ContainDict[splitM2[0].ToLower()]}\{M2Reader.Names[fileDataId]}.{extension}");

                if (M2Reader.SkinIds.ContainsValue(fileDataId))
                {
                    try
                    {
                        uint skinFileId = M2Reader.SkinIds.FirstOrDefault(x => x.Value == fileDataId).Key;
                        ListfileEntry.Add(skinFileId, $@"{ContainDict[splitM2[0]]}\{M2Reader.SkinFiles[skinFileId]}.skin");
                    }
                    catch { }
                }

                if (M2Reader.LodSkinIds.ContainsValue(fileDataId))
                {
                    try
                    {
                        uint lodSkinFileId = M2Reader.LodSkinIds.FirstOrDefault(x => x.Value == fileDataId).Key;
                        ListfileEntry.Add(lodSkinFileId, $@"{ContainDict[splitM2[0]]}\{M2Reader.LodSkinFiles[lodSkinFileId]}.skin");
                    }
                    catch { }
                }
            }

            File.Move(path, Path.ChangeExtension(path, extension));
        }

        public static void AddListfileEntry(string path)
        {
            using (StreamWriter sw = new StreamWriter(path))
            {
                foreach (uint dataid in ListfileEntry.Keys)
                {
                    sw.WriteLine($"{dataid};{ListfileEntry[dataid]}");
                }
            }
        }

        public static void LoadFileTypes(string path)
        {
            using (StreamReader sr = new StreamReader(path))
            {
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    string[] split = line.Split(';');

                    ContainDict.Add(split[0], split[1]);
                }
            }
        }

        private static void SkipUnknownChunk(FileStream ms, uint ChunkSize)
        {
            ms.Seek(ChunkSize, SeekOrigin.Current);
        }
    }
}
