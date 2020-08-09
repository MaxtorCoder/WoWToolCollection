using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using BuildMonitor.IO.CASC;
using CASCLib;
using Discord.Webhook;
using Ribbit.Constants;
using Ribbit.Protocol;

namespace BuildMonitor
{
    public class BuildMonitor
    {
        private static readonly bool isMonitoring = true;

        private static string[] cdnUrls      = { "http://level3.blizzard.com", "http://us.cdn.blizzard.com", "http://blzddist1-a.akamaihd.net" };
        private static List<string> products = new List<string>();

        private static Dictionary<string, uint> versionsIds                 = new Dictionary<string, uint>();
        private static Dictionary<uint, VersionsInfo> versionsInfo          = new Dictionary<uint, VersionsInfo>(); 

        static void Main(string[] args)
        {
            // Initialize Discord
            DiscordServer.Initialize();

            // Getting all the files in "cache"
            foreach (var file in Directory.GetFiles("cache/"))
            {
                var filenameSplit = file.Split("_").ToList();
                filenameSplit.RemoveAt(filenameSplit.IndexOf(filenameSplit.Last()));

                var product = string.Join('_', filenameSplit);
                ParseVersions(Path.GetFileName(product), file);
            }

            using (var client = new Client(Region.US))
            {
                var summary = Ribbit.ParseSummary(client.Request("v1/summary").ToString());
                foreach (var entry in summary)
                {
                    if (entry.Key.Type == "cdn" || entry.Key.Type == "bgdl" || !entry.Key.Product.StartsWith("wow"))
                        continue;

                    products.Add(entry.Key.Product);

                    // Request the product versions file.
                    var request = client.Request($"v1/products/{entry.Key.Product}/versions").ToString();

                    // Parse the version file.
                    File.WriteAllText("cache/temp", request);
                    ParseVersions(entry.Key.Product, "cache/temp");
                    File.Delete("cache/temp");

                    // Cache the file
                    File.WriteAllText($"cache/{entry.Key.Product}_{versionsIds[entry.Key.Product]}", request);

                    Thread.Sleep(100);
                }

                if (isMonitoring)
                    DiscordServer.Log("Monitoring the patch servers...");

                while (isMonitoring)
                {
                    Thread.Sleep(60000);

                    foreach (var product in products)
                    {
                        // Request the product versions file.
                        var request = client.Request($"v1/products/{product}/versions").ToString();

                        // Parse the version file.
                        File.WriteAllText("cache/temp", request);
                        ParseVersions(product, "cache/temp");
                        File.Delete("cache/temp");

                        Thread.Sleep(100);
                    }
                }
            }
        }

        /// <summary>
        /// Parse the 'versions' file from the servers.
        /// </summary>
        static void ParseVersions(string product, string file)
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

                if (!versionsInfo.ContainsKey(versions.BuildId))
                    versionsInfo.Add(versions.BuildId, versions);

                if (!versionsIds.ContainsKey(product))
                    versionsIds.Add(product, versions.BuildId);

                if (versionsIds[product] != versions.BuildId)
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
        /// <param name="versions"></param>
        /// <param name="product"></param>
        /// <param name="stream"></param>
        static void HandleNewVersion(VersionsInfo versions, string product, string file)
        {
            var buildId = versionsIds[product];
            var oldVersion = versionsInfo[buildId];

            // Send the embed.
            DiscordServer.SendEmbed(product, oldVersion, versions);

            // Check if the products are not encrypted..
            if (product == "wowdev" || product == "wowv" || product == "wowv2" || product == "wow_classic")
                return;

            Console.WriteLine($"Getting 'root' from '{versions.BuildConfig}'");
            var oldRoot = BuildConfigToRoot(RequestCDN($"tpr/wow/config/{oldVersion.BuildConfig.Substring(0, 2)}/{oldVersion.BuildConfig.Substring(2, 2)}/{oldVersion.BuildConfig}"));
            var newRoot = BuildConfigToRoot(RequestCDN($"tpr/wow/config/{versions.BuildConfig.Substring(0, 2)}/{versions.BuildConfig.Substring(2, 2)}/{versions.BuildConfig}"));

            var addedFiles = Root.DiffRoot(oldRoot.Item1, newRoot.Item1).ToList();
            if (addedFiles.Count > 1)
                FilenameGuesser.ProcessFiles(product, addedFiles, oldVersion.BuildConfig, oldVersion.CDNConfig, versions.BuildId);

            File.Delete($"cache/{product}_{oldVersion.BuildId}");
            File.WriteAllText($"cache/{product}_{versions.BuildId}", File.ReadAllText(file));
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

                var encoding        = reader.ReadLine().Split(new char[] { ' ', '=' }, StringSplitOptions.RemoveEmptyEntries)[2];
                var encodingStream  = RequestCDN($"tpr/wow/data/{encoding.Substring(0,2)}/{encoding.Substring(2,2)}/{encoding}");

                if (encodingStream == null)
                    return (string.Empty, string.Empty);

                Encoding.ParseEncoding(encodingStream);
                if (Encoding.EncodingDictionary.TryGetValue(rootContentHash.ToByteArray().ToMD5(), out var entry))
                    return (entry.ToHexString().ToLower(), encoding.ToLower());
                else
                    return (string.Empty, string.Empty);
            }
        }

        /// <summary>
        /// Request a file from the CDN.
        /// </summary>
        /// <param name="url"></param>
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
                    else
                        DiscordServer.Log($"{cdn}/{url} gave error code {response.StatusCode} ({(uint)response.StatusCode})", true);
                }

                return null;
            }
            catch (Exception ex)
            {
                DiscordServer.Log(ex.ToString(), true);

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
