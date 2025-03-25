using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Crm
{
    public class ExportContactInfo
    {
        public int Id { get; set; }
        public string Info { get; set; }
        public string Company { get; set; }
        public string Executive { get; set; }
        public string Title { get; set; }
        public string Department { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string HP { get; set; }
        public string HP1 { get; set; }
        public string HP2 { get; set; }
        public string Phone { get; set; }
        public string Fax { get; set; }
        public string Email1 { get; set; }
        public string Email2 { get; set; }
        public string Email3 { get; set; }
        public string Email4 { get; set; }
        public string Website { get; set; }
        public string Industry { get; set; }
        public string Remarks { get; set; }
    }
}
