using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Survey
{
    public class SurveyChartData
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<IndicatorValueArray> Data { get; set; }
        public List<string> Legend { get; set; }     
    }
}
