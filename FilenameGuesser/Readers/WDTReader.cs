using FilenameGuesser.Util;
using System.Collections.Generic;
using System.IO;

namespace FilenameGuesser.Readers
{
    public class WDTReader
    {
        public Dictionary<string, MAID> MAIDs = new Dictionary<string, MAID>();

        private string path;

        /// <summary>
        /// Create a new instance of <see cref="WDTReader"/>
        /// </summary>
        /// <param name="path"></param>
        public WDTReader(string path)
        {
            this.path = path;
        }

        /// <summary>
        /// Read the WDT file.
        /// </summary>
        public void ReadWDT()
        {
            using (var stream = new MemoryStream(File.ReadAllBytes(path)))
            using (var reader = new BinaryReader(stream))
            {
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    var chunkId     = (Chunk)reader.ReadUInt32().FlipUInt();
                    var chunkSize   = reader.ReadUInt32();

                    switch (chunkId)
                    {
                        case Chunk.MAID:
                            for (var x = 0; x < 64; ++x)
                            {
                                for (var y = 0; y < 64; ++y)
                                {
                                    var rootAdt = reader.ReadUInt32();
                                    var obj0Adt = reader.ReadUInt32();
                                    var obj1Adt = reader.ReadUInt32();
                                    var tex0Adt = reader.ReadUInt32();
                                    var lodAdt = reader.ReadUInt32();
                                    var mapTexture = reader.ReadUInt32();
                                    var mapTextureN = reader.ReadUInt32();
                                    var minimapTexture = reader.ReadUInt32();

                                    if (rootAdt == 0)
                                        continue;

                                    MAIDs.Add($"{x}_{y}", new MAID
                                    {
                                        RootADT         = rootAdt,
                                        Obj0ADT         = obj0Adt,
                                        Obj1ADT         = obj1Adt,
                                        Tex0ADT         = tex0Adt,
                                        LodADT          = lodAdt,
                                        MapTexture      = mapTexture,
                                        MapTextureN     = mapTextureN,
                                        MinimapTexture  = minimapTexture,
                                    });
                                }
                            }

                            break;
                        default:
                            Skip(reader, chunkSize);
                            break;
                    }
                }
            }
        }

        private void Skip(BinaryReader reader, uint size) => reader.BaseStream.Seek(size, SeekOrigin.Current);
    }

    public struct MAID
    {
        public uint RootADT;
        public uint Obj0ADT;
        public uint Obj1ADT;
        public uint Tex0ADT;
        public uint LodADT;
        public uint MapTexture;
        public uint MapTextureN;
        public uint MinimapTexture;
    }
}
