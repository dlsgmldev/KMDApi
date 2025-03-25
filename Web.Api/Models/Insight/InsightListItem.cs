using DocumentFormat.OpenXml.Office.CoverPageProps;
using KDMApi.Models.Km;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Insight
{
    public class InsightListItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public List<GenericInfo> Authors { get; set; }
        public List<GenericInfo> Categories { get; set; }
        public DateTime LastUpdate { get; set; }
        public bool Publish { get; set; }
        public string Website { get; set; }
    }
}
