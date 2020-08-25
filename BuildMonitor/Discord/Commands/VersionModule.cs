using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace BuildMonitor.Discord.Commands
{
    public class VersionModule : ModuleBase<SocketCommandContext>
    {
        [Command("version")]
        [Summary("Retrieves the version info with the given Branch Name")]
        public async Task GetVersionInfoAsync(string branchName)
        {
            if (!BuildMonitor.VersionsInfo.TryGetValue(branchName, out var versionInfo))
            {
                await ReplyAsync($"`{branchName}` does not exist! Try again later!");
                return;
            }

            var isEncrypted = (branchName == "wowdev" || branchName == "wowv" || branchName == "wowv2");
            var embed = new EmbedBuilder
            {
                Title       = $"Version info for `{branchName}`",
                Description = $"**Build**: `{versionInfo.BuildId}`\n**Is Encrypted**: `{(isEncrypted ? "Yes" : "No")}`\n" +
                              $"**BuildConfig**  : `{versionInfo.BuildConfig.Substring(0, 6)}`\n" +
                              $"**CDNConfig**    : `{versionInfo.CDNConfig.Substring(0, 6)}`\n" +
                              $"**ProductConfig**: `{versionInfo.ProductConfig.Substring(0, 6)}`\n",
                Timestamp   = DateTime.Now,
            };

            await ReplyAsync("Here is the version info you requested:", embed: embed.Build());
        }
    }
}
