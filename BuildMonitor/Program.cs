using BuildMonitor.IO;
using BuildMonitor.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web;

namespace BuildMonitor
{
    class Program
    {
        private static readonly string tacturl = "http://us.patch.battle.net:1119";
        private static readonly bool isMonitoring = true;

        private static string[] products = { "wow", "wowt", "wow_beta", "wowv", "wowdev", "wow_classic", "wow_classic_ptr", "wow_classic_beta" };
        private static Dictionary<string, uint> BranchVersions = new Dictionary<string, uint>();
        private static Dictionary<uint, VersionsInfo> BranchVersionInfo = new Dictionary<uint, VersionsInfo>(); 

        static void Main(string[] args)
        {
            Directory.CreateDirectory("cache");
            
            foreach (var product in products)
                ParseVersions(product, GetWebRequestStream($"{tacturl}/{product}/versions"));
            
            Console.WriteLine("Monitoring the patch servers...");
            while (isMonitoring)
            {
                Thread.Sleep(10000);
            
                foreach (var product in products)
                    ParseVersions(product, GetWebRequestStream($"{tacturl}/{product}/versions"));
            }
        }

        /// <summary>
        /// Parse the 'versions' file from the servers.
        /// </summary>
        /// <param name="product"></param>
        /// <param name="stream"></param>
        static void ParseVersions(string product, Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                // Skip the first 2 lines.
                reader.ReadLine();
                reader.ReadLine();

                var versions = new VersionsInfo();

                var line = reader.ReadLine();
                var lineSplit = line.Split('|');

                versions.Region         = lineSplit[0];
                versions.BuildConfig    = lineSplit[1];
                versions.CDNConfig      = lineSplit[2];

                if (lineSplit[3] != string.Empty)
                    versions.KeyRing = lineSplit[3];

                versions.BuildId        = uint.Parse(lineSplit[4]);
                versions.VersionsName   = lineSplit[5];
                versions.ProductConfig  = lineSplit[6];

                if (!BranchVersionInfo.ContainsKey(versions.BuildId))
                    BranchVersionInfo.Add(versions.BuildId, versions);

                if (!BranchVersions.ContainsKey(product))
                    BranchVersions.Add(product, versions.BuildId);

                // Copy the current stream to the MemoryStream
                // so we can convert it to raw bytes.
                var memStream = new MemoryStream();
                stream.CopyTo(memStream);

                if (BranchVersions[product] != versions.BuildId)
                {
                    var buildId = BranchVersions[product];
                    var oldVersion = BranchVersionInfo[buildId];

                    Console.WriteLine($"{product} got a new update!");
                    Console.WriteLine($"BuildId       : {buildId} -> {versions.BuildId}");
                    Console.WriteLine($"CDNConfig     : {oldVersion.CDNConfig.Substring(0, 5)} -> {versions.CDNConfig.Substring(0, 5)}");
                    Console.WriteLine($"BuildConfig   : {oldVersion.BuildConfig.Substring(0, 5)} -> {versions.BuildConfig.Substring(0, 5)}");
                    Console.WriteLine($"ProductConfig : {oldVersion.ProductConfig.Substring(0, 5)} -> {versions.ProductConfig.Substring(0, 5)}");

                    File.Delete($"cache/{product}_{oldVersion.BuildId}.versions");
                    File.WriteAllBytes($"cache/{product}_{versions.BuildId}.versions", memStream.ToArray());

                    Console.WriteLine($"Getting 'root' from '{versions.BuildConfig.Substring(0, 5)}'");
                    var oldRoot = BuildConfigToRoot(GetWebRequestStream($"http://us.cdn.blizzard.com/tpr/{product}/config/{oldVersion.BuildConfig.Substring(0, 2)}/{oldVersion.BuildConfig.Substring(2, 2)}/{oldVersion.BuildConfig}"));
                    var newRoot = BuildConfigToRoot(GetWebRequestStream($"http://us.cdn.blizzard.com/tpr/{product}/config/{versions.BuildConfig.Substring(0, 2)}/{versions.BuildConfig.Substring(2, 2)}/{versions.BuildConfig}"));

                    DiffRoot(oldRoot, newRoot);
                }

                if (!File.Exists($"cache/{product}_{versions.BuildId}.versions"))
                    File.WriteAllBytes($"cache/{product}_{versions.BuildId}.versions", memStream.ToArray());
            }
        }

