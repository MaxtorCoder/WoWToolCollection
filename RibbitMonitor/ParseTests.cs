using Ribbit.Constants;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RibbitMonitor
{
    class CacheParse
    {
        public static Dictionary<List<Versions>, string> VersionDictionary = new Dictionary<List<Versions>, string>();

        public static void ParseCacheFiles(bool parse = false)
        {
            if (Directory.Exists("cache"))
            {
                var files = Directory.EnumerateFiles("cache");
                var allFiles = new List<string>();
                allFiles.AddRange(files);

                var filesThatMatter = allFiles.FindAll(x => x.Contains("version")); // "version_" files.
                filesThatMatter.RemoveAt(0); // demo
                filesThatMatter.RemoveAt(0); // dev

                foreach (var file in filesThatMatter)
                {
                    var versionsList = new List<Versions>();
                    // Console.WriteLine($"Parsing {file}...");

                    using (var sr = File.OpenText(file))
                    {
                        var line = string.Empty;
                        while ((line = sr.ReadLine()) != null)
                        {
                            try
                            {
                                var version = HandleLine(line);
                                versionsList.Add(version);
                            }
                            catch { }
                        }
                        VersionDictionary.Add(versionsList, file);
                    }
                }
            }
        }

        private static Versions HandleLine(string line)
        {
            if (!line.StartsWith("eu") && !line.StartsWith("us") && !line.StartsWith("kr")
                && !line.StartsWith("cn") && !line.StartsWith("tw") && !line.StartsWith("sg")
                && !line.StartsWith("xx"))
                return new Versions();

            var versionStruct = new Versions();
            var lineSplit = line.Split("|");

            versionStruct.Region        = lineSplit[0];
            versionStruct.BuildConfig   = lineSplit[1];
            versionStruct.CDNConfig     = lineSplit[2];

            var keyRing = lineSplit[3];
            if (keyRing.Length == 0)
                versionStruct.KeyRing = string.Empty;
            else
                versionStruct.KeyRing = keyRing;

            versionStruct.BuildId       = uint.Parse(lineSplit[4]);
            versionStruct.VersionsName  = lineSplit[5];
            versionStruct.ProductConfig = lineSplit[6];

            return versionStruct;
        }
    }
}
