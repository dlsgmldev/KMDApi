using KDMApi.Models.Crm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class IndividualReportProposal
    {
        public List<IndividualReportProposalItem> Items { get; set; }
        public PaginationInfo Info { get; set; }
    }

    public class IndividualReportPicProposal
    {
        public List<IndividualReportPICProposalItem> Items { get; set; }
        public PaginationInfo Info { get; set; }
    }

}
