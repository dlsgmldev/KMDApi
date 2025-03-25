using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class OwnerRequest
    {
        public int DealId { get; set; }
        public int UseerId { get; set; }
        public int SegmentId { get; set; }
        public int BranchId { get; set; }
        public int PicId { get; set; }
        public List<int> Consultants { get; set; }
        public List<PercentInfo> Rms { get; set; }
        public List<PercentTribeInfo> Tribes { get; set; }
    }
}
