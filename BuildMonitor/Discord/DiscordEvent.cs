using BuildMonitor.Discord.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace BuildMonitor.Discord
{
    public partial class DiscordBot
    {
        /// <summary>
        /// <see cref="DiscordSocketClient.Log"/> Event handler.
        /// </summary>
        private Task DiscordLog(LogMessage arg)
        {
            Console.WriteLine($"[BOT]: {arg.Message}");

            return Task.CompletedTask;
        }

        /// <summary>
        /// <see cref="DiscordSocketClient.Ready"/> Event handler.
        /// </summary>
        private async Task DiscordReady()
        {
            Console.WriteLine($"[BOT]: {client.CurrentUser} connected to server");

            foreach (var guild in client.Guilds)
            {
                if (DiscordManager.DiscordGuildSettings.ContainsKey(guild.Id))
                    continue;

                DiscordManager.AddDefaultSettings(guild.Id, new DiscordGuild
                {
                    ServerName              = guild.Name,
                    IsDebugServer           = guild.Id == 454740967501856774,
                    BuildMonitorChannelId   = guild.Id == 454740967501856774 ? 720872060225716265 : 0UL,
                    BotPrefix               = '!'
                });
            }

            // Get all Tasks and execute them.
            TaskManager.GetAllTasks();
            TaskManager.RunAllTasks();
        }

        /// <summary>
        /// <see cref="DiscordSocketClient.MessageReceived"/> Event handler.
        /// </summary>
        private async Task DiscordMessageReceived(SocketMessage message)
        {
            if (message.Author.Id == client.CurrentUser.Id)
                return;

            var channel = message.Channel as SocketGuildChannel;
            if (channel == null)
                return;

            var guildSettings = DiscordManager.GetDiscordSettings(channel.Guild.Id);
            if (guildSettings == null)
                return;

            var userMessage = message as SocketUserMessage;
            if (userMessage == null)
                return;

            var argPos = 0;
            if (!(userMessage.HasCharPrefix(guildSettings.BotPrefix, ref argPos) || userMessage.HasMentionPrefix(client.CurrentUser, ref argPos)) ||
                userMessage.Author.IsBot)
                return;

            var context = new SocketCommandContext(client, userMessage);
            await commands.ExecuteAsync(context, argPos, null);
        }

        /// <summary>
        /// <see cref="DiscordSocketClient.JoinedGuild"/> Event handler.
        /// </summary>
        private Task DiscordJoined(SocketGuild guild)
        {
            Console.WriteLine($"[BOT]: {client.CurrentUser} joined a new guild! {guild.Name} ({guild.Id})");

            DiscordManager.AddDefaultSettings(guild.Id, new DiscordGuild
            {
                ServerName              = guild.Name,
                IsDebugServer           = false,
                BuildMonitorChannelId   = 0,
                BotPrefix               = '!'
            });

            return Task.CompletedTask;
        }
    }
}
