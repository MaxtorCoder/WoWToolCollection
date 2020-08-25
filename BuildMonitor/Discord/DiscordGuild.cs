namespace BuildMonitor.Discord
{
    public class DiscordGuild
    {
        public string ServerName { get; set; }
        public ulong BuildMonitorChannelId { get; set; }
        public char BotPrefix { get; set; }
        public bool IsDebugServer { get; set; }
    }
}
