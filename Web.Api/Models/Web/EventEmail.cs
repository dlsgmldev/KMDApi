using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Web
{
    public class EventEmail
    {
        public List<WebEventParticipant> Alumni { get; set; }
        public string EmailSubject { get; set; }
        public string Email { get; set; }
    }
}
