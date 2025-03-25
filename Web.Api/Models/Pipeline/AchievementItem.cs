using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class AchievementItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public long TargetNProposal { get; set; }
        public long TargetProposalValue { get; set; }
        public long TargetSalesVisit { get; set; }
        public long TargetSales { get; set; }
        public int ActualNProposal { get; set; }
        public long ActualProposalValue { get; set; }
        public int ActualSalesVisit { get; set; }
        public long ActualSales { get; set; }
        public double AchNProposal { get; set; }
        public double AchProposalValue { get; set; }
        public double AchSalesVisit { get; set; }
        public double AchSales { get; set; }
        public double AveAch { get; set; }
    }
}
