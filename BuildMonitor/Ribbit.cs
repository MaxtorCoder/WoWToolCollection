using BuildMonitor.Discord;
using BuildMonitor.IO.CASC;
using BuildMonitor.Util;
using CASCLib;
using Ribbit.Parsing;
using Ribbit.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BuildMonitor
{
    using ProductStorage = Dictionary<string, uint>;
    using SequenceStorage = Dictionary<uint, uint>;

    public static class Ribbit
    {
        public static HashSet<string> RibbitProducts = new HashSet<string>();
        public static Dictionary<string, Versions> VersionsInfo = new Dictionary<string, Versions>();
        public static ProductStorage SequenceStore = new ProductStorage();

        private static SequenceStorage CurrentVersions = new SequenceStorage();
        private static List<string> removedProducts = new List<string>();

        /// <summary>
        /// Parse the "v1/summary" response.
        /// </summary>
        public static Dictionary<(string Product, string Type), uint> ParseSummary(string summary)
        {
            var summaryDictionary = new Dictionary<(string, string), uint>();
            var parsedFile = new BPSV(summary);

            foreach (var entry in parsedFile.data)
            {
                if (string.IsNullOrEmpty(entry[2]))
                    summaryDictionary.Add((entry[0], "version"), uint.Parse(entry[1]));
                else
                    summaryDictionary.Add((entry[0], entry[2].Trim()), uint.Parse(entry[1]));
            }

            return summaryDictionary;
        }

        /// <summary>
        /// Init <see cref="Ribbit"/>
        /// </summary>
        public static void InitRibbit(Client client)
        {
            // Delete the temp file, useless on initialize
            File.Delete("cache\\temp");

            var files = Directory.GetFiles("cache").OrderBy(x => x);
            foreach (var file in files)
            {
                if (Path.GetFileName(file) == "temp")
                    continue;

                var fileText = File.ReadAllText(file);
                if (fileText == string.Empty)
                    continue;

                var product = Path.GetFileName(file).Substring(0, Path.GetFileName(file).IndexOf('-'));
                var versionInfo = ParseVersions(fileText, product);
                if (versionInfo == null)
                    continue;

                if (SequenceStore.ContainsKey(product))
                {
                    if (SequenceStore[product] < versionInfo.SequenceNumber)
                    {
                        // Console.WriteLine($"[DBG]: Replacing {product} with seq {SequenceStore[product]} -> {versionInfo.SequenceNumber}");
                        SequenceStore[product] = versionInfo.SequenceNumber;
                    }
                    else
                        Console.WriteLine($"[DBG]: {product} has lower seq_number {versionInfo.SequenceNumber} -> {SequenceStore[product]}");
                }
                else
                    SequenceStore.Add(product, versionInfo.SequenceNumber);

                // Console.WriteLine($"[DBG]: Product: {product} with {versionInfo.SequenceNumber}");
                RibbitProducts.Add(product);
            }

            CheckForVersions(client, true);
        }

        /// <summary>
        /// Check the summary for versions.
        /// </summary>
        public static void CheckForVersions(Client client, bool init = false)
        {
            var summary = ParseSummary(client.RequestSummary())
                    .Where(x => x.Key.Product.StartsWith("wow"))
                    .Where(x => x.Key.Type == "version");

            foreach (var summaryEntry in summary)
            {
                // Check if this is a removed product, we don't want to analyze it million times a day.
                if (removedProducts.Contains(summaryEntry.Key.Product))
                    continue;

                // Request the product versions file.
                // We request this at the start so we have old versions.
                var request = client.RequestVersions(summaryEntry.Key.Product);
                if (request == string.Empty)
                    continue;

                var versionInfo = ParseVersions(request, summaryEntry.Key.Product);
                if (versionInfo == null)
                    continue;

                if (!VersionsInfo.ContainsKey(versionInfo.Product))
                    VersionsInfo.Add(versionInfo.Product, versionInfo);

                if (!RibbitProducts.Contains(summaryEntry.Key.Product))
                {
                    if (!init)
                        DiscordManager.SendDebugMessage($"Found new endpoint: **{summaryEntry.Key.Product}**");

                    RibbitProducts.Add(summaryEntry.Key.Product);
                }

                if (!init && summaryEntry.Value > SequenceStore[summaryEntry.Key.Product])
                {
                    Console.WriteLine($"[RBT]: New version for {summaryEntry.Key.Product} {summaryEntry.Value} -> {SequenceStore[summaryEntry.Key.Product]}");

                    HandleNewBuild(versionInfo, "cache/temp");

                    SequenceStore[summaryEntry.Key.Product] = summaryEntry.Value;
                }

                // Remove the temp file and write to cache.
                File.Delete("cache/temp");
                File.WriteAllText($"cache/{summaryEntry.Key.Product}-{versionInfo.BuildId}", request);
            }
        }

        /// <summary>
        /// Parse the <see cref="Ribbit"/> request and return <see cref="Versions"/>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static Versions ParseVersions(string request, string product)
        {
            File.WriteAllText("cache/temp", request);

            var versions = new Versions();
            if (!versions.Parse("cache/temp"))
            {
                removedProducts.Add(product);
                
                Console.WriteLine($"[RBT]: Something went wrong while parsing {product}");
                return null;
            }

            versions.Product = product;

            if (!CurrentVersions.ContainsKey(versions.SequenceNumber))
                CurrentVersions.Add(versions.SequenceNumber, versions.BuildId);

            return versions;
        }

        /// <summary>
        /// Handle the new <see cref="Versions"/>
        /// </summary>
        /// <param name="version"></param>
        public static async Task HandleNewBuild(Versions version, string file, string oldBuild = "")
        {
            if (!VersionsInfo.TryGetValue(version.Product, out var oldVersion))
            {
                // This is impossible to hit.
                Console.WriteLine($"[DBG]: {version.Product} does not have a versioninfo");
                return;
            }

            VersionsInfo[version.Product] = version;

            var oldBuildId = 0u;
            if (oldBuild != "")
                oldBuildId = uint.Parse(oldBuild);
            else
                oldBuildId = oldVersion.BuildId;

            if (oldBuildId == version.BuildId)
            {
                Console.WriteLine($"[RBT]: {version.Product} has same BuildID, CDN change");
                return;
            }

            Console.WriteLine($"[RBT]: Processing `{version.Product}`");
            File.WriteAllText($"cache/{version.Product}-{version.BuildId}", File.ReadAllText(file));

            if (version.Product.IsEncrypted())
            {
                DiscordManager.SendBuildMonitorMessage(version.Product, oldVersion, version, oldBuild != "");
                return;
            }

            Console.WriteLine($"[RBT]: Getting old BuildConfig '{oldVersion.BuildConfig}'");
            var oldRootStream = await HTTP.RequestCDN($"tpr/wow/config/{oldVersion.BuildConfig.Substring(0, 2)}/{oldVersion.BuildConfig.Substring(2, 2)}/{oldVersion.BuildConfig}");
            if (oldRootStream == null)
                return;

            Console.WriteLine($"[RBT]: Getting new BuildConfig '{version.BuildConfig}'");
            var newRootStream = await HTTP.RequestCDN($"tpr/wow/config/{version.BuildConfig.Substring(0, 2)}/{version.BuildConfig.Substring(2, 2)}/{version.BuildConfig}");
            if (newRootStream == null)
                return;

            var oldRoot = await Encoding.RetrieveRootHash(oldRootStream);
            var newRoot = await Encoding.RetrieveRootHash(newRootStream);

            var fileInfo = await Root.DiffRoot(oldRoot.Encoding.ToHexString().ToLower(), newRoot.Encoding.ToHexString().ToLower());
            version.FileInfo = fileInfo;

            DiscordManager.SendBuildMonitorMessage(version.Product, oldVersion, version, oldBuild != "");

            if (fileInfo.Added.Count > 1)
                FilenameGuesser.ProcessFiles(version.Product, fileInfo.Added, oldVersion.BuildConfig, oldVersion.CDNConfig, version.BuildId);
        }
    }
}
