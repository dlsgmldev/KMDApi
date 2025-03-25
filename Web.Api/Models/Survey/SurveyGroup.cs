using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Survey
{
    public class SurveyGroup
    {
        public int SurveyId { get; set; }
        public string Group { get; set; }
        public List<SurveyPage> Pages { get; set; }
    }
}
