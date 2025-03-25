using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Survey
{
    public class SurveyReport
    {
        public int SurveyId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime SurveyDate { get; set; }
        public List<IndicatorText> Cover { get; set; }
        public List<SurveyDimension> Dimensions { get; set; }
        public SurveyDimension Summary { get; set; }
        // Yang berikut ini untuk HRBP
        public List<SurveyChartTableData> data { get; set; }
        public QuadrantData quadrant { get; set; }

    }
}
