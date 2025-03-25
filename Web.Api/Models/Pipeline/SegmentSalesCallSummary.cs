using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class SegmentSalesCallSummary
    {
        public int No { get; set; }
        public int Amount { get; set; }
        public string Segment { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
    }
}
