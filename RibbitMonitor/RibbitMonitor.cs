using Ribbit.Constants;
using Ribbit.Protocol;
using Ribbit.Parsing;
using RibbitMonitor.Discord;

using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace RibbitMonitor
{
    class RibbitMonitor
    {
        private static Dictionary<(string, string), string> CachedFiles = new Dictionary<(string, string), string>();
        private static List<string> WoWProducts = new List<string>()
        {
            "wow", "wowt", "wowz", "wowv", "wow_classic", "wow_classic_beta", "wow_beta"
        };


        static void Main(string[] args)
        {
            if (!Directory.Exists("cache"))
                Directory.CreateDirectory("cache");

            Console.WriteLine("-----------------------------------");
            Console.WriteLine(" Grabbing updates since last run.. ");
            Console.WriteLine("-----------------------------------");

            var client = new Client(Region.US);
            var req = client.Request("v1/summary");

            var currentSummary = ParseSummary(req.ToString());
            foreach (var entry in currentSummary)
            {
                if (entry.Value == 0)
                {
                    Console.WriteLine($"Sequence number for {entry.Key} is 0, skipping..");
                    continue;
                }

                var endpoint = "";

                if (WoWProducts.Contains(entry.Key.Item1))
                {
                    if (entry.Key.Item2 == "version" || entry.Key.Item2 == "cdn")
                        endpoint = entry.Key.Item2 + "s";
                    else if (entry.Key.Item2 == "bgdl")
                        endpoint = entry.Key.Item2;

                    try
                    {
                        var filename = $"{entry.Key.Item2}_{entry.Key.Item1}_{entry.Value}.bmime";

                        Response subRequest;

                        if (File.Exists(Path.Combine("cache", filename)))
                            subRequest = new Response(new MemoryStream(File.ReadAllBytes(Path.Combine("cache", filename))));
                        else
                        {
                            Console.WriteLine($"Product: {entry.Key.Item1}\nEndpoint: {endpoint}\nFilename: {filename}\n-----------------------------------");

                            subRequest = client.Request($"v1/products/{entry.Key.Item1}/{endpoint}");

                            File.WriteAllText(Path.Combine("cache", filename), subRequest.message.ToString());

                            Thread.Sleep(100);
                        }

                        CachedFiles[entry.Key] = subRequest.ToString();
                    }
                    catch (FormatException e)
                    {
                        Console.WriteLine($"{entry.Key} is forked");
                    }
                }
            }
            CacheParse.ParseCacheFiles();
            CacheParse.VersionDictionary.Remove(CacheParse.VersionDictionary.Keys.First());

            Console.WriteLine("Starting Monitoring Mode..");

            while (true)
            {
                var newSummaryString = client.Request("v1/summary").ToString();
                var newSummary = new Dictionary<(string, string), int>();

                try
                {
                    newSummary = ParseSummary(newSummaryString);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Parsing Summary Failed! {ex.Message}");
                    continue;
                }

                foreach (var newEntry in newSummary)
                {
                    if (currentSummary.ContainsKey(newEntry.Key))
                    {
                        if (WoWProducts.Contains(newEntry.Key.Item1))
                        {
                            if (newEntry.Value > currentSummary[newEntry.Key])
                            {
                                Console.WriteLine($"[{DateTime.Now}] Sequence number for {newEntry.Key} increased from {currentSummary[newEntry.Key]} to {newEntry.Value}");

                                var endpoint = "";

                                if (newEntry.Key.Item2 == "version" || newEntry.Key.Item2 == "cdn")
                                    endpoint = newEntry.Key.Item2 + "s";
                                else if (newEntry.Key.Item2 == "bgdl")
                                    endpoint = newEntry.Key.Item2;

                                try
                                {
                                    var subRequest = client.Request($"v1/products/{newEntry.Key.Item1}/{endpoint}");
                                    var filename = $"{newEntry.Key.Item2}_{newEntry.Key.Item1}_{newEntry.Value}_temp.bmime";
                                    File.WriteAllText(Path.Combine("cache", filename), subRequest.message.ToString());

                                    CacheParse.ParseCacheFiles();
                                    CacheParse.VersionDictionary.Remove(CacheParse.VersionDictionary.Keys.First());
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine($"Error during diff: {e.Message}");
                                }

                                currentSummary[newEntry.Key] = newEntry.Value;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[{DateTime.Now}] New endpoint found: {newEntry.Key}");
                        currentSummary[newEntry.Key] = newEntry.Value;
                    }
                }

                Thread.Sleep(3000);
            }
        }


        private static Dictionary<(string, string), int> ParseSummary(string summary)
        {
            var summaryDictionary = new Dictionary<(string, string), int>();
            var parsedFile = new BPSV(summary);

            foreach (var entry in parsedFile.data)
            {
                if (string.IsNullOrEmpty(entry[2]))
                {
                    summaryDictionary.Add((entry[0], "version"), int.Parse(entry[1]));
                }
                else
                {
                    summaryDictionary.Add((entry[0], entry[2].Trim()), int.Parse(entry[1]));
                }
            }
            return summaryDictionary;
        }

        private static string DiffFile(string oldContent, string newContent)
        {
            var oldFile = new BPSV(oldContent);
            var newFile = new BPSV(newContent);

            foreach (var oldEntry in oldFile.data)
            {
                var regionMatch = false;

                foreach (var newEntry in newFile.data)
                {
                    // Region matches
                    if (oldEntry[0] == newEntry[0])
                    {
                        regionMatch = true;
                        // diff each field
                    }
                }

                if (regionMatch == false)
                {
                    // new region
                }
            }

            return "";
        }
    }
}
