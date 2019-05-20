using Ribbit.Constants;
using RibbitMonitor.MySql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


namespace RibbitMonitor
{
    class CacheParse
    {
        private static MySqlHandler MySqlHandler = new MySqlHandler();
        public static Dictionary<string, List<Versions>> VersionDictionary = new Dictionary<string, List<Versions>>();
        public static List<Versions> versionsList = new List<Versions>();

        public static void ParseCacheFiles()
        {
            if (Directory.Exists("cache"))
            {
                var files       = Directory.EnumerateFiles("cache");
                var allFiles    = new List<string>();
                allFiles.AddRange(files);

                var filesThatMatter = allFiles.FindAll(x => x.Contains("version")); // "version_" files.

                foreach (var file in filesThatMatter)
                    HandleDatabaseEntry(file);
            }
        }
        
        public static void HandleDatabaseEntry(string file)
        {
            versionsList = new List<Versions>();

            using (var sr = File.OpenText(file))
            {
                for (int i = 0; i < 10; i++)
                    sr.ReadLine();

                var line = sr.ReadLine();
                try
                {
                    var version = HandleLine(line, file);
                    if (version != null)
                    {
                        if (!versionsList.Contains(version))
                            versionsList.Add(version);

                        MySqlHandler.AddDatabaseEntry(version, file);
                    }
                }
                catch { }
                VersionDictionary.Add(file, versionsList);
            }
        }

        public static Versions HandleLine(string line, string file)
        {
            if (!line.StartsWith("eu") && !line.StartsWith("us") && !line.StartsWith("kr")
                && !line.StartsWith("cn") && !line.StartsWith("tw") && !line.StartsWith("sg")
                && !line.StartsWith("xx"))
                return null;

            var versionStruct           = new Versions();
            var lineSplit               = line.Split("|");

            versionStruct.Region        = lineSplit[0];
            versionStruct.BuildConfig   = lineSplit[1];
            versionStruct.CDNConfig     = lineSplit[2];

            var keyRing                 = lineSplit[3];
            if (keyRing.Length == 0)
                versionStruct.KeyRing   = string.Empty;
            else
                versionStruct.KeyRing   = keyRing;

            versionStruct.BuildId       = uint.Parse(lineSplit[4]);

            var VersionSplit            = lineSplit[5].Split('.', ' ');
            if (file.Contains("dst"))
                versionStruct.VersionsName = $"{VersionSplit[0]}.{VersionSplit[1]}.{VersionSplit[2]}.{VersionSplit[3]}";
            else if (file.Contains("catalogs"))
                versionStruct.VersionsName = $"{VersionSplit[0]}";
            else
                versionStruct.VersionsName = $"{VersionSplit[0]}.{VersionSplit[1]}.{VersionSplit[2]}";

            versionStruct.ProductConfig = lineSplit[6];

            return versionStruct;
        }

        public static uint GetSeqNumber(string file)
        {
            using (var sr = File.OpenText(file))
            {
                for (int i = 0; i < 9; i++)
                    sr.ReadLine();

                string line = sr.ReadLine();
                string[] split = line.Split(' ', '=');

                sr.Close();
                return uint.Parse(split[4]);
            }
        }
    }
}
