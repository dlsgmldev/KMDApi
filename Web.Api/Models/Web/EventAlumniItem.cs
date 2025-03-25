using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Web
{
    public class EventAlumniItem
    {
        public int Id { get; set; }                 // Contact.Id
        public string Name { get; set; }
        public string Company { get; set; }
        public string Department { get; set; }
        public string JobTitle { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
    }
}
