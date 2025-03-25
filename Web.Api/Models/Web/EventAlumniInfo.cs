using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Web
{
    public class EventAlumniInfo
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public int EventId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Department { get; set; }
        public string JobTitle { get; set; }
        public int UserId { get; set; }
    }
}
