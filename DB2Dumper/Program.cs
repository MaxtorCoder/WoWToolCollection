using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DB2Dumper
{
    class Program
    {
        public static Dictionary<string, string> DBFileEntries = new Dictionary<string, string>();
        static void Main(string[] args)
        {
            PopulateDict();
            string path = @"D:\Private Servers\World of Warcraft\CASC\work\dbfilesclient";
            string[] files = Directory.GetFiles(path);

            using (var writer = new StreamWriter("dumped_dbfilesclient.txt"))
            {
                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file).Replace("_", "");
                    string entry = DBFileEntries[fileName];

                    writer.WriteLine($"public static DB2Storage<{entry}Record> {entry}Storage;");
                }
            }
        }

        public static void PopulateDict()
        {
            using (var reader = new StreamReader("DBFilesClient.csv"))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var split = line.Split(';');

                    DBFileEntries.Add(split[0], split[1]);
                }
            }
        }
    }
}
