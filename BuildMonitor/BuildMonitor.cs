using BuildMonitor.Discord;
using BuildMonitor.IO.CASC;
using CASCLib;
using System.Net.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Ribbit.Constants;
using Ribbit.Protocol;
using System.Threading;

namespace BuildMonitor
{
    public class BuildMonitor
    {
        private static readonly bool isMonitoring = true;

        private static string[] cdnUrls      = { "http://level3.blizzard.com", "http://us.cdn.blizzard.com", "http://blzddist1-a.akamaihd.net" };
        private static List<string> products = new List<string>();

        private static Dictionary<string, int> sequenceNumbers              = new Dictionary<string, int>();
        private static Dictionary<string, uint> versionsIds                 = new Dictionary<string, uint>();
        private static Dictionary<uint, VersionsInfo> versionInfo           = new Dictionary<uint, VersionsInfo>(); 
        public static Dictionary<string, VersionsInfo> VersionsInfo         = new Dictionary<string, VersionsInfo>(); 

        static void Main(string[] args)
        {
            // Initialize Discord
            DiscordManager.Initialize();

            if (!Directory.Exists("cache"))
                Directory.CreateDirectory("cache");

            using (var client = new Client(Region.US))
            {
                var summary = Ribbit.ParseSummary(client.Request("v1/summary").ToString());
                foreach (var entry in summary)
                {
                    if (entry.Key.Type == "cdn" || entry.Key.Type == "bgdl" || !entry.Key.Product.StartsWith("wow"))
                        continue;

                    sequenceNumbers.Add(entry.Key.Product, entry.Value);

                    // Request the product versions file.
                    // We request this at the start so we have old versions.
                    var request = client.Request($"v1/products/{entry.Key.Product}/versions").ToString();

                    // Parse the version file.
                    File.WriteAllText("cache/temp", request);
                    ParseVersions(entry.Key.Product, "cache/temp", false);
                    File.Delete("cache/temp");

                    // Cache the file
                    File.WriteAllText($"cache/{entry.Key.Product}_{versionsIds[entry.Key.Product]}", request);

                    Thread.Sleep(100);
                }

                while (isMonitoring)
                {
                    Thread.Sleep(50000);

                     // Request the product versions file.
                     summary = Ribbit.ParseSummary(client.Request("v1/summary").ToString());
                     foreach (var entry in summary)
                     {
                         if (entry.Key.Type == "cdn" || entry.Key.Type == "bgdl" || !entry.Key.Product.StartsWith("wow"))
                             continue;

                         // A new build happened
                         if (sequenceNumbers[entry.Key.Product] != entry.Value)
                         {
                             // Request the product versions file.
                             var request = client.Request($"v1/products/{entry.Key.Product}/versions").ToString();

                             // Parse the version file.
                             File.WriteAllText("cache/temp", request);
                             ParseVersions(entry.Key.Product, "cache/temp");
                             File.Delete("cache/temp");

                             sequenceNumbers[entry.Key.Product] = entry.Value;
                         }
                     }
                }
            }
        }

        /// <summary>
        /// Parse the 'versions' file from the servers.
        /// </summary>
        static void ParseVersions(string product, string file, bool newBuild = true)
        {
            using (var reader = new StreamReader(file))
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

                if (!versionInfo.ContainsKey(versions.BuildId))
                    versionInfo.Add(versions.BuildId, versions);

                if (!versionsIds.ContainsKey(product))
                    versionsIds.Add(product, versions.BuildId);

                if (!VersionsInfo.ContainsKey(product))
                    VersionsInfo.Add(product, versions);
                else
                    VersionsInfo[product] = versions;

                if (newBuild)
                {
                    HandleNewVersion(versions, product, file);

                    // Update the product with the new Build Id
                    versionsIds[product] = versions.BuildId;
                }
            }
        }

        /// <summary>
        /// Handle the new version for the product.
        /// </summary>
        static void HandleNewVersion(VersionsInfo versions, string product, string file)
        {
            var buildId = versionsIds[product];
            var oldVersion = versionInfo[buildId];

            // Skip CDN only changes.
            if (versions.BuildId == buildId)
                return;

            // Send the embed.
            DiscordManager.SendBuildMonitorMessage(product, oldVersion, versions);

            File.Delete($"cache/{product}_{oldVersion.BuildId}");
            File.WriteAllText($"cache/{product}_{versions.BuildId}", File.ReadAllText(file));

            // Check if the products are not encrypted..
            if (product == "wowdev" || product == "wowv" || product == "wowv2" || product == "wow_classic")
                return;

            Console.WriteLine($"Getting 'root' from '{versions.BuildConfig}'");
            var oldRoot = BuildConfigToRoot(RequestCDN($"tpr/wow/config/{oldVersion.BuildConfig.Substring(0, 2)}/{oldVersion.BuildConfig.Substring(2, 2)}/{oldVersion.BuildConfig}"));
            var newRoot = BuildConfigToRoot(RequestCDN($"tpr/wow/config/{versions.BuildConfig.Substring(0, 2)}/{versions.BuildConfig.Substring(2, 2)}/{versions.BuildConfig}"));

            var addedFiles = Root.DiffRoot(oldRoot.Item1, newRoot.Item1).ToList();
            if (addedFiles.Count > 1)
                FilenameGuesser.ProcessFiles(product, addedFiles, oldVersion.BuildConfig, oldVersion.CDNConfig, versions.BuildId);

            versionInfo.Remove(buildId);
            versionInfo[versions.BuildId] = versions;
        }

        /// <summary>
        /// Parse the build config into the root hash
        /// </summary>
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
                var line = string.Empty;
                while ((line = reader.ReadLine()) == "encoding")
                {
                    var encoding        = line.Split(" = ", StringSplitOptions.RemoveEmptyEntries)[2];
                    var encodingStream  = RequestCDN($"tpr/wow/data/{encoding.Substring(0, 2)}/{encoding.Substring(2, 2)}/{encoding}");

                    if (encodingStream == null)
                        return (string.Empty, string.Empty);

                    Encoding.ParseEncoding(encodingStream);
                    if (Encoding.EncodingDictionary.TryGetValue(rootContentHash.ToByteArray().ToMD5(), out var entry))
                        return (entry.ToHexString().ToLower(), encoding.ToLower());
                }
            }

            return (string.Empty, string.Empty);
        }

        /// <summary>
        /// Request a file from the CDN.
        /// </summary>
        public static MemoryStream RequestCDN(string url)
        {
            var client = new HttpClient();
            try
            {
                foreach (var cdn in cdnUrls)
                {
                    var response = client.GetAsync($"{cdn}/{url}").Result;

                    if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted)
                        return new MemoryStream(response.Content.ReadAsByteArrayAsync().Result);
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    public struct VersionsInfo
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
