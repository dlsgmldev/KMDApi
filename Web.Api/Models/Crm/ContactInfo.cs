using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Crm
{
    public class ContactInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Salutation { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Department { get; set; }
        public string Position { get; set; }
        public bool Valid { get; set; }
    }
}
