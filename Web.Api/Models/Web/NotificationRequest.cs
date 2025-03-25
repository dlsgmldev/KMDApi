using KDMApi.Models.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Web
{
    public class NotificationRequest
    {
        public int EventId { get; set; }
        public string EmailSubject { get; set; }                
        public string Email { get; set; }                       
        public List<EmailAddress> Recipients { get; set; }      
    }
}
