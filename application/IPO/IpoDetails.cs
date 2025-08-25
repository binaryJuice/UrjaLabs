namespace application.model.IPO
{
    // IPO details (from prospectus)
    public class IpoDetails
    {
        public decimal PriceRangeLow { get; set; }
        public decimal PriceRangeHigh { get; set; }
        public int SharesOffered { get; set; }
        public decimal ExpectedMarketCap { get; set; }
        public List<string> Underwriters { get; set; } = new();
        public DateTime ExpectedDate { get; set; }
        public string UseOfProceeds { get; set; }
    }
}
