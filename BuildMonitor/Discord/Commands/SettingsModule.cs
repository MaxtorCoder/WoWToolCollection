using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace BuildMonitor.Discord.Commands
{
    [Group("settings")]
    [RequireUserPermission(ChannelPermission.ManageChannels)]
    public class SettingsModule : ModuleBase<SocketCommandContext>
    {
        [Group("set")]
        [Summary("Command group for settings")]
        public class SetModule : ModuleBase<SocketCommandContext>
        {
            [Command("buildchannel")]
            [Summary("Set the build-monitor channel ID")]
            public async Task SetBuildChannelAsync(SocketGuildChannel channel)
            {
                var discordSettings = DiscordManager.GetDiscordSettings(channel.Guild.Id);
                if (discordSettings == null)
                    return;

                discordSettings.BuildMonitorChannelId = channel.Id;

                // Update settings
                DiscordManager.SetSettings(channel.Guild.Id, discordSettings);

                // Reply
                await ReplyAsync($"Done, set buildmonitor channel to <#{channel.Id}>!");
            }

            [Command("prefix")]
            [Summary("Set the prefix")]
            public async Task SetPrefixAsync(char prefix)
            {
                var channel = Context.Channel as SocketGuildChannel;
                if (channel == null)
                    return;

                var discordSettings = DiscordManager.GetDiscordSettings(channel.Guild.Id);
                if (discordSettings == null)
                    return;

                discordSettings.BotPrefix = prefix;

                // Update settings
                DiscordManager.SetSettings(channel.Guild.Id, discordSettings);

                // Reply
                await ReplyAsync($"Done, set bot prefix to '{prefix}'!");
            }
        }
    }
}
