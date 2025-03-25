using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class WebEventRegPayment
    {
        public int Id { get; set; }
        public int RegistrationId { get; set; }
        public string PaymentId { get; set; }
        public string ExternalId { get; set; }
        public long Amount { get; set; }
        public DateTime CreatedDAte { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
