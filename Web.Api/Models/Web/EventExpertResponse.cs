using KDMApi.Models.Crm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Web
{
    public class EventExpertResponse
    {
        public List<EventExpertItem> Items { get; set; }
        public PaginationInfo Info { get; set; }
    }
}
