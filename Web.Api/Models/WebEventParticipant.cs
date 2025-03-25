using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class WebEventParticipant
    {
        public int EventId { get; set; }
        public int RoleId { get; set; }
        public int ContactId { get; set; }
    }
}
