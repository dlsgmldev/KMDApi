using KDMApi.Models.Digital;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Survey
{
    public class EdtraReport
    {
        public int Id { get; set; }
        public string SurveyName { get; set; }
        public string Uuid { get; set; }
        public string GroupName { get; set; }
        public int Total { get; set; }
        public DateTime ReportDate { get; set; }
        public double EngagementIndex1 { get; set; }
        public double EngagementIndex2 { get; set; }
        public List<IndicatorValue> Quadrants { get; set; }
        public List<DigitalBarChartData> Charts { get; set; }
        public SurveyResult Digital { get; set; }
    }
}