        /// <summary>
        /// Diff the 2 root files.
        /// 
        /// Completely taken from https://github.com/Marlamin/CASCToolHost/blob/master/CASCToolHost/Controllers/RootController.cs#L59
        /// </summary>
        /// <param name="oldRoot"></param>
        /// <param name="newRoot"></param>
        static void DiffRoot(string oldRootHash, string newRootHash)
        {
            var oldRootStream = GetWebRequestStream($"http://us.cdn.blizzard.com/tpr/wow/data/{oldRootHash.Substring(0, 2)}/{oldRootHash.Substring(2, 2)}/{oldRootHash}");
            var newRootStream = GetWebRequestStream($"http://us.cdn.blizzard.com/tpr/wow/data/{newRootHash.Substring(0, 2)}/{newRootHash.Substring(2, 2)}/{newRootHash}");

            var rootFromEntries = Root.ParseRoot(oldRootStream).FileDataIds;
            var rootToEntries = Root.ParseRoot(newRootStream).FileDataIds;

            var fromEntries = rootFromEntries.Keys.ToHashSet();
            var toEntries = rootToEntries.Keys.ToHashSet();

            var commonEntries = fromEntries.Intersect(toEntries);
            var removedEntries = fromEntries.Except(commonEntries);
            var addedEntries = toEntries.Except(commonEntries);

            static RootEntry Prioritize(List<RootEntry> entries)
            {
                var prioritized = entries.FirstOrDefault(subEntry =>
                    subEntry.contentFlags.HasFlag(ContentFlags.LowViolence) == false &&
                    (subEntry.localeFlags.HasFlag(LocaleFlags.All_WoW) || subEntry.localeFlags.HasFlag(LocaleFlags.enUS))
                );

                if (prioritized.fileDataId != 0)
                    return prioritized;
                else
                    return entries.First();
            }

            var addedFiles = addedEntries.Select(entry => rootToEntries[entry]).Select(Prioritize);
            var removedFiles = removedEntries.Select(entry => rootFromEntries[entry]).Select(Prioritize);

            var modifiedFiles = new List<RootEntry>();
            foreach (var entry in commonEntries)
            {
                var originalFile = Prioritize(rootFromEntries[entry]);
                var patchedFile = Prioritize(rootToEntries[entry]);

                if (originalFile.md5.Equals(patchedFile.md5))
                    continue;

                modifiedFiles.Add(patchedFile);
            }

            Console.WriteLine($"Added: {addedFiles.Count()} Removed: {removedFiles.Count()} Modified: {modifiedFiles.Count()} Common: {commonEntries.Count()}");
        }

        /// <summary>
        /// Parse the build config into the root hash
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        static string BuildConfigToRoot(MemoryStream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                reader.ReadLine();
                reader.ReadLine();

                var rootContentHash = reader.ReadLine().Split(" = ")[1];

                // Skip to encoding.
                for (var i = 0; i < 6; ++i)
                    reader.ReadLine();

                var encoding = reader.ReadLine().Split(new char[] { ' ', '=' }, StringSplitOptions.RemoveEmptyEntries)[2];
                var encodingStream = GetWebRequestStream($"http://us.cdn.blizzard.com/tpr/wow/data/{encoding.Substring(0,2)}/{encoding.Substring(2,2)}/{encoding}");
                Encoding.ParseEncoding(encodingStream);

                if (Encoding.EncodingDictionary.TryGetValue(rootContentHash.ToByteArray().ToMD5(), out var entry))
                    return entry.ToHexString().ToLower();
                else
                    return string.Empty;
            }
        }

        /// <summary>
        /// Get the webresponse stream.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        static MemoryStream GetWebRequestStream(string url)
        {
            var client = new HttpClient();
            var response = client.GetByteArrayAsync(url).Result;

            return new MemoryStream(response);
        }
    }

    public class VersionsInfo
    {
        public string Region;
        public string BuildConfig;
        public string CDNConfig;
        public string KeyRing;
        public uint BuildId;
        public string VersionsName;
        public string ProductConfig;
    }
}
