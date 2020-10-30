using CASCLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BuildMonitor.IO.CASC
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

        /// <summary>
        /// Diff the 2 root files.
        /// Completely taken from https://github.com/Marlamin/CASCToolHost/blob/master/CASCToolHost/Controllers/RootController.cs#L59
        /// </summary>
        /// <param name="oldRoot"></param>
        /// <param name="newRoot"></param>
        public static async Task<List<RootEntry>> DiffRoot(string oldRootHash, string newRootHash)
        {
            var oldRootStream = await HTTP.RequestCDN($"tpr/wow/data/{oldRootHash.Substring(0, 2)}/{oldRootHash.Substring(2, 2)}/{oldRootHash}");
            var newRootStream = await HTTP.RequestCDN($"tpr/wow/data/{newRootHash.Substring(0, 2)}/{newRootHash.Substring(2, 2)}/{newRootHash}");

            if (oldRootStream == null || newRootStream == null)
                return new List<RootEntry>();
            
            var rootFromEntries = ParseRoot(oldRootStream).FileDataIds;
            var rootToEntries   = ParseRoot(newRootStream).FileDataIds;
            
            var fromEntries     = rootFromEntries.Keys.ToHashSet();
            var toEntries       = rootToEntries.Keys.ToHashSet();
            
            var commonEntries   = fromEntries.Intersect(toEntries);
            var removedEntries  = fromEntries.Except(commonEntries);
            var addedEntries    = toEntries.Except(commonEntries);
            
            static RootEntry Prioritize(List<RootEntry> entries)
            {
                var prioritized = entries.FirstOrDefault(subEntry =>
                    subEntry.ContentFlags.HasFlag(ContentFlags.Alternate) == false &&
                    (subEntry.LocaleFlags.HasFlag(LocaleFlags.All_WoW) || subEntry.LocaleFlags.HasFlag(LocaleFlags.enUS))
                );
            
                if (prioritized.FileDataId != 0)
                    return prioritized;
                else
                    return entries.First();
            }
            
            var addedFiles = addedEntries.Select(entry => rootToEntries[entry]).Select(Prioritize);
            var removedFiles = removedEntries.Select(entry => rootFromEntries[entry]).Select(Prioritize);
            
            var modifiedFiles = new List<RootEntry>();
            foreach (var entry in commonEntries)
            {
                var originalFile = Prioritize(rootFromEntries[entry]);
                var patchedFile = Prioritize(rootToEntries[entry]);
            
                if (originalFile.MD5.Equals(patchedFile.MD5))
                    continue;
            
                modifiedFiles.Add(patchedFile);
            }
            
            return addedFiles.ToList();
        }
    }

    public class RootFile
    {
        public MultiDictionary<ulong, RootEntry> Lookup;
        public MultiDictionary<uint, RootEntry> FileDataIds;
    }
}
