using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Crm
{
    public class ClientListRequest
    {
        public List<int> filterIndustries { get; set; }
        public List<int> filterSegments { get; set; }
        public List<int> filterRelManagers { get; set; }

        public int page { get; set; }
        public int countPerPage { get; set; }

        public string search { get; set; }
    }
}
