namespace application.model.IPO
{
    // Represents the company going public
    public class Company
    {
        public string Name { get; set; }
        public string Industry { get; set; }
        public string BusinessModel { get; set; }
        public List<string> RevenueStreams { get; set; } = new();
        public ManagementTeam Management { get; set; }
        public List<string> Competitors { get; set; } = new();
    }
}
