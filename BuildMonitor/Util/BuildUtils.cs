using System;

namespace BuildMonitor.Util
{
    public static class BuildUtils
    {
        public static string GetProduct(this string product)
        {
            return product switch 
            {
                "wow"               => "WoW Retail",
                "wowdev"            => "WoW Dev",
                "wowdemo"           => "WoW Demo",
                "wow_beta"          => "WoW Beta",
                "wowt"              => "WoW PTR",
                "wow_classic"       => "WoW Classic",
                "wow_classic_ptr"   => "WoW Classic PTR",
                "wow_classic_beta"  => "WoW Classic Beta",
                "wowv"              => "WoW Vendor",
                "wowv2"             => "WoW Vendor 2",
                "wowe1"             => "WoW Event 1",
                "wowe2"             => "WoW Event 2",
                "wowe3"             => "WoW Event 3",
                "wowz"              => "WoW Submission",
                _                   => "Unknown Product",
            };
        }

        public static bool IsEncrypted(this string product)
        {
            return product switch
            {
                "wowdev"    => true,
                "wowdemo"   => true,
                "wowv"      => true,
                "wowv2"     => true,
                _           => false
            };
        }
    }
}