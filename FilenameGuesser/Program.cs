using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FilenameGuesser.Readers;

namespace FilenameGuesser
{
    class Program
    {
        private static string UnknownFolderPath = @"D:\Games\WoW\CSCExplorer\Work\Unknown";
        private static string UnknownTypePath = @"D:\Games\WoW\CSCExplorer\Work\Named";

        private static string[] Folders = { "M2", "OGG", "ANIM" };
        private static ConcurrentDictionary<uint, string> FileXFileDataId = new ConcurrentDictionary<uint, string>();
        private static Dictionary<uint, string> Listfile = new Dictionary<uint, string>();
    
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
            
            Console.WriteLine($"Processing {files.Length} files..");
            Parallel.ForEach(files, file =>
            {
                var fileDataId = uint.Parse(Path.GetFileName(file).Split('_')[1]);

                if (!FileXFileDataId.ContainsKey(fileDataId))
                    FileXFileDataId.TryAdd(fileDataId, file);

                try
                {
                    using (var stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var reader = new BinaryReader(stream))
                    {
                        var chunkId = (Chunk) FlipUInt(reader.ReadUInt32());

                        var fileBytes = new byte[stream.Length];
                        stream.Position = 0;
                        stream.Read(fileBytes, 0, fileBytes.Length);

                        // Reset position back to 0
                        reader.BaseStream.Position = 0;
                        switch (chunkId)
                        {
                            case Chunk.MD21:
                                var m2Reader = new M2Reader(reader);
                                NameTextures(m2Reader);
                                NameSkins(m2Reader);

                                File.WriteAllBytes($"{UnknownTypePath}\\M2\\{m2Reader.GetName()}.m2", fileBytes);
                                break;
                            case Chunk.OGGS:
                                File.WriteAllBytes($"{UnknownTypePath}\\OGG\\{Path.GetFileNameWithoutExtension(file)}.ogg", fileBytes);
                                break;
                            case Chunk.AFM2:
                                File.WriteAllBytes($"{UnknownTypePath}\\ANIM\\{Path.GetFileNameWithoutExtension(file)}.anim", fileBytes);
                                break;
                        }

                        reader.Close();
                        stream.Close();
                    }
                }
                catch (Exception ex) { }
            });

            watch.Stop();
            Console.WriteLine($"Finished processing {files.Length} files in {watch.Elapsed}");
        }

        static void NameTextures(M2Reader m2Reader)
        {
            Parallel.ForEach(m2Reader.GetTextures(), texture =>
            {
                var texFilename = FileXFileDataId[texture];
                if (texFilename == null)
                    return;
                
                File.WriteAllBytes($"{UnknownTypePath}\\M2\\{m2Reader.GetName()}_{texture}.blp", File.ReadAllBytes(texFilename));
            });
        }

        static void NameSkins(M2Reader m2Reader)
        {
            var skinList = m2Reader.GetSkins();
            Parallel.ForEach(skinList, skin =>
            {
                var skinFilename = FileXFileDataId[skin];
                if (skinFilename == null)
                    return;

                var skinCount = skinList.IndexOf(skin);
                File.WriteAllBytes($"{UnknownTypePath}\\M2\\{m2Reader.GetName()}_{skinCount:00}.skin", File.ReadAllBytes(skinFilename));
            });
        }

        public static uint FlipUInt(uint n)
        {
            return (n << 24) | (((n >> 16) << 24) >> 16) | (((n << 16) >> 24) << 16) | (n >> 24);
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
        TXID = 1415072068
    }
}