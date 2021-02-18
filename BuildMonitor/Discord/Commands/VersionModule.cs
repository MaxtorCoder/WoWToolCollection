using BuildMonitor.Util;
using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace BuildMonitor.Discord.Commands
{
    [Group("version")]
    public class VersionModule : ModuleBase<SocketCommandContext>
    {
        [Command("get")]
        [Summary("Retrieves the version info with the given Branch Name")]
        public async Task GetVersionInfoAsync(string branchName = "all")
        {
            var embed = new EmbedBuilder();
            if (branchName == "all")
            {
                embed.Title = "All Available branches: ";

                foreach (var version in Ribbit.VersionsInfo)
                {
                    embed.Description += $"[`{version.Key}`] **({version.Key.GetProduct()})** with Build: {version.Value.BuildId}\n";
                }
            }
            else
            {
                if (!Ribbit.VersionsInfo.TryGetValue(branchName, out var versionInfo))
                {
                    var request = DiscordManager.RibbitClient.RequestVersions(branchName);
                    if (request == string.Empty)
                    {
                        await ReplyAsync($"`{branchName}` does not exist!");
                        return;        
                    }

                    versionInfo = Ribbit.ParseVersions(request, branchName);
                    if (versionInfo == null)
                    {
                        await ReplyAsync($"Parsing versions for `{branchName}` failed!");
                        return;  
                    }
                }

                var isEncrypted = branchName.IsEncrypted();
                embed.Title       = $"Version info for **{branchName.GetProduct()}** (`{branchName}`)";
                embed.Description = $"**Build**: `{versionInfo.BuildId}`\n" +
                                    $"**BuildConfig**  : `{versionInfo.BuildConfig.Substring(0, 6)}`\n" +
                                    $"**CDNConfig**    : `{versionInfo.CDNConfig.Substring(0, 6)}`\n" +
                                    $"**ProductConfig**: `{versionInfo.ProductConfig.Substring(0, 6)}`\n" + 
                                    $"**VersionsName** : `{versionInfo.VersionsName}`\n" +
                                    $"**Is Encrypted**: `{(isEncrypted ? "Yes" : "No")}`\n";
            }

            embed.Timestamp = DateTime.Now;

            await ReplyAsync("Here is the version info you requested:", embed: embed.Build());
        }

        [Command("process")]
        [RequireOwner]
        public async Task HandleProcessVersion(string branchName, string oldBuild)
        {
            var request = DiscordManager.RibbitClient.RequestVersions(branchName);
            if (request == string.Empty)
            {
                await ReplyAsync($"`{branchName}` does not exist!");
                return;        
            }

            var versionInfo = Ribbit.ParseVersions(request, branchName);
            if (versionInfo == null)
            {
                await ReplyAsync($"Parsing versions for `{branchName}` failed!");
                return;  
            }

            await ReplyAsync($"Processing `{branchName}`..");
            await Ribbit.HandleNewBuild(versionInfo, $"cache/temp", oldBuild);
        }
    }
}
