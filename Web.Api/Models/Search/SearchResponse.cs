using KDMApi.Models.Km;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Search
{
    /*    public class SearchResponse
        {
            public List<ElasticEntry> Entries { get; set; }
            public SearchInfo Info { get; set; }
        }*/
        public class SearchResponse
        {
            public List<ViewSearchItem> Items { get; set; }
            public SearchInfo Info { get; set; }
        }

}
