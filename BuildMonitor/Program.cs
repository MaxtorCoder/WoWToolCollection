using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Web;

namespace BuildMonitor
{
    class Program
    {
        private static string[] products = { "wow", "wowt", "wow_beta", "wowv", "wowdev", "wow_classic", "wow_classic_ptr", "wow_classic_beta" };
        private static string tacturl = "http://us.patch.battle.net:1119";

        private static Dictionary<string, uint> BranchVersions = new Dictionary<string, uint>();
        private static Dictionary<uint, VersionsInfo> BranchVersionInfo = new Dictionary<uint, VersionsInfo>(); 
        private static bool isMonitoring = true;

        static void Main(string[] args)
        {
            Directory.CreateDirectory("cache");

            foreach (var product in products)
            {
                var request = WebRequest.Create($"{tacturl}/{product}/versions");
                var response = request.GetResponse().GetResponseStream();

                ParseVersions(product, response);
            }

            Console.WriteLine("Monitoring the patch servers...");
            while (isMonitoring)
            {
                Thread.Sleep(10000);

                foreach (var product in products)
                {
                    var request = WebRequest.Create($"{tacturl}/{product}/versions");
                    var response = request.GetResponse().GetResponseStream();

                    ParseVersions(product, response);
                }
            }
        }

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
                }

                if (!File.Exists($"cache/{product}_{versions.BuildId}.versions"))
                    File.WriteAllBytes($"cache/{product}_{versions.BuildId}.versions", memStream.ToArray());
            }
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
