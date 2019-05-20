using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;

namespace RibbitMonitor.MySql
{
    public class MySqlHandler
    {
        private const string ConnString = "SERVER=localhost;UID=root;PASSWORD=;DATABASE=maxtorcoder.me;";

        public void AddDatabaseEntry(Versions version, string file)
        {
            var split = file.Split('.');
            string endpointString = string.Empty;

            using (var conn = new MySqlConnection(ConnString))
            {
                conn.Open();

                if (split[1].StartsWith("agent") || split[1].StartsWith("bna") ||
                    split[1].StartsWith("bts") || split[1].StartsWith("catalogs"))
                    endpointString = "agent";
                else if (split[1].StartsWith("d3"))
                    endpointString = "diablo";
                else if (split[1].StartsWith("dst"))
                    endpointString = "destiny";
                else if (split[1].StartsWith("hero"))
                    endpointString = "hots";
                else if (split[1].StartsWith("hs"))
                    endpointString = "hs";
                else if (split[1].StartsWith("pro"))
                    endpointString = "ow";
                else if (split[1].StartsWith("s1"))
                    endpointString = "sc1";
                else if (split[1].StartsWith("s2"))
                    endpointString = "sc2";
                else if (split[1].StartsWith("w3"))
                    endpointString = "wc3";
                else if (split[1].StartsWith("wow"))
                    endpointString = "wow";
                else
                    Console.WriteLine($"Unknown endpoint detected. '{split[1]}'");

                if (SelectEntry(conn, version, endpointString) == true)
                    UpdateEntry(conn, version, split[1], endpointString);
                else
                    InsertEntry(conn, version, split[1], endpointString);
            }
        }

        public bool SelectEntry(MySqlConnection conn, Versions version,  string DatabaseName)
        {
            using (var selectComm = new MySqlCommand($"SELECT * FROM `{DatabaseName}_builds` where `buildconfig` = @BuildConfig", conn))
            {
                selectComm.Parameters.AddWithValue("@BuildConfig", version.BuildConfig);

                var reader = selectComm.ExecuteReader();
                if (reader.HasRows)
                    return true;
                else
                    return false;
            }

        }

        public void InsertEntry(MySqlConnection conn, Versions version, string endpoint, string DatabaseName)
        {
            using (var insertComm = new MySqlCommand($"INSERT INTO `{DatabaseName}_builds` (`branch`, `buildconfig`, `cdnconfig`, `buildid`, `versionsname`, `productconfig`) VALUES " +
                $"(@branch, @buildConfig, @CDNConfig, @buildid, @versionsName, @productConfig)", conn))
            {
                insertComm.Parameters.AddWithValue("@branch", MySqlMisc.GetBranch(endpoint));
                insertComm.Parameters.AddWithValue("@buildconfig", version.BuildConfig);
                insertComm.Parameters.AddWithValue("@CDNConfig", version.CDNConfig);
                insertComm.Parameters.AddWithValue("@buildid", version.BuildId);
                insertComm.Parameters.AddWithValue("@versionsName", version.VersionsName);
                insertComm.Parameters.AddWithValue("@productConfig", version.ProductConfig);
                insertComm.ExecuteNonQuery();

                Console.WriteLine($"{endpoint} : {version.VersionsName} has been added to the Database.");
            }
        }

        public void UpdateEntry(MySqlConnection conn, Versions version, string endpoint, string DatabaseName)
        {
            try
            {
                using (var updateComm = new MySqlCommand($"UPDATE `{DatabaseName}_builds` SET `branch` = @Branch, `buildconfig` = @BuildConfig, `cdnconfig` = @CdnConfig, " +
                    $"`buildid` = @BuildId, `versionsname` = @Version, `productconfig` = @ProductConfig " +
                    $"WHERE `buildconfig` = @BuildConfig", conn))
                {
                    updateComm.Parameters.AddWithValue("@Branch", MySqlMisc.GetBranch(endpoint));
                    updateComm.Parameters.AddWithValue("@BuildConfig", version.BuildConfig);
                    updateComm.Parameters.AddWithValue("@CdnConfig", version.CDNConfig);
                    updateComm.Parameters.AddWithValue("@BuildId", version.BuildId);
                    updateComm.Parameters.AddWithValue("@Version", version.VersionsName);
                    updateComm.Parameters.AddWithValue("@ProductConfig", version.ProductConfig);
                    updateComm.ExecuteNonQuery();

                    Console.WriteLine($"{endpoint} : {version.VersionsName} has been updated.");
                }
            }
            catch (Exception ex) { Console.WriteLine(ex); };
        }
    }
}
