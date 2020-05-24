using BuildMonitor.IO;
using BuildMonitor.Model;
using BuildMonitor.Util;
using CASCLib;
using DBFileReaderLib;
using Discord.Webhook;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace BuildMonitor
{
    public static class FilenameGuesser
    {
        public static ConcurrentDictionary<uint, string> AddedFileDataIds = new ConcurrentDictionary<uint, string>();
        public static ConcurrentDictionary<uint, string> FDIDFilename = new ConcurrentDictionary<uint, string>();

        private static Dictionary<uint, string> Listfile = new Dictionary<uint, string>();
        private static DiscordWebhookClient webhookClient;

        /// <summary>
        /// Process the newly added files and automatically name them.
        /// </summary>
        public static void ProcessFiles(string product, List<RootEntry> entries, (string oldBuildConfig, string newBuildConfig) buildConfig, (string oldCdnConfig, string newCdnConfig) cdnconfig, DiscordWebhookClient webhook)
        {
            if (webhook != null)
                webhookClient = webhook;

            // Read the original listfile first.
            new Thread(ReadOriginalListfile).Start();

            // Load old casc
            Console.WriteLine("Loading old casc...");
            var oldCascHandler = CASCHandler.OpenSpecificStorage(product, buildConfig.oldBuildConfig, cdnconfig.oldCdnConfig);
            oldCascHandler.Root.SetFlags(LocaleFlags.All_WoW);

            // Load new casc
            Console.WriteLine("Loading new casc...");
            var newCascHandler = CASCHandler.OpenSpecificStorage(product, buildConfig.newBuildConfig, cdnconfig.newCdnConfig);
            newCascHandler.Root.SetFlags(LocaleFlags.All_WoW);

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            Console.WriteLine($"Processing {entries.Count} root entries");
            Parallel.ForEach(entries, entry =>
            {
                if (newCascHandler.FileExists((int)entry.FileDataId))
                {
                    using (var stream = newCascHandler.OpenFile((int)entry.FileDataId))
                    {
                        if (stream == null)
                            return;

                        using (var reader = new BinaryReader(stream))
                        {
                            var chunkId = (Chunk)reader.ReadUInt32().FlipUInt();
                            if (chunkId == Chunk.MD21)
                            {
                                reader.BaseStream.Position = 0;
                                var m2Reader = new M2Reader(reader);
                                var pathName = Names.GetPathFromName(m2Reader.GetName());

                                AddToListfile(entry.FileDataId, $"{pathName}/{m2Reader.GetName()}.m2");

                                // Name all the files.
                                m2Reader.NameAllFiles();
                            }

                            // Close the streams to save memory.
                            reader.Close();
                            stream.Close();
                        }
                    }
                }
                else
                    Program.Log($"{entry.FileDataId} does not exist!!", true);
            });

            stopWatch.Stop();
            Console.WriteLine($"Finished processing {entries.Count} root entries in {stopWatch.Elapsed}\n");

            // Diff the 2 Map.db2 files.
            Program.Log("Diffing Map.db2 for new map entries...");
            var oldMapStorage = new DBReader(oldCascHandler.OpenFile(1349477)).GetRecords<Map>();
            var newMapStorage = new DBReader(newCascHandler.OpenFile(1349477)).GetRecords<Map>();

            if (oldMapStorage.Count < newMapStorage.Count)
            {
                Parallel.ForEach(newMapStorage, entry =>
                {
                    if (!oldMapStorage.ContainsKey(entry.Key))
                    {
                        Console.WriteLine($"New map: {entry.Value.Directory} ({entry.Value.MapName}) with WDT {entry.Value.WdtFileDataId}");
                        if (entry.Value.WdtFileDataId != 0 && !Listfile.ContainsKey(entry.Value.WdtFileDataId))
                            Console.WriteLine($"{entry.Value.WdtFileDataId} does not exist in the listfile, yet.");

                        if (entry.Value.WdtFileDataId != 0)
                        {
                            Console.WriteLine($"{entry.Value.WdtFileDataId} exists in the current unknown file list.");

                            var wdt = new WDTReader();
                            wdt.ReadWDT(newCascHandler, entry.Value.WdtFileDataId);

                            AddToListfile(entry.Value.WdtFileDataId, $"world/maps/{entry.Value.Directory}/{entry.Value.Directory}.wdt");
                            foreach (var maid in wdt.MAIDs)
                            {
                                AddToListfile(maid.Value.RootADT,           $"world/maps/{entry.Value.Directory}/{entry.Value.Directory}_{maid.Key}.adt");
                                AddToListfile(maid.Value.Obj0ADT,           $"world/maps/{entry.Value.Directory}/{entry.Value.Directory}_{maid.Key}_obj0.adt");
                                AddToListfile(maid.Value.Obj1ADT,           $"world/maps/{entry.Value.Directory}/{entry.Value.Directory}_{maid.Key}_obj1.adt");
                                AddToListfile(maid.Value.Tex0ADT,           $"world/maps/{entry.Value.Directory}/{entry.Value.Directory}_{maid.Key}_tex0.adt");
                                AddToListfile(maid.Value.LodADT,            $"world/maps/{entry.Value.Directory}/{entry.Value.Directory}_{maid.Key}_lod.adt");
                                AddToListfile(maid.Value.MapTexture,        $"world/maptextures/{entry.Value.Directory}/{entry.Value.Directory}_{maid.Key}.blp");
                                AddToListfile(maid.Value.MapTextureN,       $"world/maptextures/{entry.Value.Directory}/{entry.Value.Directory}_{maid.Key}_n.blp");
                                AddToListfile(maid.Value.MinimapTexture,    $"world/minimaps/{entry.Value.Directory}/{entry.Value.Directory}_{maid.Key}.blp");
                            }
                        }
                    }
                });
            }

            // Generate the listfile now.
            GenerateListfile();
        }

        /// <summary>
        /// Read the original listfile to check
        /// if a file is already named or not.
        /// </summary>
        private static void ReadOriginalListfile()
        {
            if (File.Exists("listfile.csv"))
                File.Delete("listfile.csv");

            using (var client = new WebClient())
            {
                client.DownloadFile("https://wow.tools/casc/listfile/download/csv/unverified", "listfile.csv");

                using (var reader = new StreamReader("listfile.csv"))
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

        /// <summary>
        /// Add a new listfile entry to the listfile.
        /// </summary>
        /// <param name="fileDataId"></param>
        /// <param name="filename"></param>
        public static void AddToListfile(uint fileDataId, string filename)
        {
            if (Listfile.TryGetValue(fileDataId, out var listfileFile))
            {
                if (listfileFile.Contains("unnamed"))
                    AddedFileDataIds.TryAdd(fileDataId, filename);
            }
            else
                AddedFileDataIds.TryAdd(fileDataId, filename);
        }

        /// <summary>
        /// Generate the listfile from the newest files
        /// and send the file over the webhook so it can
        /// be submitted to Marlamin's website.
        /// </summary>
        private static void GenerateListfile()
        {
            using (var writer = new StreamWriter("listfile_exported.csv"))
            {
                var keys = AddedFileDataIds.Keys.ToList();
                keys.Sort();

                foreach (var entry in keys)
                    writer.WriteLine($"{entry};{AddedFileDataIds[entry]}");

                writer.Close();
            }

            // Send the file over the webhook
            webhookClient.SendFileAsync("listfile_exported.csv", $"**{AddedFileDataIds.Count}** new listfile entries:");
        }

        public enum Chunk
        {
            // M2 Chunks
            MD21 = 1296314929,
            TXAC = 1415070019,
            EXP2 = 1163415602,
            PGD1 = 1346847793,
            LDV1 = 1279546929,
            SFID = 1397115204,
            TXID = 1415072068,
            AFID = 1095125316,

            // WDT Chunks
            MAID = 1296124228,
        }
    }
}
