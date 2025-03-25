using System;
using System.Collections.Generic;
using System.Text;

namespace Web.Api.Core.Domain.Entities
{
    public class CrmRelManager
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SegmentId { get; set; }
        public int BranchId { get; set; }
        public int LeaderId { get; set; }           // Id of the leader in the same table
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
    }
}
