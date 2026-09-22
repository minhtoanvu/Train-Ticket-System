namespace TrainTicket.Data.Helpers
{
    public static class RegionHelper
    {
        public const string HQ = "HQ";
        public const string North = "North";
        public const string Central = "Central";
        public const string South = "South";

        public static string GetRegionName(string code)
        {
            return code switch
            {
                "North" => "Miền Bắc",
                "Central" => "Miền Trung",
                "South" => "Miền Nam",
                _ => "Trụ sở chính"
            };
        }
    }
}