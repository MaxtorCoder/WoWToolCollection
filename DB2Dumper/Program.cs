using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

namespace DB2Dumper
{
    class Program
    {
        static string DBStores          = @"C:\Users\MaxtorCoder\source\repos\Firestorm-Source\src\server\game\DataStores\DB2Stores.cpp";
        static string DB2Path           = @"D:\WoW\Servers\Firestorm-Server\Data\dbc";
        static string DBDPath           = @"C:\Users\MaxtorCoder\source\repos\WoWDBDefs\definitions";
        static List<string> DB2Files    = new List<string>();
        static List<string> DB2Files2   = new List<string>();

        static void Main(string[] args)
        {
            using (var reader = new StreamReader(DBStores))
            {
                for (var i = 0; i < 3168; i++)
                    reader.ReadLine();

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line.StartsWith("    LoadDB2(l_BadDB2Files, "))
                    {
                        var seperator = new char[] { ' ', ',', '"' };
                        var array = line.Split(seperator, StringSplitOptions.RemoveEmptyEntries);

                        var db2File = array[3];
                        var dbdFile = array[3].Replace("db2", "dbd");

                        DB2Files2.Add(db2File);

                        if (!File.Exists($"{DB2Path}\\{db2File}"))
                            Console.WriteLine($"{db2File} does not exist in the data folder.");

                        GetDBDStructure(dbdFile, db2File);
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine();

            var newResult = DB2Files2.Where(k => !DB2Files.Contains(k)).ToList();
            foreach (var result in newResult)
            {
                Console.WriteLine($"{result} structure changed...");
            }
        }

        static void GetDBDStructure(string dbdFile, string db2File)
        {
            // Console.WriteLine($"Getting DBD structure -> {dbdFile}");

            var lineList    = new List<string>();
            var lineNumber  = 1;
            var counter     = 0;
            var seperator   = new char[] { ' ', ','};

            using (var reader = new StreamReader($"{DBDPath}\\{dbdFile}"))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    lineList.Add(line);

                    lineNumber++;

                    if (line.Contains("8.3.0.33062"))
                    {
                        var layout      = lineList[lineNumber - 3];
                        var array       = layout.Split(seperator, StringSplitOptions.RemoveEmptyEntries);
                        var hashing     = DB2Reader.ReadDB2($"{DB2Path}\\{db2File}").LayoutHash.ToString("X02");

                        foreach (var hash in array)
                        {
                            if (hash == hashing)
                            {
                                DB2Files.Add(db2File);
                                Console.WriteLine($"{db2File} is fine...");
                                counter++;
                            }
                        }
                    }
                }

                lineNumber = 0;
            }
            
        }
    }

    class EntityComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {
            return x == y;
        }

        public int GetHashCode(string obj)
        {
            return 0;
        }
    }
}
