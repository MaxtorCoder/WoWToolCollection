using System.ComponentModel;
using System.IO;
using CASCLib;

namespace FilenameGuesser
{
    public static class CASC
    {
        private static CASCHandler cascHandler;

        public static void LoadCASC()
        {
            //cascHandler = CASCHandler.OpenLocalStorage(@"C:\Program Files (x86)\World of Warcraft", "wow_beta");
            cascHandler = CASCHandler.OpenOnlineStorage("wow_beta");
            cascHandler.Root.SetFlags(LocaleFlags.enUS, createTree: false);
        }

        public static Stream OpenFile(uint fileDataId)
        {
            if (!cascHandler.FileExists((int) fileDataId))
                return null;

            var stream = cascHandler.OpenFile((int) fileDataId);
            return stream;
        }
    }
}