using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class ChartData
    {
        public ChartData()
        {
            Series = new List<ChartSeriesItem>();
            Xaxis = new List<string>();
        }
        public string Title { get; set; }
        public List<string> Xaxis { get; set; }
        public List<ChartSeriesItem> Series { get; set; }
    }
}
