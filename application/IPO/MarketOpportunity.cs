namespace application.model.IPO
{
    // Market opportunity information
    public class MarketOpportunity
    {
        public decimal TotalAddressableMarket { get; set; } // in $ billions
        public double CAGR { get; set; } // growth rate
        public string OpportunitySummary { get; set; }
    }
}
