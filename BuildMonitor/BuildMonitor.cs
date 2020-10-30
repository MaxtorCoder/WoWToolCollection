using System.IO;
using Ribbit.Constants;
using Ribbit.Protocol;
using System.Threading;
using BuildMonitor.Discord;
using System.Threading.Tasks;
using System;

namespace BuildMonitor
{
    public class BuildMonitor
    {
        private static readonly bool isMonitoring = true;

        static void Main(string[] args)
        {
            // Initialize Discord
            DiscordManager.Initialize();

            if (!Directory.Exists("cache"))
                Directory.CreateDirectory("cache");

            using (var client = new Client(Region.US))
            {
                Ribbit.InitRibbit(client);

                Task.Run(() =>
                {
                    while (isMonitoring)
                    {
                        Thread.Sleep(50000);

                        Ribbit.CheckForVersions(client);
                    }
                });

                while (true)
                {
                    Console.Write("> ");
                    var command = Console.ReadLine();
                }
            }
        }
    }
}
