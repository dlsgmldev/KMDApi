using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Km
{
    public class AddParticipantRequest
    {
        public int ProjectId { get; set; }
        public int UserId { get; set; }
        public List<WorkshopParticipant> Add { get; set; }
    }
}
