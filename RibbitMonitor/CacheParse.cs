using Ribbit.Constants;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;

namespace RibbitMonitor
{
    class CacheParse
    {
        public static Dictionary<string, List<Versions>> VersionDictionary = new Dictionary<string, List<Versions>>();
        public static List<Versions> versionsList = new List<Versions>();
        private static readonly string ConnString = "SERVER=localhost;UID=root;PASSWORD=;DATABASE=;";

        public static void ParseCacheFiles(bool addDatabase)
        {
            if (Directory.Exists("cache"))
            {
                var files = Directory.EnumerateFiles("cache");
                var allFiles = new List<string>();
                allFiles.AddRange(files);

                var filesThatMatter = allFiles.FindAll(x => x.Contains("version")); // "version_" files.

                if (addDatabase)
                    HandleDBLine(filesThatMatter);
                else
                    HandleLines(filesThatMatter);
            }
        }

        private static void HandleLines(List<string> filesThatMatter)
        {
            foreach (var file in filesThatMatter)
            {
                versionsList = new List<Versions>();

                using (var sr = File.OpenText(file))
                {
                    var line = string.Empty;
                    while ((line = sr.ReadLine()) != null)
                    {
                        try
                        {
                            var version = HandleLine(line);
                            if (version != null)
                                versionsList.Add(version);
                        }
                        catch { }
                    }
                    VersionDictionary.Add(file, versionsList);
                }
            }
        }
        private static void HandleDBLine(List<string> filesThatMatter)
        {
            foreach (var file in filesThatMatter)
            {
                versionsList = new List<Versions>();

                using (var sr = File.OpenText(file))
                {
                    for (int i = 0; i < 10; i++)
                    {
                        sr.ReadLine();
                    }
                    var line = sr.ReadLine();
                    try
                    {
                        var version = HandleLine(line);
                        if (version != null)
                        {
                            versionsList.Add(version);
                            AddDatabaseEntry(version, file);
                        }
                    }
                    catch { }
                    VersionDictionary.Add(file, versionsList);
                }
            }
        }

        private static Versions HandleLine(string line)
        {
            if (!line.StartsWith("eu") && !line.StartsWith("us") && !line.StartsWith("kr")
                && !line.StartsWith("cn") && !line.StartsWith("tw") && !line.StartsWith("sg")
                && !line.StartsWith("xx"))
                return null;

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
            versionStruct.VersionsName  = lineSplit[5].Remove(lineSplit[5].Length - 6, 6);
            versionStruct.ProductConfig = lineSplit[6];

            return versionStruct;
        }

        private static void AddDatabaseEntry(Versions version, string file)
        {
            var query       = "INSERT INTO `wow_builds` (`branch`, `buildconfig`, `cdnconfig`, `buildid`, `versionsname`, `productconfig`, `publish_time`) " +
                "VALUES (@branch, @buildConfig, @CDNConfig, @buildid, @versionsName, @productConfig, @publishTime)";
            var selectQuery = "SELECT * FROM `wow_builds` where `productconfig` = @productConfig";
            bool RowExists  = false;

            try
            {
                using (var conn = new MySqlConnection(ConnString))
                {
                    using (var command = new MySqlCommand(selectQuery, conn))
                    {
                        conn.Open();

                        command.Parameters.AddWithValue("@productConfig", version.ProductConfig);

                        var reader = command.ExecuteReader();
                        if (!reader.HasRows)
                            RowExists = false;
                        else
                        {
                            RowExists = true;
                            Console.WriteLine($"Product Config '{version.ProductConfig}' already exists in the database.");
                        }
                    }

                    if (!RowExists)
                    {
                        using (var command2 = new MySqlCommand(query, conn))
                        {
                            command2.Parameters.AddWithValue("@branch", GetBranch(file));
                            command2.Parameters.AddWithValue("@buildconfig", version.BuildConfig);
                            command2.Parameters.AddWithValue("@CDNConfig", version.CDNConfig);
                            command2.Parameters.AddWithValue("@buildid", version.BuildId);
                            command2.Parameters.AddWithValue("@versionsName", version.VersionsName);
                            command2.Parameters.AddWithValue("@productConfig", version.ProductConfig);
                            command2.Parameters.AddWithValue("@publishTime", DateTime.Now);
                            command2.ExecuteNonQuery();
                    
                            Console.WriteLine($"{GetBranch(file)} {version.VersionsName} has been added to the Database.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error has occured: {ex.Message}");
            }
        }

        private static string GetBranch(string file)
        {
            if (file.Contains("wow_"))
            {
                if (file.Contains("wow_beta"))
                    return "Beta";
                else if (file.Contains("wow_classic"))
                {
                    if (file.Contains("beta"))
                        return "Classic Beta";
                    else
                        return "Classic";
                }
                else
                    return "Retail";
            }
            else if (file.Contains("wowt"))
                return "PTR";
            else if (file.Contains("wowz"))
                return "Submission";
            else if (file.Contains("wowv"))
                return "Vendor";
            else if (file.Contains("wowe1"))
                return "Event 1";
            else if (file.Contains("wowe2"))
                return "Event 2";
            else if (file.Contains("wowe3"))
                return "Event 3";
            else
                return "UNKNOWN";
        }
    }
}
