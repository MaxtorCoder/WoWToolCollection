using Discord;
using Discord.Webhook;
using System;
using System.Collections.Generic;
using System.IO;

namespace BuildMonitor
{
    public static class DiscordServer
    {
        public static DiscordWebhookClient Webhook;
        public static DiscordWebhookClient MrGMHook;

        /// <summary>
        /// Initialize the <see cref="DiscordWebhookClient"/> instance.
        /// </summary>
        public static void Initialize()
        {
            // Initialize the Discord Webhook.
            Webhook = new DiscordWebhookClient(File.ReadAllText("webhook.txt"));
            if (Webhook == null)
                throw new Exception("Webhook is null!");

            // Initialize MrGMs Discord Webhook.
            // MrGMHook = new DiscordWebhookClient(File.ReadAllText("webhook_mrgm.txt"));
            // if (MrGMHook == null)
            //     throw new Exception("Webhook is null!");
        }

        /// <summary>
        /// Log message to console and webhook
        /// </summary>
        public static void Log(string message, bool error = false)
        {
            // Replace ` and * because fck that
            Console.WriteLine(message.Replace("`", "").Replace("*", ""));

            if (error)
                Webhook.SendMessageAsync($"<@376821416105869315> : ```{message}```");
            else
                Webhook.SendMessageAsync(message);
        }

        /// <summary>
        /// Build an <see cref="Embed"/>.
        /// </summary>
        public static void SendEmbed(string product, VersionsInfo oldVersion, VersionsInfo newVersion)
        {
            var isEncrypted = (product == "wowdev" || product == "wowv" || product == "wowv2");
            var embed = new EmbedBuilder
            {
                Title           = $"New Build for `{product}`",
                Description     = $"⚠️ There is a new build for `{product}`! **{oldVersion.BuildId}** <:arrowJoin:740705934644609024> **{newVersion.BuildId}**\n" +
                                  $"{(isEncrypted ? "\n⛔ This build is **NOT** datamineable\n" : "")}" +
                                  $"\n**Build Config**:\n" +
                                  $"`{oldVersion.BuildConfig.Substring(0, 6)}` <:arrowJoin:740705934644609024> `{newVersion.BuildConfig.Substring(0, 6)}`" +
                                  $"\n**CDN Config**:\n" +
                                  $"`{oldVersion.CDNConfig.Substring(0, 6)}` <:arrowJoin:740705934644609024> `{newVersion.CDNConfig.Substring(0, 6)}`" +
                                  $"\n**Product Config**:\n" +
                                  $"`{oldVersion.ProductConfig.Substring(0, 6)}` <:arrowJoin:740705934644609024> `{newVersion.ProductConfig.Substring(0, 6)}`",
                Timestamp       = DateTime.Now,
                // ThumbnailUrl    = "https://www.clipartmax.com/png/middle/307-3072138_world-of-warcraft-2-discord-emoji-world-of-warcraft-w.png"
            };

            // Send the message.
            Webhook.SendMessageAsync(embeds: new List<Embed>() { embed.Build() });
            MrGMHook?.SendMessageAsync(embeds: new List<Embed>() { embed.Build() });
        }
    }
}
