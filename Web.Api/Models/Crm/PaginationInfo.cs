using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Crm
{
    public class PaginationInfo
    {
        public int page { get; set; }
        public int perPage { get; set; }
        public int total { get; set; }

        public PaginationInfo(int p, int pp, int t)
        {
            page = p;
            perPage = pp;
            total = t;
        }
    }
}
