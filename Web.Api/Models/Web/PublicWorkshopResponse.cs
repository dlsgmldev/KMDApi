using KDMApi.Models.Km;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Web
{
    public class PublicWorkshopResponse
    {
        public GenericInfo Branch { get; set; }
        public List<PublicWorkshopItem> Items { get; set; }
    }
}
