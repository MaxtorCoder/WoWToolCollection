using System;
using System.IO;
using System.Linq;

namespace CASCLib
{
    public sealed class CASCHandler : CASCHandlerBase
    {
        public EncodingHandler Encoding { get; private set; }
        public DownloadHandler Download { get; private set; }
        public RootHandlerBase Root { get; private set; }
        public InstallHandler Install { get; private set; }

        private CASCHandler(CASCConfig config) : base(config)
        {
            using (var fs = OpenEncodingFile(this))
                Encoding = new EncodingHandler(fs);

            if ((CASCConfig.LoadFlags & LoadFlags.Download) != 0)
            {
                using (var fs = OpenDownloadFile(Encoding, this))
                    Download = new DownloadHandler(fs);
            }

            KeyService.LoadKeys();

            using (var fs = OpenRootFile(Encoding, this))
            {
                if (config.GameType == CASCGameType.WoW)
                    Root = new WowRootHandler(fs);
                else
                {
                    using (var ufs = new FileStream("unk_root", FileMode.Create))
                        fs.BaseStream.CopyTo(ufs);

                    throw new Exception("Unsupported game " + config.BuildUID);
                }
            }

            if ((CASCConfig.LoadFlags & LoadFlags.Install) != 0)
            {
                using (var fs = OpenInstallFile(Encoding, this))
                    Install = new InstallHandler(fs);
            }
        }

        public static CASCHandler OpenStorage(CASCConfig config) => Open(config);

        public static CASCHandler OpenLocalStorage(string basePath, string product = null)
        {
            CASCConfig config = CASCConfig.LoadLocalStorageConfig(basePath, product);

            return Open(config);
        }

        public static CASCHandler OpenOnlineStorage(string product, string region = "us")
        {
            CASCConfig config = CASCConfig.LoadOnlineStorageConfig(product, region);

            return Open(config);
        }

        public static CASCHandler OpenSpecificStorage(string product, string buildconfig, string encoding)
        {
            var config = CASCConfig.LoadSpecificStorageConfig(product, buildconfig, encoding);

            return Open(config);
        }

        private static CASCHandler Open(CASCConfig config)
        {
            return new CASCHandler(config);
        }

        public override bool FileExists(int fileDataId)
        {
            if (Root is WowRootHandler rh)
                return rh.FileExist(fileDataId);
            return false;
        }

        public override bool FileExists(string file) => FileExists(Hasher.ComputeHash(file));

        public override bool FileExists(ulong hash) => Root.GetAllEntries(hash).Any();

        public bool GetEncodingEntry(ulong hash, out EncodingEntry enc)
        {
            var rootInfos = Root.GetEntries(hash);
            if (rootInfos.Any())
                return Encoding.GetEntry(rootInfos.First().MD5, out enc);

            if ((CASCConfig.LoadFlags & LoadFlags.Install) != 0)
            {
                var installInfos = Install.GetEntries().Where(e => Hasher.ComputeHash(e.Name) == hash && e.Tags.Any(t => t.Type == 1 && t.Name == Root.Locale.ToString()));
                if (installInfos.Any())
                    return Encoding.GetEntry(installInfos.First().MD5, out enc);

                installInfos = Install.GetEntries().Where(e => Hasher.ComputeHash(e.Name) == hash);
                if (installInfos.Any())
                    return Encoding.GetEntry(installInfos.First().MD5, out enc);
            }

            enc = default;
            return false;
        }

        public override Stream OpenFile(int fileDataId)
        {
            if (Root is WowRootHandler rh)
                if (FileExists(fileDataId))
                    return OpenFile(rh.GetHashByFileDataId(fileDataId));

            // if (CASCConfig.ThrowOnFileNotFound)
            //     Console.WriteLine($"File '{fileDataId}' was not found!");

            return null;
        }

        public override Stream OpenFile(string name) => OpenFile(Hasher.ComputeHash(name));

        public override Stream OpenFile(ulong hash)
        {
            if (hash == 0)
                return null;

            if (GetEncodingEntry(hash, out EncodingEntry encInfo))
                return OpenFile(encInfo.Key);

            if (CASCConfig.ThrowOnFileNotFound)
                Console.WriteLine($"File '{hash:X16}' was not found!");

            return null;
        }

        public override void SaveFileTo(ulong hash, string extractPath, string fullName)
        {
            if (GetEncodingEntry(hash, out EncodingEntry encInfo))
            {
                SaveFileTo(encInfo.Key, extractPath, fullName);
                return;
            }

            if (CASCConfig.ThrowOnFileNotFound)
                throw new FileNotFoundException(fullName);
        }

        protected override Stream OpenFileOnline(MD5Hash key)
        {
            IndexEntry idxInfo = CDNIndex.GetIndexInfo(key);
            return OpenFileOnlineInternal(idxInfo, key);
        }

        protected override Stream GetLocalDataStream(MD5Hash key)
        {
            IndexEntry idxInfo = LocalIndex.GetIndexInfo(key);
            if (idxInfo == null)
            {
                Console.WriteLine("Local index missing: {0}", key.ToHexString());
            }
            return GetLocalDataStreamInternal(idxInfo, key);
        }

        protected override void ExtractFileOnline(MD5Hash key, string path, string name)
        {
            IndexEntry idxInfo = CDNIndex.GetIndexInfo(key);
            ExtractFileOnlineInternal(idxInfo, key, path, name);
        }

        public void Clear()
        {
            CDNIndex?.Clear();
            CDNIndex = null;

            foreach (var stream in DataStreams)
                stream.Value.Dispose();

            DataStreams.Clear();

            Encoding?.Clear();
            Encoding = null;

            Install?.Clear();
            Install = null;

            LocalIndex?.Clear();
            LocalIndex = null;

            Root?.Clear();
            Root = null;

            Download?.Clear();
            Download = null;
        }
    }
}
