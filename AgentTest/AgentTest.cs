using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace AgentTest
{
    class Program
    {
        #region Old Code
        // static void Main(string[] args)
        // {
        // Console.Write("Enter product Id: ");
        // var product = Console.ReadLine();

        // Console.Write("Enter installation path: ");
        // var path = Console.ReadLine();

        // var client = new HttpClient();
        // client.DefaultRequestHeaders.Add("User-Agent", "phoenix-agent/1.0");
        // 
        // var authResponse = client.GetStringAsync("http://127.0.0.1:1120/agent").Result;
        // dynamic task = JObject.Parse(authResponse);
        // client.DefaultRequestHeaders.Add("Authorization", (string)task.authorization);
        // 
        // var result = client.GetAsync("http://127.0.0.1:1120/game/wow_alpha");

        // var install = new Install
        // {
        //     InstructiosDataSet = new string[] { "torrent", "win", product, "" },
        //     InstructionsPatch = $"http://us.patch.battle.net:1119/{Endpoint.GetTactFromAgent(product)}",
        //     InstructionProduct = "NGDP",
        //     MonitorPID = 9692,
        //     Priority = new Priority
        //     {
        //         InsertAtHead = false,
        //         Value = 900
        //     },
        //     UID = product
        // };

        // var installBeta = new InstallBeta
        // {
        //     AccountCountry = "NLD",
        //     Finalized = true,
        //     GameDir = path.Replace("/", "\\"),
        //     GeoIPCountry = "NL",
        //     Language = new string[] { "enUS" },
        //     SelectedAsset = "enUS",
        //     SelectedLocale = "enUS",
        //     Shortcut = "all",
        //     TomeTorrent = ""
        // };

        // var update = new Update
        // {
        //     Priority = new Priority
        //     {
        //         InsertAtHead = false,
        //         Value = 699
        //     },
        //     UID = product
        // };

        //Console.WriteLine($"Installing {product}..");
        //client.PostAsync("http://127.0.0.1:1120/install", new StringContent(JsonConvert.SerializeObject(install)));

        // while(true)
        // {
        //     // Installing..
        //     var result = client.PostAsync($"http://127.0.0.1:1120/install/{product}", new StringContent(JsonConvert.SerializeObject(installBeta))).Result;
        //     if (result.StatusCode == HttpStatusCode.OK || result.StatusCode == HttpStatusCode.Created)
        //     {
        //         client.PostAsync("http://127.0.0.1:1120/update", new StringContent(JsonConvert.SerializeObject(update)));
        //         var updateResult = client.GetAsync($"http://127.0.0.1:1120/update/{product}");
        //         // var json = JsonConvert.DeserializeObject<UpdateResponse>(updateResult.ToString());
        //         // 
        //         // if (json.progress >= 100)
        //         // {
        //         //     Console.WriteLine("Download has completed! Have fun!");
        //         //     Console.WriteLine("Program closing in 10 seconds..");
        //         //     Thread.Sleep(TimeSpan.FromSeconds(10));
        //         //     return;
        //         // }
        //     }

        //     var consoleColor = result.StatusCode == HttpStatusCode.OK || result.StatusCode == HttpStatusCode.Created ? ConsoleColor.Green : ConsoleColor.Red;
        //     Console.ForegroundColor = consoleColor;
        //     Console.WriteLine($"Response: {result.StatusCode}");
        //     Thread.Sleep(2000);
        // }
        // }
        #endregion


        static void Main(string[] args)
        {
            var processes = Process.GetProcessesByName("Agent");
            var agentProcess = processes[0];

            var commandLines = agentProcess.GetCommandLine().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            commandLines.RemoveAt(0);

            commandLines.Add("--show");
            commandLines.Add("--allowversionrefresh");
            commandLines.Add("--allowrestart");
            commandLines.Add("--tracelevel=4");
            commandLines.Add("--readabledatabase");
            commandLines.Add("--nohttpauth");

            agentProcess.Kill();
            var newProcess = new Process();
            newProcess.StartInfo = new ProcessStartInfo(@"C:\ProgramData\Battle.net\Agent\Agent.6926\Agent.exe", string.Join(' ', commandLines));
            newProcess.Start();
        }
    }

    static class Extensions
    {
        public static string GetCommandLine(this Process process)
        {
            using (var searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id))
            using (var objects = searcher.Get())
            {
                return objects.Cast<ManagementBaseObject>().SingleOrDefault()?["CommandLine"]?.ToString();
            }
        }
    }
}
