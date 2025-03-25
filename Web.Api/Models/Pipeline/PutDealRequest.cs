using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class PutDealRequest
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ClientId { get; set; }
        public int ContactId { get; set; }
        public int PicId { get; set; }
        public DateTime DealDate { get; set; }
        public int SegmentId { get; set; }
        public int BranchId { get; set; }
        public string Name { get; set; }
        public int StageId { get; set; }
        public int Probability { get; set; }
    }
}
