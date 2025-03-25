using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class CrmDealTarget
    {
        public int Id { get; set; }
        public long Target { get; set; }
        public int PeriodId { get; set; }
        public int KpiId { get; set; }
        public int LinkedId { get; set; }           // Kalau type == "rm", User.Id dari RM tersebut
        public string Type { get; set; }            // "tribe", "segment", "branch", atau "rm"
        public string Status { get; set; }          // "Need approval", "Approved", "Rejected"
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? LastUpdated { get; set; }
        public int LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public int DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }

    }
}
