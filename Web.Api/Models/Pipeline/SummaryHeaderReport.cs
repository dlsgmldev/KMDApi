using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class SummaryHeaderReport
    {
        public string Type { get; set; }
        public List<string> Headers { get; set; }
        public SummaryHeaderReport()
        {
            Headers = new List<string>();
        }
    }
}
