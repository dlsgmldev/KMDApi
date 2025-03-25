using KDMApi.Models.Km;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class VisitInfo
    {
        public VisitInfo()
        {
            Rms = new List<GenericInfo>();
            Tribes = new List<GenericInfo>();
        }
        public int VisitId { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; }
        public string Location { get; set; }
        public List<GenericInfo> Rms { get; set; }
        public List<GenericInfo> Contacts { get; set; }
        public List<GenericInfo> Consultants { get; set; }
        public List<GenericInfo> Tribes { get; set; }
        public string NextStep { get; set; }
        public string Objective { get; set; }
        public string Remark { get; set; }
        public DateTime FromTime { get; set; }
        public DateTime ToTime { get; set; }
    }
}
