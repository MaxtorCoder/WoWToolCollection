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
    using VersionStorage = Dictionary<uint, Versions>;

    public static class Ribbit
    {
        public static HashSet<string> RibbitProducts = new HashSet<string>();

        public static ProductStorage SequenceStore = new ProductStorage();
        public static VersionStorage VersionStore = new VersionStorage();
        public static ProductStorage CurrentVersions = new ProductStorage();
        public static Dictionary<string, Versions> VersionsInfo = new Dictionary<string, Versions>();

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

            foreach (var file in Directory.GetFiles("cache"))
            {
                var fileText = File.ReadAllText(file);
                if (fileText == string.Empty)
                    continue;

                var product = Path.GetFileName(file).Substring(0, Path.GetFileName(file).IndexOf('-'));
                var versionInfo = ParseVersions(fileText, product);
                if (versionInfo == null)
                    continue;

                SequenceStore.Add(product, versionInfo.SequenceNumber);
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

                if (!SequenceStore.ContainsKey(summaryEntry.Key.Product))
                    SequenceStore.Add(summaryEntry.Key.Product, summaryEntry.Value);
                else
                {
                    // There's a higher Sequence Number since shutdown.
                    if (summaryEntry.Value > SequenceStore[summaryEntry.Key.Product])
                    {
                        SequenceStore[summaryEntry.Key.Product] = summaryEntry.Value;

                        Task.Run(async () =>
                        {
                            await HandleNewBuild(versionInfo, "cache/temp");
                        });
                    }
                }

                if (!RibbitProducts.Contains(summaryEntry.Key.Product))
                {
                    if (!init)
                        DiscordManager.SendDebugMessage($"Found new endpoint: **{summaryEntry.Key.Product}**");

                    RibbitProducts.Add(summaryEntry.Key.Product);
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
                Console.WriteLine($"Something went wrong while parsing {product}");
                return null;
            }

            versions.Product = product;

            if (!CurrentVersions.ContainsKey(product))
                CurrentVersions.Add(product, versions.BuildId);

            return versions;
        }

        /// <summary>
        /// Handle the new <see cref="Versions"/>
        /// </summary>
        /// <param name="version"></param>
        public static async Task HandleNewBuild(Versions version, string file)
        {
            var oldBuildId = CurrentVersions[version.Product];
            if (oldBuildId == version.BuildId)
                return;

            Versions oldVersion = null;
            if (File.Exists($"{version.Product}_{oldBuildId}"))
            {
                oldVersion = ParseVersions(File.ReadAllText($"{version.Product}_{oldBuildId}"), version.Product);
                File.Delete($"{version.Product}_{oldBuildId}");
            }

            if (oldVersion == null)
                return;

            File.WriteAllText($"{version.Product}_{version.BuildId}", File.ReadAllText(file));
            DiscordManager.SendBuildMonitorMessage(version.Product, oldVersion, version);

            CurrentVersions[version.Product] = version.BuildId;

            if (version.Product.IsEncrypted())
                return;

            Console.WriteLine($"Getting old BuildConfig '{oldVersion.BuildConfig}'");
            var oldRootStream = await HTTP.RequestCDN($"tpr/wow/config/{oldVersion.BuildConfig.Substring(0, 2)}/{oldVersion.BuildConfig.Substring(2, 2)}/{oldVersion.BuildConfig}");
            if (oldRootStream == null)
                return;

            Console.WriteLine($"Getting new BuildConfig '{version.BuildConfig}'");
            var newRootStream = await HTTP.RequestCDN($"tpr/wow/config/{version.BuildConfig.Substring(0, 2)}/{version.BuildConfig.Substring(2, 2)}/{version.BuildConfig}");
            if (newRootStream == null)
                return;

            var oldRoot = await Encoding.RetrieveRootHash(oldRootStream);
            var newRoot = await Encoding.RetrieveRootHash(newRootStream);

            var addedFiles = await Root.DiffRoot(oldRoot.Encoding.ToHexString(), newRoot.Encoding.ToHexString());
            if (addedFiles.Count > 1)
                FilenameGuesser.ProcessFiles(version.Product, addedFiles, oldVersion.BuildConfig, oldVersion.CDNConfig, version.BuildId);
        }
    }
}
