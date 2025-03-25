using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class WebEventRegParticipant
    {
        public int Id { get; set; }
        public int RegistrationId { get; set; }
        public string Participant { get; set; }
        public string JobTitle { get; set; }
        public string Department { get; set; }
        public string Handphone { get; set; }
        public string Email { get; set; }
        public String Gender { get; set; }
    }
}
