using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FilenameGuesser.Readers;
using FilenameGuesser.Util;
using DBFileReaderLib;
using FilenameGuesser.DB2Structures;

namespace FilenameGuesser
{
    class Program
    {
        private static string UnknownFolderPath = @"D:\WoW\Tools\CASC\work\unknown";
        private static ConcurrentDictionary<uint, string> AddedFileDataIds = new ConcurrentDictionary<uint, string>();
        private static ConcurrentDictionary<uint, string> FileDataIdXFilename = new ConcurrentDictionary<uint, string>();
        private static Dictionary<uint, string> Listfile = new Dictionary<uint, string>();

        static void Main(string[] args)
        {
            // Read the original listfile.
            ReadOriginalListfile();

            var files = Directory.GetFiles(UnknownFolderPath, "*.*", SearchOption.AllDirectories);
            var watch = new Stopwatch();
            watch.Start();

            Console.WriteLine($"Processing {files.Length} files...");
            //foreach (var file in files)
            Parallel.ForEach(files, file =>
            {
                var fileDataId = uint.Parse(Path.GetFileName(file).Split('_')[1]);
                FileDataIdXFilename.TryAdd(fileDataId, file);
            
                using (var stream = new MemoryStream(File.ReadAllBytes(file)))
                using (var reader = new BinaryReader(stream))
                {
                    var chunkId = (Chunk)FlipUInt(reader.ReadUInt32());
            
                    reader.BaseStream.Position = 0;
                    switch (chunkId)
                    {
                        case Chunk.MD21:
                            var m2Reader = new M2Reader(reader);
                            var pathName = Names.GetPathFromName(m2Reader.GetName());
            
                            AddToListfile(fileDataId, $"{pathName}/{m2Reader.GetName()}.m2");
            
                            NameTextures(m2Reader);
                            NameSkins(m2Reader);
                            NameAnims(m2Reader);
                            NameLodSkins(m2Reader);
            
                            break;
                    }
                }
            });

            watch.Stop();
            Console.WriteLine($"Finished processing {files.Length} files in {watch.Elapsed}\n");

            // Read Map.db2
            var oldMapStorage = new DBReader("Map_old.db2").GetRecords<Map>();
            var newMapStorage = new DBReader("Map_new.db2").GetRecords<Map>();

            foreach (var entry in newMapStorage)
            {
                if (!oldMapStorage.ContainsKey(entry.Key))
                {
                    Console.WriteLine($"New map: {entry.Value.MapName} with WDT {entry.Value.WdtFileDataId}");
                    if (entry.Value.WdtFileDataId != 0 && !Listfile.ContainsKey(entry.Value.WdtFileDataId))
                        Console.WriteLine($"{entry.Value.WdtFileDataId} does not exist in the listfile, yet.");

                    if (entry.Value.WdtFileDataId != 0 && FileDataIdXFilename.ContainsKey(entry.Value.WdtFileDataId))
                        Console.WriteLine($"{entry.Value.WdtFileDataId} exists in the current unknown file list.");
                }
            }

            //Console.WriteLine($"Writing listfile, {AddedFileDataIds.Count} new entries");
            //GenerateListfile();
            //Console.ReadKey();
        }

        static void NameTextures(M2Reader m2Reader)
        {
            foreach (var texture in m2Reader.GetTextures())
            {
                if (texture == 0)
                    continue;

                var pathName = Names.GetPathFromName(m2Reader.GetName());
                string m2Name = m2Reader.GetName();

                AddToListfile(texture, $"{pathName}/{m2Name}_{texture}.blp");
            }
        }

        static void NameSkins(M2Reader m2Reader)
        {
            var skinList = m2Reader.GetSkins();
            foreach (var skin in skinList)
            {
                var skinCount = skinList.IndexOf(skin);
                var pathName = Names.GetPathFromName(m2Reader.GetName());

                AddToListfile(skin, $"{pathName}/{m2Reader.GetName()}{skinCount:00}.skin");
            }
        }

        static void NameLodSkins(M2Reader m2Reader)
        {
            var lodSkinList = m2Reader.GetLodSkins();
            foreach (var lodksin in lodSkinList)
            {
                var skinCount = lodSkinList.IndexOf(lodksin);
                var pathName = Names.GetPathFromName(m2Reader.GetName());

                AddToListfile(lodksin, $"{pathName}/{m2Reader.GetName()}_lod{skinCount:00}.skin");
            }
        }

        static void NameAnims(M2Reader m2Reader)
        {
            var animList = m2Reader.GetAnims();
            foreach (var anim in animList)
            {
                var pathName = Names.GetPathFromName(m2Reader.GetName());

                AddToListfile(anim.AnimFileId, $"{pathName}/{m2Reader.GetName()}{anim.AnimId:0000}_{anim.SubAnimId:00}.anim");
            }
        }

        public static uint FlipUInt(uint n)
        {
            return (n << 24) | (((n >> 16) << 24) >> 16) | (((n << 16) >> 24) << 16) | (n >> 24);
        }

        public static void GenerateListfile()
        {
            using (var writer = new StreamWriter("listfile.csv"))
            {
                var keys = AddedFileDataIds.Keys.ToList();
                keys.Sort();

                foreach (var entry in keys)
                    writer.WriteLine($"{entry};{AddedFileDataIds[entry]}");

                writer.Close();
            }
        }

        static void AddToListfile(uint fileDataId, string filename)
        {
            if (Listfile.TryGetValue(fileDataId, out var listfileFile))
            {
                if (listfileFile.Contains("unnamed"))
                    AddedFileDataIds.TryAdd(fileDataId, filename);
            }
            else
                AddedFileDataIds.TryAdd(fileDataId, filename);
        }

        public static void ReadOriginalListfile()
        {
            using (var reader = new StreamReader("listfile_export.csv"))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var lineSplit = line.Split(';');

                    if (!Listfile.ContainsKey(uint.Parse(lineSplit[0])))
                        Listfile.Add(uint.Parse(lineSplit[0]), lineSplit[1]);
                }
            }
        }
    }

    public enum Chunk
    {
        MVER = 1380275789,
        BLP2 = 1112297522,
        OGGS = 1332176723,
        SKIN = 1397442894,
        AFM2 = 1095126322,
        RVXT = 1381390420,
        
        // M2 Chunks
        MD21 = 1296314929,
        TXAC = 1415070019,
        EXP2 = 1163415602,
        PGD1 = 1346847793,
        LDV1 = 1279546929,
        SFID = 1397115204,
        TXID = 1415072068,
        AFID = 1095125316
    }
}