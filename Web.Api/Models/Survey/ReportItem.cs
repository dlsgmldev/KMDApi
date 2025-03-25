using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Survey
{
    public class ReportItem
    {
        public string Item { get; set; }
        public int RatingId { get; set; }
        public int Ranking { get; set; }
        public string AnswerText { get; set; }
        public string Answer { get; set; }
        public int Val { get; set; }
    }
}
