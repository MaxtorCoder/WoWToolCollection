using Ribbit.Constants;
using Ribbit.Protocol;
using Ribbit.Parsing;

using System;
using System.Collections.Generic;

namespace RibbitMonitor
{
    class RibbitMonitor
    {
        private static Dictionary<(string, string), string> CachedFiles = new Dictionary<(string, string), string>();
        private static uint oldSeq, newSeq = 0;

        static void Main()
        {
            var client = new Client(Region.US);
            var request = client.Request("v1/summary");

            var summary = ParseSummary(request.ToString());
            Console.WriteLine(request.ToString());
        }

        // static void Main()
        // {
        //     MySqlMisc.LoadBranches("Branches.csv");
        // 
        //     if (!Directory.Exists("cache"))
        //         Directory.CreateDirectory("cache");
        // 
        //     Console.WriteLine("-----------------------------------");
        //     Console.WriteLine(" Grabbing updates since last run.. ");
        //     Console.WriteLine("-----------------------------------");
        // 
        //     var client = new Client(Region.US);
        //     var req = client.Request("v1/summary");
        // 
        //     var currentSummary = ParseSummary(req.ToString());
        //     foreach (var entry in currentSummary)
        //     {
        //         if (entry.Value == 0)
        //             continue;
        // 
        //         try
        //         {
        //             var endpoint = "";
        // 
        //             if (entry.Key.Item2 == "version"
        //              || entry.Key.Item2 == "cdn")
        //                 endpoint = entry.Key.Item2 + "s";
        //             else
        //                 endpoint = entry.Key.Item2;
        // 
        //             var oldFile = $"{entry.Key.Item2}.{entry.Key.Item1}.bmime";
        //             var newFile = $"{entry.Key.Item2}.{entry.Key.Item1}_new.bmime";
        // 
        //             var subRequest = client.Request($"v1/products/{entry.Key.Item1}/{endpoint}");
        // 
        //             File.WriteAllText(Path.Combine("cache", newFile), subRequest.message.ToString());
        // 
        //             if (File.Exists(Path.Combine("cache", oldFile)))
        //                 oldSeq = CacheParse.GetSeqNumber(Path.Combine("cache", oldFile));
        // 
        //             if (File.Exists(Path.Combine("cache", newFile)))
        //                 newSeq = CacheParse.GetSeqNumber(Path.Combine("cache", newFile));
        // 
        //             if (oldSeq < newSeq)
        //             {
        //                 Console.WriteLine($"Higher Sequence Number! [{oldSeq} > {newSeq}][{oldFile}]");
        // 
        //                 File.Delete(Path.Combine("cache", oldFile));
        //                 File.Move(Path.Combine("cache", newFile), Path.Combine("cache", oldFile));
        //             }
        //             else
        //                 File.Delete(Path.Combine("cache", newFile));
        // 
        //             CachedFiles[entry.Key] = subRequest.ToString();
        //         }
        //         catch (FormatException e) { Console.WriteLine($"{entry.Key} is forked. Error: {e.Message}"); }
        //     }
        //     CacheParse.ParseCacheFiles();
        //     Console.WriteLine("Starting Monitoring Mode..");
        // 
        //     while (true)
        //     {
        //         var newSummaryString = client.Request("v1/summary").ToString();
        //         var newSummary = new Dictionary<(string, string), int>();
        // 
        //         try
        //         {
        //             newSummary = ParseSummary(newSummaryString);
        //         }
        //         catch (Exception ex)
        //         {
        //             Console.WriteLine($"Parsing Summary Failed! {ex.Message}");
        //             continue;
        //         }
        // 
        //         foreach (var newEntry in newSummary)
        //         {
        //             if (currentSummary.ContainsKey(newEntry.Key))
        //             {
        //                 if (newEntry.Value > currentSummary[newEntry.Key])
        //                 {
        //                     Console.WriteLine($"[{DateTime.Now}] Sequence number for {newEntry.Key} increased from {currentSummary[newEntry.Key]} -> {newEntry.Value}");
        // 
        //                     try
        //                     {
        //                         var endpoint = "";
        // 
        //                         if (newEntry.Key.Item2 == "version"
        //                          || newEntry.Key.Item2 == "cdn")
        //                             endpoint = newEntry.Key.Item2 + "s";
        //                         else
        //                             endpoint = newEntry.Key.Item2;
        // 
        //                         var oldFile = $"{newEntry.Key.Item2}.{newEntry.Key.Item1}.bmime";
        //                         var newFile = $"{newEntry.Key.Item2}.{newEntry.Key.Item1}_new.bmime";
        // 
        //                         var subRequest = client.Request($"v1/products/{newEntry.Key.Item1}/{endpoint}");
        // 
        //                         File.Delete(Path.Combine("cache", oldFile));
        //                         File.WriteAllText(Path.Combine("cache", oldFile), subRequest.message.ToString());
        // 
        //                         CacheParse.ParseCacheFiles();
        //                     }
        //                     catch (Exception e) { Console.WriteLine($"Error during diff: {e.Message}"); }
        // 
        //                     currentSummary[newEntry.Key] = newEntry.Value;
        //                 }
        //             }
        //             else
        //             {
        //                 Console.WriteLine($"[{DateTime.Now}] New endpoint found: {newEntry.Key}");
        //                 currentSummary[newEntry.Key] = newEntry.Value;
        //             }
        //         }
        // 
        //         Thread.Sleep(90000);
        //     }
        // }

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
    }
}
