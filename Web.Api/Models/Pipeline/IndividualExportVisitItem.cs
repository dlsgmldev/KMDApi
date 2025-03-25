using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class IndividualExportVisitItem
    {
        public int No { get; set; }
        public int VisitId { get; set; }
        public int DealId { get; set; }
        public int ClientId { get; set; }
        public string Company { get; set; }
        public DateTime VisitDate { get; set; }
        public string Location { get; set; }
        public string Objective { get; set; }
        public string NextStep { get; set; }
        public string Remarks { get; set; }
    }
}
