using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RibbitMonitor.Discord
{
    [JsonObject]
    public class WebHook
    {
        private readonly HttpClient httpClient;
        private readonly string webHookURL;

        [JsonProperty("content")]
        public string Content { get; set; }
        [JsonProperty("username")]
        public string Username { get; set; }
        [JsonProperty("avatar_url")]
        public string AvatarUrl { get; set; }
        [JsonProperty("tts")]
        public bool IsTTS { get; set; }
        [JsonProperty("embeds")]
        public List<Embed> Embeds { get; set; } = new List<Embed>();

        public WebHook(string webhookUrl)
        {
            httpClient = new HttpClient();
            webHookURL = webhookUrl;
        }

        public WebHook(ulong id, string token) : this($"https://discordapp.com/api/webhooks/{id}/{token}")
        {
        }

        public async Task<HttpResponseMessage> Send()
        {
            var content = new StringContent(JsonConvert.SerializeObject(this), Encoding.UTF8, "application/json");
            return await httpClient.PostAsync(webHookURL, content);
        }

        // ReSharper disable once InconsistentNaming
        public async Task<HttpResponseMessage> Send(string content, string username = null, string avatarUrl = null, bool isTTS = false, IEnumerable<Embed> embeds = null)
        {
            Content = content;
            Username = username;
            AvatarUrl = avatarUrl;
            IsTTS = isTTS;
            Embeds.Clear();
            if (embeds != null)
            {
                Embeds.AddRange(embeds);
            }

            return await Send();
        }
    }
}
