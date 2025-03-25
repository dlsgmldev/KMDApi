using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class ProposalDetail
    {
        public int No { get; set; }
        public string Company { get; set; }
        public string Tribe { get; set; }
        public string Segment { get; set; }
        public string RM { get; set; }
        public string DealName { get; set; }
        public long Amount { get; set; }
    }
}
