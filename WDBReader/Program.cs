using System;
using System.IO;
using System.Linq;
using System.Text;

namespace WDBReader
{
    class Program
    {
        private const string CreatureCache = "creaturecache.wdb";

        static void Main(string[] args)
        {
            using (var stream = File.OpenRead(CreatureCache))
            using (var reader = new WDBReader(stream))
            using (var writer = new StreamWriter("output.txt"))
            {
                var signature   = Encoding.UTF8.GetString(reader.ReadBytes(4).ToArray());
                var build       = reader.ReadUInt32();
                var locale      = Encoding.UTF8.GetString(reader.ReadBytes(4).Reverse().ToArray());
                var unk         = reader.ReadUInt64();
                var version     = reader.ReadUInt32();

                Console.WriteLine($"Signature: {signature} Locale: {locale} Build: {build}");

                // while (reader.BaseStream.Position != reader.BaseStream.Length)
                // {
                var entry   = reader.ReadUInt32();
                var hasData = reader.ReadBit();
                reader.ResetBitReading();
                Console.WriteLine($"Entry: {entry} HasData: {hasData}");

                if (hasData != 0)
                {
                    var titleLen        = reader.ReadBits<int>(11);
                    var titleAltLen     = reader.ReadBits<int>(11);
                    var cursorNameLen   = reader.ReadBits<int>(6);

                    var racialLeader    = reader.ReadBit();

                    Console.WriteLine($"TitleLen: {titleLen} TitleAltLen: {titleAltLen} RacialLeader: {racialLeader}");

                    var stringLens = new int[4][];
                    for (var i = 0; i < 4; i++)
                    {
                        stringLens[i] = new int[2];
                        stringLens[i][0] = reader.ReadBits<int>(11);
                        stringLens[i][1] = reader.ReadBits<int>(11);
                    }

                    var name = new string[4];
                    var nameAlt = new string[4];

                    for (var i = 0; i < 4; i++)
                    {
                        name[i] = "";
                        nameAlt[i] = "";

                        if (stringLens[i][0] > 1)
                            name[i] = reader.ReadCString();

                        if (stringLens[i][1] > 1)
                            nameAlt[i] = reader.ReadCString();

                        Console.WriteLine($"Name: {name[i]} NameAlt: {nameAlt[i]}");
                    }

                    var typeflags = reader.ReadUInt32();
                    var typeflags2 = reader.ReadUInt32();
                    var type = reader.ReadUInt32();
                    var family = reader.ReadUInt32();
                    var rank = reader.ReadUInt32();

                    var killCredit = new uint?[2];
                    for (var i = 0; i < 2; i++)
                        killCredit[i] = reader.ReadUInt32();

                    var modelcount = reader.ReadUInt32();
                }
                // }
            }
        }

        static void WriteOutput(uint Entry, string Name, uint DisplayId)
        {
            
        }
    }
}
