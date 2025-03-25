using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class MonthStageInfo
    {
        public int Stage { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public string Text { get; set; }
    }
}
