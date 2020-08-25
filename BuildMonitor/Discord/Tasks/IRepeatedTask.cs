using Discord.WebSocket;
using System.Threading.Tasks;

namespace BuildMonitor.Discord.Tasks
{
    public interface IRepeatedTask
    {
        int Interval { get; }

        Task Run(DiscordSocketClient client);
    }
}
