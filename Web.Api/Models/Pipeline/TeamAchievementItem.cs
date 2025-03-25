using KDMApi.Models.Km;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class TeamAchievementItem
    {
        public GenericInfo User { get; set; }
        public GenericInfo Segment { get; set; }
        public string Authority { get; set; }
        public int NProposals { get; set; }
        public int Visits { get; set; }
        public long ProposalValue { get; set; }
        public long Sales { get; set; }
        public string Status { get; set; }
        public int FromMonth { get; set; }
        public int ToMonth { get; set; }
        public int Year { get; set; }
    }
}
