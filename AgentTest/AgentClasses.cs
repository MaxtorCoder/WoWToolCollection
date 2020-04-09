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

    #region Install Response
    public class ExtendedStatus
    {
        public double current { get; set; }
        public double total { get; set; }
        public double state { get; set; }
        public double remaining { get; set; }
        public double rate { get; set; }
        public string unit_type { get; set; }
    }

    public class SharedContainerInfo
    {
        public string subpath { get; set; }
    }

    public class Us
    {
        public string config_key { get; set; }
        public string display_version { get; set; }
        public bool playable { get; set; }
    }

    public class Eu
    {
        public string config_key { get; set; }
        public string display_version { get; set; }
        public bool selected { get; set; }
        public bool playable { get; set; }
    }

    public class Cn
    {
        public string config_key { get; set; }
        public string display_version { get; set; }
        public bool playable { get; set; }
    }

    public class Kr
    {
        public string config_key { get; set; }
        public string display_version { get; set; }
        public bool playable { get; set; }
    }

    public class RegionalVersionInfo
    {
        public Us us { get; set; }
        public Eu eu { get; set; }
        public Cn cn { get; set; }
        public Kr kr { get; set; }
    }

    public class UpdateResponse
    {
        public bool installed { get; set; }
        public double progress { get; set; }
        public double playable_progress { get; set; }
        public bool download_complete { get; set; }
        public bool patch_application_complete { get; set; }
        public double state { get; set; }
        public double download_rate { get; set; }
        public bool paused { get; set; }
        public double download_remaining { get; set; }
        public double info_download_bytes { get; set; }
        public double info_written_bytes { get; set; }
        public double info_failed_bytes { get; set; }
        public double info_expected_bytes { get; set; }
        public double info_expected_org_bytes { get; set; }
        public bool needs_rebase { get; set; }
        public bool ignore_disc { get; set; }
        public bool using_media { get; set; }
        public ExtendedStatus extended_status { get; set; }
        public string product_family { get; set; }
        public SharedContainerInfo shared_container_info { get; set; }
        public string region { get; set; }
        public string branch { get; set; }
        public string local_version { get; set; }
        public string active_config_key { get; set; }
        public double current_version { get; set; }
        public bool playable { get; set; }
        public RegionalVersionInfo regional_version_info { get; set; }
    }
    #endregion
}
