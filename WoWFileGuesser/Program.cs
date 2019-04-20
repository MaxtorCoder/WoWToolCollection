using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WoWFileGuesser
{
    class Program
    {
        static void Main(string[] args)
        {
            string CollectionFile = "FileTypes.csv";
            string exportedPath = @"D:\WoW PS\CASC\work\unknown\";

            if (!Directory.Exists(exportedPath))
                Console.WriteLine($"Directory '{args[0]}' does not exist.");

            if (!File.Exists(CollectionFile))
                Console.WriteLine($"'{CollectionFile}' does not exist.");
            else
                FileGuesser.LoadFileTypes(CollectionFile);

            string[] m2Files = Directory.GetFiles(exportedPath, "*.m2");
            foreach (string m2File in m2Files)
            {
                FileGuesser.ProcessFile(m2File);
            }

            FileGuesser.AddListfileEntry("listfile.csv");
            Console.ReadLine();
        }
    }
}
