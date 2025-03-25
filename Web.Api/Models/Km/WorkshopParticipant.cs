using KDMApi.Models.Crm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Km
{
    public class WorkshopParticipant
    {
        public int ClientId { get; set; }
        public List<ContactInfo> Participants { get; set; }
    }
}
