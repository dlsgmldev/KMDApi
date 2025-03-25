using KDMApi.Models.Crm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Km
{
    public class WorkshopParticipantWithClient
    {
        public WorkshopParticipantWithClient()
        {
            Client = new GenericInfo();
            Participants = new List<ContactInfo>();
        }
        public GenericInfo Client { get; set; }
        public List<ContactInfo> Participants { get; set; }
    }
}
