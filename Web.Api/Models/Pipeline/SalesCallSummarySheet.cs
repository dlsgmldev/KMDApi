using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class SalesCallSummarySheet
    {
        public List<string> Headers { get; set; }
        public List<SegmentSalesCallSummary> Items { get; set; }
    }
}
