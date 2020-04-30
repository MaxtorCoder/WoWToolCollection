using CASCLib;
using System.IO;

namespace BuildMonitor.IO
{
    // https://github.com/Marlamin/CASCToolHost/blob/master/CASCToolHost/CASC/NGDP.cs#L197
    public static class Root
    {
        const uint HeaderFmt = 1296454484;

        /// <summary>
        /// Parse the root file into <see cref="RootFile"/>
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static RootFile ParseRoot(MemoryStream contentStream)
        {
            var rootfile = new RootFile
            {
                Lookup = new MultiDictionary<ulong, RootEntry>(),
                FileDataIds = new MultiDictionary<uint, RootEntry>()
            };

            var namedCount      = 0;
            var unnamedCount    = 0;
            var newRoot         = false;

            using (var stream = new MemoryStream(BLTE.Parse(contentStream.ToArray())))
            using (var reader = new BinaryReader(stream))
            {
                var header = reader.ReadUInt32();
                if (header == HeaderFmt)
                {
                    var totalFiles = reader.ReadUInt32();
                    var namedFiles = reader.ReadUInt32();
                    newRoot = true;
                }
                else
                    reader.BaseStream.Position = 0;

                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    var count           = reader.ReadUInt32();
                    var contentFlags    = (ContentFlags)reader.ReadUInt32();
                    var localeFlags     = (LocaleFlags)reader.ReadUInt32();

                    var rootEntries = new RootEntry[count];
                    var fileDataIds = new int[count];

                    var idx = 0;
                    for (var i = 0; i < count; ++i)
                    {
                        rootEntries[i].LocaleFlags  = localeFlags;
                        rootEntries[i].ContentFlags = contentFlags;

                        fileDataIds[i] = idx + reader.ReadInt32();
                        rootEntries[i].FileDataId = (uint)fileDataIds[i];
                        idx = fileDataIds[i] + 1;
                    }

                    if (!newRoot)
                    {
                        for (var i = 0; i < count; ++i)
                        {
                            rootEntries[i].MD5      = reader.Read<MD5Hash>();
                            rootEntries[i].Lookup   = reader.ReadUInt64();

                            rootfile.Lookup.Add(rootEntries[i].Lookup, rootEntries[i]);
                            rootfile.FileDataIds.Add(rootEntries[i].FileDataId, rootEntries[i]);
                        }
                    }
                    else
                    {
                        for (var i = 0; i < count; ++i)
                            rootEntries[i].MD5 = reader.Read<MD5Hash>();

                        for (var i = 0; i < count; ++i)
                        {
                            if (contentFlags.HasFlag(ContentFlags.NoNameHash))
                            {
                                rootEntries[i].Lookup = 0;
                                unnamedCount++;
                            }
                            else
                            {
                                rootEntries[i].Lookup = reader.ReadUInt64();
                                namedCount++;

                                rootfile.Lookup.Add(rootEntries[i].Lookup, rootEntries[i]);
                            }

                            rootfile.FileDataIds.Add(rootEntries[i].FileDataId, rootEntries[i]);
                        }
                    }
                }
            }

            return rootfile;
        }
    }

    public class RootFile
    {
        public MultiDictionary<ulong, RootEntry> Lookup;
        public MultiDictionary<uint, RootEntry> FileDataIds;
    }
}
