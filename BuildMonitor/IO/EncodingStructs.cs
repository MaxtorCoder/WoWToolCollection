using System;
using System.Collections.Generic;
using System.Text;

namespace BuildMonitor.IO
{
    public struct EncodingFile
    {
        public byte version;
        public byte cKeyLength;
        public byte eKeyLength;
        public ushort cKeyPageSize;
        public ushort eKeyPageSize;
        public uint cKeyPageCount;
        public uint eKeyPageCount;
        public byte unk;
        public ulong stringBlockSize;
        public string[] stringBlockEntries;
        public EncodingHeaderEntry[] aHeaders;
        public Dictionary<MD5Hash, EncodingFileEntry> aEntries;
        public EncodingHeaderEntry[] bHeaders;
        public EncodingFileDescEntry[] bEntries;
    }

    public struct EncodingHeaderEntry
    {
        public MD5Hash firstHash;
        public MD5Hash checksum;
    }

    public struct EncodingFileEntry
    {
        public long size;
        public MD5Hash eKey;
    }

    public struct EncodingFileDescEntry
    {
        public MD5Hash key;
        public uint stringIndex;
        public ulong compressedSize;
    }
}
