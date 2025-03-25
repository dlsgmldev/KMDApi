using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class SummaryTypeReport
    {
        public SummaryTypeReport()
        {
            Header = new SummaryHeaderReport();
        }
        public SummaryHeaderReport Header { get; set; }
        public List<SummaryReportRow> Items { get; set; }
    }
}
