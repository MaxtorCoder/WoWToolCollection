using System.IO;

namespace BuildMonitor
{
    public class Versions
    {
        public string Product { get; set; }
        public uint SequenceNumber { get; set; }
        public string Region { get; set; }
        public string BuildConfig { get; set; }
        public string CDNConfig { get; set; }
        public string KeyRing { get; set; }
        public uint BuildId { get; set; }
        public string VersionsName { get; set; }
        public string ProductConfig { get; set; }

        /// <summary>
        /// Parse the <see cref="Versions"/> file and fill the structure.
        /// </summary>
        public bool Parse(string file)
        {
            using (var reader = new StreamReader(file))
            {
                reader.ReadLine();

                var sequenceLine = reader.ReadLine().Replace("## seqn = ", "");
                SequenceNumber = uint.Parse(sequenceLine);

                var structure = reader.ReadLine().Split("|");
                if (structure.Length < 6)
                    return false;

                Region          = structure[0];
                BuildConfig     = structure[1];
                CDNConfig       = structure[2];

                if (structure[3] != string.Empty)
                    KeyRing = structure[3];

                BuildId         = uint.Parse(structure[4]);
                VersionsName    = structure[5];
                ProductConfig   = structure[6];

                return true;
            }
        }
    }
}
