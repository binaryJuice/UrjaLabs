using System;
using System.Collections.Generic;

namespace application.model.IPO
{

    // Final report
    public class IpoAnalysisReport
    {
        public Company Company { get; set; }
        public IpoDetails IpoDetails { get; set; }
        public MarketOpportunity Market { get; set; }
        public Financials Financials { get; set; }
        public Governance Governance { get; set; }
        public List<RiskFactor> Risks { get; set; } = new();
        public Valuation Valuation { get; set; }
        public string StrengthsSummary { get; set; }
        public string ConclusionRecommendation { get; set; }
    }
}
