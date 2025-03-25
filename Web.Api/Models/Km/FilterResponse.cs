using KDMApi.Models.Crm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Km
{
    public class FilterResponse
    {
        public List<FilterProjectInfo> projects { get; set; }
        public PaginationInfo info { get; set; }
    }
}
