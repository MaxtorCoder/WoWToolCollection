using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
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
                InstructiosDataSet = new string[] { "torrent", "win", "wow_classic_beta", "enus" },
                InstructionsPatch = "http://us.patch.battle.net:1119/wow_classic_beta",
                InstructionProduct = "NGDP",
                MonitorPID = 12345,
                Priority = new Priority
                {
                    InsertAtHead = false,
                    Value = 900
                },
                UID = "wow_classic_beta"
            };
            
            InstallBeta installBeta = new InstallBeta
            {
                AccountCountry = "NLD",
                Finalized = true,
                GameDir = @"C:\\Program Files (x86)\\World of Warcraft\\World of Warcraft Public Test\\",
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
                UID = "wow_classic_beta"
            };
            
            Console.WriteLine(client.PostAsync("http://127.0.0.1:1120/install", new StringContent(JsonConvert.SerializeObject(install))).Result);
            
            while(true)
            {
                Console.WriteLine(client.PostAsync("http://127.0.0.1:1120/install/wow_classic_beta", new StringContent(JsonConvert.SerializeObject(installBeta))).Result);
                Console.WriteLine(client.PostAsync("http://127.0.0.1:1120/update", new StringContent(JsonConvert.SerializeObject(update))).Result);
            
                Thread.Sleep(2000);
            }
        }
    }
}
