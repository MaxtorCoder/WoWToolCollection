using BuildMonitor.IO;
using CASCLib;
using Discord.Webhook;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace BuildMonitor
{
    class Program
    {
        private static DiscordWebhookClient webhookClient;

        private static readonly string tacturl = "http://us.patch.battle.net:1119";
        private static readonly bool isMonitoring = true;

        private static string[] cdnUrls = { "http://level3.blizzard.com", "http://us.cdn.blizzard.com", "http://blzddist1-a.akamaihd.net" };
        private static string[] products = { "wow", "wowt", "wow_beta", "wowv", "wowv2", "wowdev", "wow_classic", "wow_classic_ptr", "wow_classic_beta" };
        private static Dictionary<string, uint> BranchVersions = new Dictionary<string, uint>();
        private static Dictionary<uint, VersionsInfo> BranchVersionInfo = new Dictionary<uint, VersionsInfo>(); 

        static void Main(string[] args)
        {
            webhookClient = new DiscordWebhookClient(File.ReadAllText("webhook.txt"));
            if (webhookClient == null)
                throw new Exception("Webhook is null!");

            Directory.CreateDirectory("cache");
            
            //foreach (var file in Directory.GetFiles("cache"))
            //{
            //    foreach (var product in products)
            //    {
            //        if (file.StartsWith(product))
            //        {
            //            var fileStream = File.Open(file, FileMode.Open);

            //            var memStream = new MemoryStream();
            //            fileStream.CopyToStream(memStream, fileStream.Length);

            //            ParseVersions(product, null);
            //        }
            //    }
            //}

            BranchVersions.Add("wow_beta", 34615);
            BranchVersionInfo.Add(34615, new VersionsInfo
            {
                CDNConfig = "bd42528d330e052042a0e3d16cc33bd2",
                BuildConfig = "4a8da195b8ca1375b61a87264b2e183a",
                ProductConfig = "1e81a3f523b5883e47b3cb4cbd5dff53",
                BuildId = 34615,
                VersionsName = "9.0.1.34615",
            });

            Log("Monitoring the patch servers...");
            while (isMonitoring)
            {
                Thread.Sleep(40000);
            
                foreach (var product in products)
                {
                    var stream = GetWebRequestStream($"{tacturl}/{product}/versions");
                    if (stream != null)
                        ParseVersions(product, stream);

                    Thread.Sleep(100);
                }
            }
        }

        /// <summary>
        /// Parse the 'versions' file from the servers.
        /// </summary>
        /// <param name="product"></param>
        /// <param name="stream"></param>
        static void ParseVersions(string product, MemoryStream stream)
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

                try
                {
                    if (BranchVersions[product] != versions.BuildId)
                    {
                        if (!product.Contains("wow_classic"))
                            HandleNewVersion(versions, product, stream);
                        
                        // Update the product with the new Build Id
                        BranchVersions[product] = versions.BuildId;
                    }
                }
                catch (Exception ex)
                {
                    Log(ex.ToString(), true);
                    return;
                }
            }
        }

        /// <summary>
        /// Diff the 2 root files.
        /// Completely taken from https://github.com/Marlamin/CASCToolHost/blob/master/CASCToolHost/Controllers/RootController.cs#L59
        /// </summary>
        /// <param name="oldRoot"></param>
        /// <param name="newRoot"></param>
        static IEnumerable<RootEntry> DiffRoot(string oldRootHash, string newRootHash)
        {
            try
            {
                var oldRootStream = RequestCDN($"tpr/wow/data/{oldRootHash.Substring(0, 2)}/{oldRootHash.Substring(2, 2)}/{oldRootHash}");
                var newRootStream = RequestCDN($"tpr/wow/data/{newRootHash.Substring(0, 2)}/{newRootHash.Substring(2, 2)}/{newRootHash}");
                if (oldRootStream == null || newRootStream == null)
                {
                    Log("Root is null", true);
                    return new List<RootEntry>();
                }

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
                        subEntry.ContentFlags.HasFlag(ContentFlags.Alternate) == false &&
                        (subEntry.LocaleFlags.HasFlag(LocaleFlags.All_WoW) || subEntry.LocaleFlags.HasFlag(LocaleFlags.enUS))
                    );

                    if (prioritized.FileDataId != 0)
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

                    if (originalFile.MD5.Equals(patchedFile.MD5))
                        continue;

                    modifiedFiles.Add(patchedFile);
                }

                Log($"Added: **{addedFiles.Count()}**\nRemoved: **{removedFiles.Count()}**\nModified: **{modifiedFiles.Count()}**");
                return addedFiles;
            }
            catch (Exception ex)
            {
                Log(ex.ToString(), true);
                return new List<RootEntry>();
            }
        }

        /// <summary>
        /// Handle the new version for the product.
        /// </summary>
        /// <param name="versions"></param>
        /// <param name="product"></param>
        /// <param name="stream"></param>
        static void HandleNewVersion(VersionsInfo versions, string product, MemoryStream stream)
        {
            var buildId = BranchVersions[product];
            var oldVersion = BranchVersionInfo[buildId];

            Log($"`{product}` got a new update!\n\n" +
                $"```BuildId       : {buildId} -> {versions.BuildId}\n" +
                $"CDNConfig     : {oldVersion.CDNConfig.Substring(0, 6)} -> {versions.CDNConfig.Substring(0, 6)}\n" +
                $"BuildConfig   : {oldVersion.BuildConfig.Substring(0, 6)} -> {versions.BuildConfig.Substring(0, 6)}\n" +
                $"ProductConfig : {oldVersion.ProductConfig.Substring(0, 6)} -> {versions.ProductConfig.Substring(0, 6)}```");

            File.Delete($"cache/{product}_{oldVersion.BuildId}.versions");
            File.WriteAllBytes($"cache/{product}_{versions.BuildId}.versions", stream.ToArray());

            // Check if the products are not encrypted..
            if (product == "wowdev" || product == "wowv" || product == "wowv2")
                return;

            Console.WriteLine($"Getting 'root' from '{versions.BuildConfig}'");
            var oldRoot = BuildConfigToRoot(RequestCDN($"tpr/wow/config/{oldVersion.BuildConfig.Substring(0, 2)}/{oldVersion.BuildConfig.Substring(2, 2)}/{oldVersion.BuildConfig}"));
            var newRoot = BuildConfigToRoot(RequestCDN($"tpr/wow/config/{versions.BuildConfig.Substring(0, 2)}/{versions.BuildConfig.Substring(2, 2)}/{versions.BuildConfig}"));

            var addedFiles = DiffRoot(oldRoot.Item1, newRoot.Item1).ToList();

            if (addedFiles.Count > 1)
                FilenameGuesser.ProcessFiles(product, addedFiles, (oldVersion.BuildConfig, versions.BuildConfig), (oldVersion.CDNConfig, versions.CDNConfig), webhookClient);
        }

        /// <summary>
        /// Parse the build config into the root hash
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        static (string, string) BuildConfigToRoot(MemoryStream stream)
        {          
            if (stream == null)
                return (string.Empty, string.Empty);

            using (var reader = new StreamReader(stream))
            {
                reader.ReadLine();
                reader.ReadLine();

                var rootContentHash = reader.ReadLine().Split(" = ")[1];

                // Skip to encoding.
                for (var i = 0; i < 6; ++i)
                    reader.ReadLine();

                var encoding = reader.ReadLine().Split(new char[] { ' ', '=' }, StringSplitOptions.RemoveEmptyEntries)[2];
                var encodingStream = RequestCDN($"tpr/wow/data/{encoding.Substring(0,2)}/{encoding.Substring(2,2)}/{encoding}");
                if (encodingStream == null)
                {
                    Log("Encoding stream is null", true);
                    return (string.Empty, string.Empty);
                }

                Encoding.ParseEncoding(encodingStream);
                if (Encoding.EncodingDictionary.TryGetValue(rootContentHash.ToByteArray().ToMD5(), out var entry))
                    return (entry.ToHexString().ToLower(), encoding.ToLower());
                else
                    return (string.Empty, string.Empty);
            }
        }

        /// <summary>
        /// Get the webresponse stream.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        static MemoryStream GetWebRequestStream(string url)
        {
            try
            {
                var client = new HttpClient();
                var response = client.GetAsync(url).Result;

                if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted)
                    return new MemoryStream(response.Content.ReadAsByteArrayAsync().Result);
                else
                {
                    Log($"Error code: {response.StatusCode}", true);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString(), true);
                return null;
            }
        }

        /// <summary>
        /// Request a file from the CDN.
        /// </summary>
        /// <param name="url"></param>
        static MemoryStream RequestCDN(string url)
        {
            var client = new HttpClient();
            try
            {
                foreach (var cdn in cdnUrls)
                {
                    var response = client.GetAsync($"{cdn}/{url}").Result;

                    if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted)
                        return new MemoryStream(response.Content.ReadAsByteArrayAsync().Result);
                    else
                    {
                        Log($"{cdn}/{url} gave error code {response.StatusCode} ({(uint)response.StatusCode})", true);
                        continue;
                    }
                }

                Log("Both CDNs throw an error! Please check the CDN path! :smile:", true);
                return null;
            }
            catch (Exception ex)
            {
                Log(ex.ToString(), true);
                return null;
            }
        }

        /// <summary>
        /// Log message to console and webhook
        /// </summary>
        /// <param name="message"></param>
        public static void Log(string message, bool error = false)
        {
            // Replace ` and * because fck that
            Console.WriteLine(message.Replace("`", "").Replace("*", ""));

            if (error)
                webhookClient.SendMessageAsync($"<@376821416105869315> : ```{message}```");
            else
                webhookClient.SendMessageAsync(message);
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
