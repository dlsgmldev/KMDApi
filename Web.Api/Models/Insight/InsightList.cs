using KDMApi.Models.Crm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Insight
{
    public class InsightList
    {
        public List<InsightListItem> items { get; set; }
        public PaginationInfo Info { get; set; }
        public int TotalAllInsightsPublished { get; set; }
        public int TotalAllInsightsPublishedGML { get; set; }
        public int TotalAllInsightsPublishedCDHX { get; set; }
    }
}
