using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FilenameGuesser.Readers;
using FilenameGuesser.Util;

namespace FilenameGuesser
{
    class Program
    {
        private static string UnknownFolderPath = @"D:\WoW\Tools\CASC\work\unknown";
        private static string UnknownTypePath = @"D:\WoW\Tools\CASC\work\Named";

        private static string[] Folders = { "M2", "OGG" };
        private static ConcurrentDictionary<uint, string> FileXFileDataId = new ConcurrentDictionary<uint, string>();
        private static ConcurrentDictionary<uint, string> Listfile = new ConcurrentDictionary<uint, string>();

        static void Main(string[] args)
        {
            var files = Directory.GetFiles(UnknownFolderPath, "*.*", SearchOption.AllDirectories);
            var watch = new Stopwatch();
            watch.Start();

            // Delete all files and folders
            if (Directory.Exists(UnknownTypePath))
                Directory.Delete(UnknownTypePath, true);

            // Recreate the folders
            Directory.CreateDirectory(UnknownTypePath);
            foreach (var folder in Folders)
                Directory.CreateDirectory($"{UnknownTypePath}\\{folder}");

            Parallel.ForEach(files, file =>
            {
                var fileDataId = uint.Parse(Path.GetFileName(file).Split('_')[1]);
                FileXFileDataId.TryAdd(fileDataId, file);
            });

            Console.WriteLine($"Processing {files.Length} files...");
            Parallel.ForEach(files, file =>
            {
                try
                {
                    var fileDataId = uint.Parse(Path.GetFileName(file).Split('_')[1]);

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

                                Listfile.TryAdd(fileDataId, $"{pathName}/{m2Reader.GetName()}.m2");

                                NameTextures(m2Reader);
                                NameSkins(m2Reader);
                                NameAnims(m2Reader);
                                NameLodSkins(m2Reader);

                                break;
                        }
                    }
                }
                catch (Exception ex) { }
            });

            watch.Stop();
            Console.WriteLine($"Finished processing {files.Length} files in {watch.Elapsed}");

            Console.WriteLine($"Writing listfile, {Listfile.Count} new entries");
            GenerateListfile();
            Console.ReadKey();
        }

        static void NameTextures(M2Reader m2Reader)
        {
            foreach (var texture in m2Reader.GetTextures())
            {
                if (texture == 0)
                    continue;

                var pathName = Names.GetPathFromName(m2Reader.GetName());
                string m2Name = m2Reader.GetName();

                if (!Listfile.ContainsKey(texture))
                    Listfile.TryAdd(texture, $"{pathName}/{m2Name}_{texture}.blp");
                else  // Shared Texture
                {
                    var split = m2Reader.GetName().Split('_').ToList();
                    split.Remove(split.Last());

                    m2Name = string.Join('_', split.ToArray());
                    Listfile[texture] = $"{pathName}/{m2Name}_{texture}.blp";
                }
            }
        }

        static void NameSkins(M2Reader m2Reader)
        {
            var skinList = m2Reader.GetSkins();
            foreach (var skin in skinList)
            {
                var skinCount = skinList.IndexOf(skin);
                var pathName = Names.GetPathFromName(m2Reader.GetName());
                Listfile.TryAdd(skin, $"{pathName}/{m2Reader.GetName()}{skinCount:00}.skin");
            }
        }

        static void NameLodSkins(M2Reader m2Reader)
        {
            var lodSkinList = m2Reader.GetLodSkins();
            foreach (var skin in lodSkinList)
            {
                var skinCount = lodSkinList.IndexOf(skin);
                var pathName = Names.GetPathFromName(m2Reader.GetName());
                Listfile.TryAdd(skin, $"{pathName}/{m2Reader.GetName()}_lod{skinCount:00}.skin");
            }
        }

        static void NameAnims(M2Reader m2Reader)
        {
            var animList = m2Reader.GetAnims();
            foreach (var anim in animList)
            {
                if (!FileXFileDataId.TryGetValue(anim.AnimFileId, out string animFilename))
                    continue;

                var pathName = Names.GetPathFromName(m2Reader.GetName());
                Listfile.TryAdd(anim.AnimFileId, $"{pathName}/{m2Reader.GetName()}{anim.AnimId:0000}_{anim.SubAnimId:00}.anim");
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
                foreach (var entry in Listfile)
                    writer.WriteLine($"{entry.Key};{entry.Value}");

                writer.Close();
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