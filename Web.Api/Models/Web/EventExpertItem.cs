using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Web
{
    public class EventExpertItem
    {
        public int Id { get; set; }     // WebEventExpert.Id
        public int EventId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }
}
