namespace application.model.IPO
{
    // Financial performance section
    public class Financials
    {
        public List<AnnualFinancial> AnnualResults { get; set; } = new();
        public decimal Debt { get; set; }
        public decimal Cash { get; set; }
    }
}
