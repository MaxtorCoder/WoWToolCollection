using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading;

namespace AgentTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "phoenix-agent/1.0");

            var authResponse = client.GetStringAsync("http://127.0.0.1:1120/agent").Result;
            dynamic task = JObject.Parse(authResponse);
            client.DefaultRequestHeaders.Add("Authorization", (string)task.authorization);

            Install install = new Install
            {
                InstructiosDataSet = new string[] { "torrent", "win", "w3", "enus" },
                InstructionsPatch = "http://eu.patch.battle.net:1119/w3",
                InstructionProduct = "NGDP",
                MonitorPID = 13892,
                Priority = new Priority
                {
                    InsertAtHead = false,
                    Value = 900
                },
                UID = "w3"
            };
            
            InstallBeta installBeta = new InstallBeta
            {
                AccountCountry = "NLD",
                Finalized = true,
                GameDir = @"C:\\Program Files (x86)\\Warcraft 3",
                GeoIPCountry = "NL",
                Language = new string[] { "enUS" },
                SelectedAsset = "enUS",
                SelectedLocale = "enUS",
                Shortcut = "all",
                TomeTorrent = ""
            };
            
            Update update = new Update
            {
                Priority = new Priority
                {
                    InsertAtHead = false,
                    Value = 699
                },
                UID = "w3"
            };

            Uninstall uninstall = new Uninstall
            {
                RunCompaction = true,
                UID = "w3"
            };
            
            var Install     = new StringContent(JsonConvert.SerializeObject(install, Formatting.Indented));
            var InstallBeta = new StringContent(JsonConvert.SerializeObject(installBeta, Formatting.Indented));
            var Update      = new StringContent(JsonConvert.SerializeObject(update, Formatting.Indented));
            var Uninstall   = new StringContent(JsonConvert.SerializeObject(uninstall, Formatting.Indented));
            
            Console.WriteLine(client.PostAsync("http://127.0.0.1:1120/uninstall", Uninstall).Result);
            
            // while(true)
            // {
            //     Console.WriteLine(client.PostAsync("http://127.0.0.1:1120/install/w3", InstallBeta).Result);
            //     Console.WriteLine(client.PostAsync("http://127.0.0.1:1120/update", Update).Result);
            // 
            //     Thread.Sleep(2000);
            // }
        }
    }
}
