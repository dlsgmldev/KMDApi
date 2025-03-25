using KDMApi.Models.Crm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Web
{
    public class WebContact
    {
        public List<WebContactRegister> Contacts { get; set; }
        public PaginationInfo Info { get; set; }
    }

}
