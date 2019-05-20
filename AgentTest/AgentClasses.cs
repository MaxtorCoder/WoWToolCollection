using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AgentTest
{
    public class Register
    {
        [JsonProperty(PropertyName = "install_dir")]
        public string InstallationDir { get; set; }
        [JsonProperty(PropertyName = "primary_locale_hint")]
        public string PrimaryLocale { get; set; }
        [JsonProperty(PropertyName = "uid")]
        public string UID { get; set; }
    }

    public class Install
    {
        [JsonProperty(PropertyName = "instructions_dataset")]
        public string[] InstructiosDataSet { get; set; }
        [JsonProperty(PropertyName = "instructions_patch_url")]
        public string InstructionsPatch { get; set; }
        [JsonProperty(PropertyName = "instructions_product")]
        public string InstructionProduct { get; set; }
        [JsonProperty(PropertyName = "monitor_pid")]
        public uint MonitorPID { get; set; }
        [JsonProperty(PropertyName = "priority")]
        public Priority Priority { get; set; }
        [JsonProperty(PropertyName = "uid")]
        public string UID { get; set; }
    }

    public class InstallBeta
    {
        [JsonProperty(PropertyName = "account_country")]
        public string AccountCountry { get; set; }
        [JsonProperty(PropertyName = "finalized")]
        public bool Finalized { get; set; }
        [JsonProperty(PropertyName = "game_dir")]
        public string GameDir { get; set; }
        [JsonProperty(PropertyName = "geo_ip_country")]
        public string GeoIPCountry { get; set; }
        [JsonProperty(PropertyName = "language")]
        public string[] Language { get; set; }
        [JsonProperty(PropertyName = "selected_asset_locale")]
        public string SelectedAsset { get; set; }
        [JsonProperty(PropertyName = "selected_locale")]
        public string SelectedLocale { get; set; }
        [JsonProperty(PropertyName = "shortcut")]
        public string Shortcut { get; set; }
        [JsonProperty(PropertyName = "tome_torrent")]
        public string TomeTorrent { get; set; }
    }

    public class Update
    {
        [JsonProperty(PropertyName = "priority")]
        public Priority Priority { get; set; }
        [JsonProperty(PropertyName = "uid")]
        public string UID { get; set; }
    }

    public class Priority
    {
        [JsonProperty(PropertyName = "insert_at_head")]
        public bool InsertAtHead { get; set; }
        [JsonProperty(PropertyName = "value")]
        public uint Value { get; set; }
    }

    public class Uninstall
    {
        [JsonProperty(PropertyName = "run_compaction")]
        public bool RunCompaction { get; set; }
        [JsonProperty(PropertyName = "uid")]
        public string UID { get; set; }
    }
}
