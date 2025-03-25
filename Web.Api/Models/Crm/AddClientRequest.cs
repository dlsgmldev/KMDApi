using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Crm
{
    public class AddClientRequest
    {
        public string Company { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string Phone { get; set; }
        public string Fax { get; set; }
        public string Website { get; set; }
        public string Remarks { get; set; }
        public int IndustryId { get; set; }
        public List<int> RelManagerIds { get; set; }
        public List<ContactInfo> contacts { get; set; }
        public int UserId { get; set; }

    }
}
