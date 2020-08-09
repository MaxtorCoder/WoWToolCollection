using Ribbit.Parsing;
using Ribbit.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace BuildMonitor
{
    public static class Ribbit
    {
        /// <summary>
        /// Parse the "v1/summary" response.
        /// </summary>
        public static Dictionary<(string Product, string Type), int> ParseSummary(string summary)
        {
            var summaryDictionary = new Dictionary<(string, string), int>();
            var parsedFile = new BPSV(summary);

            foreach (var entry in parsedFile.data)
            {
                if (string.IsNullOrEmpty(entry[2]))
                    summaryDictionary.Add((entry[0], "version"), int.Parse(entry[1]));
                else
                    summaryDictionary.Add((entry[0], entry[2].Trim()), int.Parse(entry[1]));
            }

            return summaryDictionary;
        }
    }
}
