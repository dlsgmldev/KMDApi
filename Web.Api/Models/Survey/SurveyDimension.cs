using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Survey
{
    public class SurveyDimension
    {
        public SurveyDimension()
        {
            Dimensions = new List<SurveyDimension>();
        }
        public int Id { get; set; }
        public string Title { get; set; }
        public double Score { get; set; }
        public string Description { get; set; }
        public List<IndicatorValue> Indicators { get; set; }            // will be a list with 0 element when having subdimensions
        public List<SurveyDimension> Dimensions { get; set; }     // Subdimension of this dimension
    }
}
