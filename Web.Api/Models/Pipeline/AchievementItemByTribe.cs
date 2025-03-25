using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class AchievementItemByTribe
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Tribe { get; set; }
        public int ActualNProposal { get; set; }
        public long ActualProposalValue { get; set; }
        public int ActualSalesVisit { get; set; }
    }
}
