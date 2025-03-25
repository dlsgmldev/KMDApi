using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Survey
{
    public class ReportSummary
    {
        public ReportSummary()
        {
            Result = "";
            Score = 0.0d;
            Description = "";
            Values = new List<IndicatorValue>();
        }
        public string Result { get; set; }
        public Double Score { get; set; }
        public string Description { get; set; }
        public List<IndicatorValue> Values { get; set; }
    }
}
