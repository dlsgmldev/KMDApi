using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class SalesVisitRequest
    {
        public int Id { get; set; }         // Id dari CrmDealVisit
        public int DealId { get; set; }
        public int UserId { get; set; }
        public int ClientId { get; set; }
        public string VisitDate { get; set; }       
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public List<int> Contacts { get; set; }
        public List<int> Rms { get; set; }
        public List<int> Consultants { get; set; }
        public List<int> Tribes { get; set; }
        public string Location { get; set; }
        public string Objective { get; set; }
        public string NextStep { get; set; }
        public string Remark { get; set; }
    }
}
