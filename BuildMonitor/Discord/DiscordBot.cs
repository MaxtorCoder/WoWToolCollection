using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Reflection;
using System.Threading;

namespace BuildMonitor.Discord
{
    public partial class DiscordBot
    {
        private DiscordSocketClient client;
        private CommandService commands;

        public DiscordBot(CommandService commands, DiscordSocketClient client)
        {
            this.commands = commands;
            this.client = client;

            // Add Event handlers.
            client.Ready            += DiscordReady;
            client.MessageReceived  += DiscordMessageReceived;
            client.Log              += DiscordLog;
            client.JoinedGuild      += DiscordJoined;
        }

        /// <summary>
        /// Start the <see cref="DiscordSocketClient"/>
        /// </summary>
        public void Start(string token)
        {
            // Login the bot
            client.LoginAsync(TokenType.Bot, token);
            client.StartAsync();

            // Set some stats
            client.SetGameAsync("World of Warcraft");
            client.SetStatusAsync(UserStatus.DoNotDisturb);

            // Hold the thread until the client is connected.
            while (client.ConnectionState == ConnectionState.Connecting) { }

            commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);

            Thread.Sleep(Timeout.Infinite);
        }

        /// <summary>
        /// Get the current <see cref="DiscordSocketClient"/>
        /// </summary>
        public DiscordSocketClient GetClient() => client;
    }
}
