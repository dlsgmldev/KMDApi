using KDMApi.Models.Km;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Survey
{
    public class SurveyGroupsData
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public List<GenericURL> Groups { get; set; }
    }
}
