using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Survey
{
    public class PostSurveyResponse
    {
        public int SurveyId { get; set; }
        public string Uuid { get; set; }                            // Group UUID
        public List<SurveyResponse> responses { get; set; }
    }
}
