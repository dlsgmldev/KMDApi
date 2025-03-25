using KDMApi.Models.Km;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class RMListItem
    {
        public int Id { get; set; }                 // CrmRelManager.Id
        public int UserId { get; set; }
        public string Name { get; set; }
        public string NIK { get; set; }
        public string JobTitle { get; set; }
        public GenericInfo Platform { get; set; }
        public GenericInfo Segment { get; set; }
        public GenericInfo Branch { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string ProfileURL { get; set; }
    }
}
