using CASCLib;
using System.IO;

namespace BuildMonitor
{
    public static class CASC
    {
        public static CASCHandler OldStorage;
        public static CASCHandler NewStorage;

        /// <summary>
        /// Open both instances of <see cref="CASCHandler"/> (old and new)
        /// </summary>
        public static void OpenCasc(string product, string buildConfig, string cdnConfig)
        {
            // Open old CASC.
            OldStorage = CASCHandler.OpenSpecificStorage(product, buildConfig, cdnConfig);
            OldStorage.Root.SetFlags(LocaleFlags.All_WoW);

            // Open new CASC.
            NewStorage = CASCHandler.OpenOnlineStorage(product);
            NewStorage.Root.SetFlags(LocaleFlags.All_WoW);
        }

        /// <summary>
        /// Open a new <see cref="BinaryReader"/> instance with the given FileId.
        /// </summary>
        public static BinaryReader OpenFile(uint fileDataId)
        {
            if (!NewStorage.FileExists((int)fileDataId))
                return null;

            var stream = NewStorage.OpenFile((int)fileDataId);
            if (stream == null)
                return null;

            return new BinaryReader(stream);
        }
    }
}
