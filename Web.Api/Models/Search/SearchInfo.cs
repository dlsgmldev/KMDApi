using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Search
{
    public class SearchInfo
    {
        public int Page { get; set; }
        public int PerPage { get; set; }
        public long Total { get; set; }
        public SearchInfo(int p, int pp, long t)
        {
            Page = p;
            PerPage = pp;
            Total = t;
        }
    }
}
