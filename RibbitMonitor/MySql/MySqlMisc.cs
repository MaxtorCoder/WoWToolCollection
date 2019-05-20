using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RibbitMonitor.MySql
{
    public class MySqlMisc
    {
        public static Dictionary<string, string> ContainDict = new Dictionary<string, string>();
        public static void LoadBranches(string path)
        {
            using (StreamReader sr = new StreamReader(path))
            {
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    string[] split = line.Split(';');

                    ContainDict.Add(split[0], split[1]);
                }
            }
        }

        public static string GetBranch(string endpoint)
        {
            if (ContainDict.ContainsKey(endpoint))
            {
                return ContainDict[endpoint];
            }
            else
                return "Unknown";
        }
    }
}
