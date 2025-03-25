using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Km
{
    public class InsightIntro
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Extract { get; set; }
        public List<GenericInfo> Categories { get; set; }
        public List<GenericInfo> Authors { get; set; }
        public string Thumbnail { get; set; }
        public string Slug { get; set; }
    }
}
