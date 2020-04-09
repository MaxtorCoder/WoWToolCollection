using System;
using System.Collections.Generic;
using System.Text;

namespace AgentTest
{
    public static class Endpoint
    {
        public static string GetTactFromAgent(string agentProd)
        {
            switch (agentProd)
            {
                // HotS
                case "heroes":
                    return "hero";
                case "heroes_tournament":
                    return "heroc";
                case "heroes_ptr":
                    return "herot";

                // Hearthstone
                case "hs_beta":
                    return "hsb";
                case "hs_tournament":
                    return "hsc";

                // Overwatch (this is a long one so be prepared)
                case "prometheus":
                    return "pro";
                case "prometheus_tournament":
                    return "proc";
                case "prometheus_tournament_cn":
                    return "proc_cn";
                case "prometheus_tournament_eu":
                    return "proc_eu";
                case "prometheus_tournament_kr":
                    return "proc_kr";
                case "prometheus_tournament2":
                    return "proc2";
                case "prometheus_tournament2_cn":
                    return "proc2_cn";
                case "prometheus_tournament2_eu":
                    return "proc2_eu";
                case "prometheus_tournament2_kr":
                    return "proc2_kr";
                case "prometheus_tournament3":
                    return "proc3";
                case "prometheus_tournament_viewer":
                    return "procr";
                case "prometheus_tournament_viewer_2":
                    return "procr2";
                case "prometheus_dev":
                    return "prodev";
                case "prometheus_test":
                    return "prot";
                case "prometheus_vendor":
                    return "prov";
                case "prometheus_viewer":
                    return "proms";

                // Starcraft 1
                case "s1":
                    return "s1";
                case "s1_ptr":
                    return "s1t";

                // Starcraft 2
                case "s2_eu":
                case "s2_cn":
                case "s2_kr":
                case "s2_xx":
                case "s2_us":
                    return "s2";

                // Warcraft 3
                case "w3":
                    return "w3";
                case "w3_ptr":
                    return "w3t";
                case "w3_beta":
                    return "w3b";

                // WoW
                case "wow_eu":
                case "wow_us":
                case "wow_cn":
                case "wow_kr":
                case "wow_sg":
                case "wow_xx":
                    return "wow";
                case "wow_beta":
                    return "wow_beta";
                case "wow_test":
                case "wow_classic":
                    return "wow_classic";
                case "wow_classic_beta":
                    return "wow_classic_beta";
                case "wow_event1":
                    return "wowe1";
                case "wow_event2":
                    return "wowe2";
                case "wow_event3_eu":
                case "wow_event3_us":
                case "wow_event3_cn":
                case "wow_event3_kr":
                case "wow_event3_sg":
                case "wow_event3_xx":
                    return "wowe3";
                case "wow_ptr_eu":
                case "wow_ptr_us":
                case "wow_ptr_cn":
                case "wow_ptr_kr":
                case "wow_ptr_sg":
                case "wow_ptr_xx":
                    return "wowt";
                case "wow_vendor":
                    return "wowv";
                case "wow_vendor2":
                    return "wowv2";
                case "wow_submission":
                    return "wowz";
                case "wow_alpha":
                    return "wowdev";

                default:
                    return null;
            }
        }
    }
}
