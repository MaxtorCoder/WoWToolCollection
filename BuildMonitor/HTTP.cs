using BuildMonitor.Discord;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace BuildMonitor
{
    public static class HTTP
    {
        private static string[] cdnUrls = { "http://level3.blizzard.com", "http://us.cdn.blizzard.com", "http://blzddist1-a.akamaihd.net" };

        /// <summary>
        /// Request a file from the CDN.
        /// </summary>
        public static async Task<MemoryStream> RequestCDN(string url)
        {
            var client = new HttpClient();

            try
            {
                foreach (var cdn in cdnUrls)
                {
                    var response = await client.GetAsync($"{cdn}/{url}");

                    if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted)
                    {
                        var memStream = await response.Content.ReadAsByteArrayAsync();
                        return new MemoryStream(memStream);
                    }

                    DiscordManager.SendDebugMessage($"Tried {cdn} with {url}");
                }

                return null;
            }
            catch (Exception ex)
            {
                DiscordManager.SendDebugMessage($"```{ex}```");
                return null;
            }
        }
    }
}
