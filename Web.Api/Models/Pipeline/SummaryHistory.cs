using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class SummaryHistory
    {
        public string Title { get; set; }
        public long Amount { get; set; }
        public int Percent { get; set; }
        public string Note { get; set; }
    }
}
