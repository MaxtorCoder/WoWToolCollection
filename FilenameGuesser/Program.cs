using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using DBFileReaderLib;
using FilenameGuesser.DB2Structures;
using FilenameGuesser.Readers;
using FilenameGuesser.Util;

namespace FilenameGuesser
{
    class Program
    {
        public static ConcurrentDictionary<uint, string> AddedFileDataIds = new ConcurrentDictionary<uint, string>();
        public static ConcurrentDictionary<uint, string> FDIDFilename = new ConcurrentDictionary<uint, string>();
        private static Dictionary<uint, string> Listfile = new Dictionary<uint, string>();

        private static uint currentFileDataId = 0;

        static void Main(string[] args)
        {
            // Read the original listfile.
            ReadOriginalListfile();

            // Load new casc
            Console.WriteLine("Loading casc...");
            CASC.LoadCASC();

            Console.WriteLine($"Going from FileDataId 3081147 to 3723033");
            for (var fileDataId = 3081147u; fileDataId <= 3723033u; ++fileDataId)
            {
                currentFileDataId = fileDataId;

                try
                {
                    var stream = CASC.OpenFile(fileDataId);
                    if (stream == null)
                        continue;

                    if (stream.IsModel())
                    {
                        var m2Model = new M2Reader();
                        m2Model.Process(stream);

                        var path = Names.GetPathFromName(m2Model.GetName());
                        AddToListfile(fileDataId, $"{path}/{m2Model.GetName()}.m2");
                        m2Model.NameAllFiles();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            Console.WriteLine($"Writing listfile, {AddedFileDataIds.Count} new entries");
            GenerateListfile();
        }

        public static void GenerateListfile()
        {
            using (var writer = new StreamWriter("listfile_export.csv"))
            {
                var keys = AddedFileDataIds.Keys.ToList();
                keys.Sort();

                foreach (var entry in keys)
                    writer.WriteLine($"{entry};{AddedFileDataIds[entry]}");

                writer.Close();
            }
        }

        public static void AddToListfile(uint fileDataId, string filename)
        {
            if (!Listfile.ContainsKey(fileDataId))
                AddedFileDataIds.TryAdd(fileDataId, filename);
        }

        public static void ReadOriginalListfile()
        {
            using (var reader = new StreamReader("listfile.csv"))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var lineSplit = line.Split(';');

                    Listfile.Add(uint.Parse(lineSplit[0]), lineSplit[1]);
                }
            }
        }
    }

    public enum Chunk
    {
        // WDT Chunks
        MAID = 1296124228,
    }
}