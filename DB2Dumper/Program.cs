using CsvHelper;
using DBCD;
using DBCD.Providers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;

namespace DB2Dumper
{
    public class RaceXDisplayID
    {
        public int RaceID { get; set; }
        public int MaleDisplayID { get; set; }
        public int FemaleDisplayID { get; set; }
    }

    public class ClassInformation
    {
        public int ClassID { get; set; }
        public ushort DefaultSpec { get; set; }
        public byte DisplayPower { get; set; }
    }

    public class CharacterLoadout
    {
        public byte RaceID { get; set; }
        public int ClassID { get; set; }
        public uint ItemID { get; set; }
    }

    public class Item
    {
        public uint ItemID { get; set; }
        public uint ItemDisplayInfoID { get; set; }
        public byte InventoryType { get; set; }
        public byte SubClassID { get; set; }
    }

    class Program
    {
        const string DB2Path = @"E:\WoW\Servers\Firestorm-Server-Shadowlands\Data\dbc\";

        static IDBCDStorage ChrClasses { get; set; }

        static IDBCDStorage CharacterLoadout { get; set; }
        static IDBCDStorage CharacterLoadoutItem { get; set; }

        static IDBCDStorage Item { get; set; }
        static IDBCDStorage ItemAppearance { get; set; }
        static IDBCDStorage ItemModifiedAppearance { get; set; }

        static void Main(string[] args)
        {
            var dbcd = new DBCD.DBCD(new DBCProvider(), new DBDProvider());

            Item                    = dbcd.Load($"{DB2Path}/Item.db2");
            ItemAppearance          = dbcd.Load($"{DB2Path}/ItemAppearance.db2");
            ItemModifiedAppearance  = dbcd.Load($"{DB2Path}/ItemModifiedAppearance.db2");

            var itemList = new List<Item>();
            foreach (var id in ItemModifiedAppearance.Keys)
            {
                var itemID = ItemModifiedAppearance.GetField<int>(id, "ItemID");
                if (itemID == 0 || !Item.ContainsKey(itemID))
                    continue;

                var item = new Item
                {
                    ItemID          = (uint)itemID,
                    SubClassID      = Item.GetField<byte>(itemID, "SubclassID"),
                    InventoryType   = Item.GetField<byte>(itemID, "InventoryType"),
                };

                var itemAppearanceID = ItemModifiedAppearance.GetField<int>(id, "ItemAppearanceID");
                if (ItemAppearance.ContainsKey(itemAppearanceID))
                {
                    var itemDisplayInfo = ItemAppearance.GetField<uint>(itemAppearanceID, "ItemDisplayInfoID");
                    item.ItemDisplayInfoID = itemDisplayInfo;
                }

                Console.WriteLine($"Added Item: {itemID} with ItemDisplay: {item.ItemDisplayInfoID} InventoryType: {item.InventoryType}");
                itemList.Add(item);
            }

            WriteRecords("item.csv", itemList);
        }

        static void WriteRecords<T>(string file, IEnumerable<T> container) where T : new()
        {
            using (var csv = new CsvWriter(new StreamWriter(file), CultureInfo.InvariantCulture))
                csv.WriteRecords(container);
        }

        static void Backup()
        {
            var dbcd = new DBCD.DBCD(new DBCProvider(), new DBDProvider());
            ChrClasses = dbcd.Load($"{DB2Path}/ChrClasses.db2");

            var classInfo = new List<ClassInformation>();
            foreach (var id in ChrClasses.Keys)
            {
                var defaultSpec = ChrClasses.GetField<ushort>(id, "DefaultSpec");
                var powerType = ChrClasses.GetField<byte>(id, "DisplayPower");

                classInfo.Add(new ClassInformation
                {
                    ClassID = id,
                    DefaultSpec = defaultSpec,
                    DisplayPower = powerType,
                });
            }

            WriteRecords("ClassInformation.csv", classInfo);

            var raceDict = new Dictionary<uint, byte>
            {
                {1,             1},
                {2,             2},
                {4,             3},
                {8,             4},
                {16,            5},
                {32,            6},
                {64,            7},
                {128,           8},
                {256,           9},
                {512,           10},
                {1024,          11},
                {2097152,       22},
                {8388608,       24},
                {16777216,      25},
                {33554432,      26},
                {67108864,      27},
                {134217728,     28},
                {268435456,     29},
                {536870912,     30},
                {1073741824,    31},
                {2147483648,    32},
                {2048,          34},
                {4096,          35},
                {8192,          36},
                {16384,         37}
            };

            CharacterLoadout = dbcd.Load($"{DB2Path}/CharacterLoadout.db2");
            CharacterLoadoutItem = dbcd.Load($"{DB2Path}/CharacterLoadoutItem.db2");

            var charLoadout = new Dictionary<(byte, int), List<uint>>();
            foreach (var id in CharacterLoadoutItem.Keys)
            {
                var loadoutId = CharacterLoadoutItem.GetField<int>(id, "CharacterLoadoutID");
                if (loadoutId == 0)
                    continue;

                var purpose = CharacterLoadout.GetField<int>(loadoutId, "Purpose");
                if (purpose != 9)
                    continue;

                var raceMask = CharacterLoadout.GetField<uint>(loadoutId, "Racemask");
                if (!raceDict.TryGetValue(raceMask, out var raceId))
                {
                    Console.WriteLine($"Unknown Racemask: {raceMask}");
                    continue;
                }

                var itemId = CharacterLoadoutItem.GetField<uint>(id, "ItemID");
                if (itemId == 0)
                    continue;

                var classId = CharacterLoadout.GetField<int>(loadoutId, "ChrClassID");
                if (!charLoadout.ContainsKey((raceId, classId)))
                    charLoadout.Add((raceId, classId), new List<uint>());

                charLoadout[(raceId, classId)].Add(itemId);
            }

            var loadoutList = new List<CharacterLoadout>();
            foreach (var loadout in charLoadout)
            {
                loadout.Value.ForEach(itemId =>
                {
                    loadoutList.Add(new CharacterLoadout
                    {
                        RaceID = loadout.Key.Item1,
                        ClassID = loadout.Key.Item2,
                        ItemID = itemId,
                    });
                });
            }

            WriteRecords("CharacterLoadout.csv", loadoutList);
        }
    }

    public class DBCProvider : IDBCProvider
    {
        public Stream StreamForTableName(string tableName, string build) => File.OpenRead(tableName);
    }

    public class DBDProvider : IDBDProvider
    {
        private static Uri baseURI = new Uri("https://raw.githubusercontent.com/wowdev/WoWDBDefs/master/definitions/");
        private HttpClient client = new HttpClient();

        public DBDProvider()
        {
            client.BaseAddress = baseURI;
        }

        /// <summary>
        /// Get <see cref="Stream"/> for a DB2/DBC table.
        /// </summary>
        public Stream StreamForTableName(string tableName, string build = null)
        {
            var dbdName = Path.GetFileName(tableName).Replace(".db2", ".dbd");
            var bytes = client.GetByteArrayAsync(dbdName).Result;

            return new MemoryStream(bytes);
        }
    }
}
