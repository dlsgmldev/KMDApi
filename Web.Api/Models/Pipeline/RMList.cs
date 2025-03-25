using KDMApi.Models.Crm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class RMList
    {
        public List<RMListItem> Items { get; set; }
        public PaginationInfo Info { get; set; }
    }
}
