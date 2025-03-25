using KDMApi.Models.Km;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Web
{
    public class DetailAlumniResponse
    {
        public int Id { get; set; }                 // Contact.Id
        public string Name { get; set; }
        public GenericInfo Company { get; set; }
        public string Department { get; set; }
        public string JobTitle { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public List<GenericInfo> Events { get; set; }
    }
}
