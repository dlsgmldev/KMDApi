using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class RMInfo
    {
        public RMInfo()
        {
            Nominal = 0;
            UsePercent = true;
        }
        public int Id { get; set; }         // CrmRelManager.Id
        public int UserId { get; set; }
        public int SegmentId { get; set; }
        public int BranchId { get; set; }
        public int LeaderId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public double Percentage { get; set; }
        public bool UsePercent { get; set; }
        public long Nominal { get; set; }
    }
}
