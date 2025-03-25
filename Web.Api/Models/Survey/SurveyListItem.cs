using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Survey
{
    public class SurveyListItem
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string Title { get; set; }
        public string Intro { get; set; }
        public string AddInfo { get; set; }
        public bool Grouping { get; set; }
    }
}
