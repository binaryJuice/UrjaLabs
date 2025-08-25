namespace application.model.IPO
{
    // Governance and ownership
    public class Governance
    {
        public bool DualClassShares { get; set; }
        public List<Shareholder> MajorShareholders { get; set; } = new();
        public bool BoardIndependent { get; set; }
    }
}
