using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Crm
{
    public class ClientContactResponse
    {
        public List<ClientContactInfo> Contacts { get; set; }
        public PaginationInfo Info { get; set; }
    }
}
