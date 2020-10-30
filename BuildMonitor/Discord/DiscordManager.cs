using BuildMonitor.Util;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace BuildMonitor.Discord
{
    using SettingsDictionary = Dictionary<ulong, DiscordGuild>;

    public static class DiscordManager
    {
        public static DiscordBot Bot;
        public static SettingsDictionary DiscordGuildSettings = new SettingsDictionary();

        /// <summary>
        /// Initialize the <see cref="DiscordManager"/>
        /// </summary>
        public static void Initialize()
        {
            if (!File.Exists("token.txt"))
                throw new Exception("token.txt does not exist!!!");

            new Thread(() =>
            {
                Bot = new DiscordBot(new CommandService(), new DiscordSocketClient());
                Bot.Start(File.ReadAllText("token.txt"));
            }).Start();

            // Add default settings
            if (!File.Exists("settings.json"))
                Save();

            // Load the settings.
            DiscordGuildSettings = JsonConvert.DeserializeObject<SettingsDictionary>(File.ReadAllText("settings.json"));
        }

        /// <summary>
        /// Get the <see cref="DiscordGuild"/> settings for <see cref="SocketGuild"/>
        /// </summary>
        public static DiscordGuild GetDiscordSettings(ulong guildId)
        {
            if (!DiscordGuildSettings.TryGetValue(guildId, out var settings))
                return null;

            return settings;
        }

        /// <summary>
        /// Add <see cref="ulong"/> and <see cref="DiscordGuild"/> to <see cref="SettingsDictionary"/>
        /// </summary>
        public static void AddDefaultSettings(ulong guildId, DiscordGuild guild)
        {
            // Add the data
            DiscordGuildSettings.Add(guildId, guild);

            // Save data
            Save();
        }

        /// <summary>
        /// Set the current <see cref="DiscordGuild"/> for the given <see cref="ulong"/> guildId
        /// </summary>
        public static void SetSettings(ulong guildId, DiscordGuild guild)
        {
            // Update the settings and save
            DiscordGuildSettings[guildId] = guild;
            Save();
        }

        /// <summary>
        /// Send a message via <see cref="DiscordSocketClient"/> with the supplied channelId and message
        /// </summary>
        public static void SendBuildMonitorMessage(string product, Versions oldVersion, Versions newVersion)
        {
            foreach (var guild in DiscordGuildSettings)
            {
                var guildChannel = Bot.GetClient().GetGuild(guild.Key);
                if (guildChannel == null)
                    continue;

                var channel = guildChannel.GetChannel(guild.Value.BuildMonitorChannelId) as ISocketMessageChannel;
                if (channel == null)
                    continue;

                var isEncrypted = product.IsEncrypted();
                var embed = new EmbedBuilder
                {
                    Title       = $"New Build for **{product.GetProduct()}** (`{product}`)",
                    Description = $"⚠️ There is a new build for `{product}`! **{oldVersion.VersionsName}** ▶️ **{newVersion.VersionsName}**\n" +
                                  $"{(isEncrypted ? "\n⛔ This build is **NOT** datamineable\n" : "")}" +
                                  $"\n**Build Config**:\n" +
                                  $"`{oldVersion.BuildConfig.Substring(0, 6)}` ▶️ `{newVersion.BuildConfig.Substring(0, 6)}`" +
                                  $"\n**CDN Config**:\n" +
                                  $"`{oldVersion.CDNConfig.Substring(0, 6)}` ▶️ `{newVersion.CDNConfig.Substring(0, 6)}`" +
                                  $"\n**Product Config**:\n" +
                                  $"`{oldVersion.ProductConfig.Substring(0, 6)}` ▶️ `{newVersion.ProductConfig.Substring(0, 6)}`",
                    Timestamp   = DateTime.Now,
                    // ThumbnailUrl    = "https://www.clipartmax.com/png/middle/307-3072138_world-of-warcraft-2-discord-emoji-world-of-warcraft-w.png"
                };

                channel.SendMessageAsync(embed: embed.Build());
            }
        }

        /// <summary>
        /// Send a <see cref="File"/> over the network.
        /// </summary>
        public static void SendFile(string filename, string message)
        {
            if (!File.Exists(filename))
                return;

            foreach (var guild in DiscordGuildSettings)
            {
                if (!guild.Value.IsDebugServer)
                    continue;

                var guildChannel = Bot.GetClient().GetGuild(guild.Key);
                if (guildChannel == null)
                    continue;

                var channel = guildChannel.GetChannel(guild.Value.BuildMonitorChannelId) as ISocketMessageChannel;
                if (channel == null)
                    continue;

                channel.SendFileAsync(filename, message);
            }
        }

        /// <summary>
        /// Send a debug message to the Debug Server.
        ///</summary>
        public static void SendDebugMessage(string message)
        {
            foreach (var guild in DiscordGuildSettings)
            {
                if (!guild.Value.IsDebugServer)
                    continue;

                var guildChannel = Bot.GetClient().GetGuild(guild.Key);
                if (guildChannel == null)
                    continue;

                var channel = guildChannel.GetChannel(guild.Value.BuildMonitorChannelId) as ISocketMessageChannel;
                if (channel == null)
                    continue;

                channel.SendMessageAsync(message);
            }
        }

        private static void Save() =>
            File.WriteAllText("settings.json", JsonConvert.SerializeObject(DiscordGuildSettings, Formatting.Indented));
    }
}
