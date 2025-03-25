using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class ProposalDetailSheet
    {
        public List<string> Headers { get; set; }
        public List<ProposalDetail> Items { get; set; }
    }
}
