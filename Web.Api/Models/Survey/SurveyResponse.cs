using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Survey
{
    public class SurveyResponse
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public int RatingId { get; set; }
        public string AnswerText { get; set; }
    }
}
