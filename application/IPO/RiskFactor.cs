namespace application.model.IPO
{
    // Risk factor section
    public class RiskFactor
    {
        public string Category { get; set; } // e.g., "Market", "Operational", "Regulatory"
        public string Description { get; set; }
        public int SeverityLevel { get; set; } // scale 1-5
    }
}
