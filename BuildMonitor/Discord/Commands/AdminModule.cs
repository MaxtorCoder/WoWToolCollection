using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace BuildMonitor.Discord.Commands
{
    [Group("admin")]
    [RequireOwner]
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("maintenance")]
        [Summary("Broadcasts to other channels that there is maintenance")]
        public async Task SetMaintenance()
        {
            foreach (var guildSettings in DiscordManager.DiscordGuildSettings)
            {
                var guild = DiscordManager.Bot.GetClient().GetGuild(guildSettings.Key);
                if (guild == null)
                {
                    Console.WriteLine($"Guild does not exist {guildSettings.Key}");
                    continue;
                }

                var guildChannel = guild.GetChannel(guildSettings.Value.BuildMonitorChannelId) as ISocketMessageChannel;
                if (guildChannel == null)
                {
                    Console.WriteLine($"Channel does not exist {guildSettings.Value.BuildMonitorChannelId}");
                    continue;
                }

                await guildChannel.SendMessageAsync("**Bot is going into Maintenance!**");
            }

            Environment.Exit(0);
        }
    }
}