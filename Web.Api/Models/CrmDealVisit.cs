using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class CrmDealVisit
    {
        public int Id { get; set; }
        public int DealId { get; set; }
        public int ClientId { get; set; }
        public DateTime VisitFromTime { get; set; }
        public DateTime VisitToTime { get; set; }
        public int PeriodId { get; set; }
        public string Location { get; set; }
        public string Objective { get; set; }
        public string NextStep { get; set; }
        public string Remark { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? LastUpdated { get; set; }
        public int LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public int DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }

    }
}
