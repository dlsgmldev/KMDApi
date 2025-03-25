using KDMApi.Models.Crm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Survey
{
    public class DxIndividualReportItem
    {
        public string Name { get; set; }
        public string Company { get; set; }
        public DateTime SurveyDate { get; set; }
        public string Uuid { get; set; }
    }

    public class DxIndividualReportList
    {
        public List<DxIndividualReportItem> Items { get; set; }
        public PaginationInfo Info { get; set; }
    }
}
