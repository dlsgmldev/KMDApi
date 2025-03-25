using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class CrmDeal
    {

         public int Id { get; set; }
        public string Name { get; set; }
        public DateTime DealDate { get; set; }
        public int Probability { get; set; }
        public int ClientId { get; set; }
         public int StageId { get; set; }
         public int SegmentId { get; set; }
         public int BranchId { get; set; }
         public int StateId { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? LastUpdated { get; set; }
        public int LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public int DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }

    }
}
