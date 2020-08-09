namespace Ribbit.Constants
{
    public enum Region
    {
        EU, US, KR, CN, TW, SG, XX, Custom
    }

    internal static class RegionExtensions
    {
        public static string GetName(this Region region)
        {
            switch (region)
            {
                case Region.EU: return "eu";
                case Region.US: return "us";
                case Region.KR: return "kr";
                case Region.CN: return "cn";
                case Region.TW: return "tw";
                case Region.SG: return "sg";
                case Region.XX: return "xx";
                case Region.Custom: return "127.0.0.1";
                default: throw new UnknownRegionException(region);
            }
        }

        public static string GetHostname(this Region region)
        {
            var hostname = "";

            if (region == Region.Custom)
                hostname = region.GetName();
            else
                hostname = region.GetName() + ".version.battle.net";

            return hostname;
        }
    }

    public class UnknownRegionException : System.Exception
    {
        public Region region;

        public UnknownRegionException(Region region) : base("Unknown region provided")
        {
            this.region = region;
        }
    }
}
