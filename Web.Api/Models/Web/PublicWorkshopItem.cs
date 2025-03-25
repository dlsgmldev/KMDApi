using KDMApi.Models.Km;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Web
{
    public class PublicWorkshopItem
    {
        public int WorkshopId { get; set; }
        public int EventId { get; set; }
        public string Title { get; set; }
        public int CategoryId { get; set; }
        public bool MgrUp { get; set; }
        public bool Mgr { get; set; }
        public bool Spv { get; set; }
        public bool Tl { get; set; }
        public bool Staff { get; set; }
        public List<List<GenericInfo>> Dates { get; set; }
    }
}
