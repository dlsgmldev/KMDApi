using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class ChartSeriesItem
    {
        public int Id { get; set; }
        public long Target { get; set; }
        public long Actual { get; set; }
        public int Month { get; set; }
        public long Achievement { get; set; }


    }
}
