using KDMApi.Models.Crm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class IndividualExportProposal
    {
        public List<string> Headers { get; set; }
        public List<IndividualExportProposalItem> Items { get; set; }
        public PaginationInfo Info { get; set; }
    }

    public class IndividualExportPICProposal
    {
        public List<string> Headers { get; set; }
        public List<IndividualExportPICProposalItem> Items { get; set; }
        public PaginationInfo Info { get; set; }
    }

}
