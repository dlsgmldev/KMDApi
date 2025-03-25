using KDMApi.Models.Crm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class IndividualExportVisit
    {
        public List<string> Headers { get; set; }
        public List<IndividualExportVisitItem> Items { get; set; }
        public PaginationInfo Info { get; set; }
    }
}
