using BuildMonitor.Util;
using CASCLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BuildMonitor.IO.CASC
{
    // https://github.com/Marlamin/CASCToolHost/blob/master/CASCToolHost/CASC/NGDP.cs#L287
    public static class Encoding
    {
        public static Dictionary<MD5Hash, MD5Hash> EncodingDictionary = new Dictionary<MD5Hash, MD5Hash>(new MD5HashComparer());

        /// <summary>
        /// Read the Encoding file and return a parsed <see cref="EncodingFile"/>
        /// </summary>
        public static EncodingFile ParseEncoding(MemoryStream contentStream)
        {
            var encodingFile = new EncodingFile();

            using (var stream = new MemoryStream(BLTE.Parse(contentStream.ToArray())))
            using (var reader = new BinaryReader(stream))
            {
                if (System.Text.Encoding.UTF8.GetString(reader.ReadBytes(2)) != "EN")
                    throw new Exception("Encoding file might be corrupted!");

                encodingFile.version            = reader.ReadByte();
                encodingFile.cKeyLength         = reader.ReadByte();
                encodingFile.eKeyLength         = reader.ReadByte();
                encodingFile.cKeyPageSize       = reader.ReadUInt16(true);
                encodingFile.eKeyPageSize       = reader.ReadUInt16(true);
                encodingFile.cKeyPageCount      = reader.ReadUInt32(true);
                encodingFile.eKeyPageCount      = reader.ReadUInt32(true);
                encodingFile.stringBlockSize    = reader.ReadUInt40(true);

                reader.BaseStream.Position += (long)encodingFile.stringBlockSize;
                reader.BaseStream.Position += encodingFile.cKeyPageCount * 32;

                var tableAStart = reader.BaseStream.Position;
                var entries = new Dictionary<MD5Hash, EncodingFileEntry>(new MD5HashComparer());

                for (var i = 0; i < encodingFile.cKeyPageCount; ++i)
                {
                    byte keysCount;

                    while ((keysCount = reader.ReadByte()) != 0)
                    {
                        var entry = new EncodingFileEntry { size = reader.ReadInt40BE() };
                        var cKey = reader.Read<MD5Hash>();

                        // @TODO add support for multiple encoding keys
                        for (int key = 0; key < keysCount; key++)
                        {
                            if (key == 0)
                                entry.eKey = reader.Read<MD5Hash>();
                            else
                                reader.ReadBytes(16);
                        }

                        entries.Add(cKey, entry);

                        if (!EncodingDictionary.ContainsKey(cKey))
                            EncodingDictionary.Add(cKey, entry.eKey);
                    }

                    var remaining = 4096 - ((reader.BaseStream.Position - tableAStart) % 4096);
                    if (remaining > 0) 
                        reader.BaseStream.Position += remaining;
                }

                encodingFile.aEntries = entries;
            }

            return encodingFile;
        }

        /// <summary>
        /// Retrieve the root hash from the <see cref="EncodingFile"/>
        /// </summary>
        public static async Task<(MD5Hash Encoding, string Hash)> RetrieveRootHash(MemoryStream stream)
        {
            var (encoding, hash) = (new MD5Hash(), string.Empty);
            using (var reader = new StreamReader(stream))
            {
                reader.ReadLine();
                reader.ReadLine();

                var rootContentHash = reader.ReadLine().Split(" = ")[1];

                // Skip to encoding.
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (!line.StartsWith("encoding = "))
                        continue;

                    line = line.Replace("=", "");

                    var encodingHash = line.Split(" ", StringSplitOptions.RemoveEmptyEntries)[2];

                    var encodingStream = await HTTP.RequestCDN($"tpr/wow/data/{encodingHash.Substring(0, 2)}/{encodingHash.Substring(2, 2)}/{encodingHash}");
                    if (encodingStream != null)
                    {
                        // Parse the Encoding file to get hashes.
                        ParseEncoding(encodingStream);

                        // Retrieve the Root encoding
                        if (EncodingDictionary.TryGetValue(rootContentHash.ToByteArray().ToMD5(), out var entry))
                        {
                            encoding = entry;
                            hash = encodingHash.ToLower();

                            break;
                        }
                    }
                }
            }

            return (encoding, hash);
        }
    }
}
