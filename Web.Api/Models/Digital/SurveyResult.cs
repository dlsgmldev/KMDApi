using KDMApi.Models.Survey;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Digital
{
    public class SurveyResult
    {
        public int Total { get; set; }
        public List<string> GroupName { get; set; }
        public DigitalQuadrantData Chart1 { get; set; }
        public DigitalBarChartData Chart2 { get; set; }
        public List<DigitalBarItem> Chart3 { get; set; }
        public List<DigitalBarItem> Chart4 { get; set; }
    }
}
