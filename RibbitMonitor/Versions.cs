using Ribbit.Constants;
using System;
using System.Collections.Generic;
using System.Text;

namespace RibbitMonitor
{
    public struct Versions
    {
        public string Region { get; set; }
        public string BuildConfig { get; set; }
        public string CDNConfig { get; set; }
        public string KeyRing { get; set; }
        public uint BuildId { get; set; }
        public string VersionsName { get; set; }
        public string ProductConfig { get; set; }
    }
}
