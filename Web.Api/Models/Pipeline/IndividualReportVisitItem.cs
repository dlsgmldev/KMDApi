using KDMApi.Models.Km;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class IndividualReportVisitItem
    {
        public IndividualReportVisitItem()
        {
            Contacts = new List<GenericInfo>();
            Rms = new List<PercentTribeResponse>();
            Cons = new List<PercentTribeResponse>();
        }
        public int VisitId { get; set; }
        public int DealId { get; set; }
        public int CompanyId { get; set; }
        public string Company { get; set; }
        public DateTime VisitDate { get; set; }
        public string Location { get; set; }
        public string Objective { get; set; }
        public string NextStep { get; set; }
        public string Remarks { get; set; }
        public List<GenericInfo> Contacts { get; set; }
        public List<PercentTribeResponse> Rms { get; set; }
        public List<PercentTribeResponse> Cons { get; set; }
    }
}
