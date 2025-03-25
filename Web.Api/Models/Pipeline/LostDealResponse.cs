using KDMApi.Models.Crm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class LostDealResponse
    {
        public List<LostDealItem> items { get; set; }
        public PaginationInfo info { get; set; }
    }
}
