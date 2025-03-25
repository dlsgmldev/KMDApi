using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class CrmRelManager
    {
        public int Id { get; set; }
        public string JobTitle { get; set; }
        public int UserId { get; set; }
        public int SegmentId { get; set; }
        public int BranchId { get; set; }
        public int PlatformId { get; set; }
        public int LeaderId { get; set; }           // User.ID of the leader
        public bool IsTeamLeader { get; set; }
        public string TeamName { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime LastUpdated { get; set; }
        public int LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public int DeletedBy { get; set; }
        public DateTime DeletedDate { get; set; }
        public bool isActive { get; set; }
        public int DeactivatedBy { get; set; }
        public DateTime DeactivatedDate { get; set; }
            
       /* public virtual ICollection<CrmClientRelManager> CrmClientRelManagers { get; set; }
*/
    }
}
