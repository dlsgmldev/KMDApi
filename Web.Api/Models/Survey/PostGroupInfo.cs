using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Survey
{
    public class PostGroupInfo
    {
        public int SurveyId { get; set; }
        public List<TextTime> Groups { get; set; }
    }
}
