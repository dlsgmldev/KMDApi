using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class SummaryReportRow
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public long Amount1 { get; set; }
        public long Amount2 { get; set; }
        public long Amount3 { get; set; }
    }
}
