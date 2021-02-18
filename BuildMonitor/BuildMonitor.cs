using System.IO;
using System.Threading;
using Ribbit.Constants;
using Ribbit.Protocol;
using BuildMonitor.Discord;

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

            if (!Directory.Exists("listfiles"))
                Directory.CreateDirectory("listfiles");

            using (var client = new Client(Region.US))
            {
                Ribbit.InitRibbit(client);
                DiscordManager.RibbitClient = client;

                var monitorThread = new Thread(() => 
                {
                    while (isMonitoring)
                    {
                        Thread.Sleep(50000);

                        Ribbit.CheckForVersions(client);
                    }
                });
                monitorThread.Start();

                //while (true)
                //{
                //    Console.Write("> ");
                //    var command = Console.ReadLine();
                //}
            }
        }
    }
}
