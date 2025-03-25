using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class CrmDealInternalMember
    {
        public int Id { get; set; }
        public int DealId { get; set; }
        public int RoleId { get; set; }
        public int UserId { get; set; }
        public double Percentage { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? LastUpdated { get; set; }
        public int LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public int DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
        public long Nominal { get; set; }
        public bool UsePercent { get; set; }

    }
}
