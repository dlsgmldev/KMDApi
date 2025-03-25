using KDMApi.Models.Km;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Crm
{
    public class Team
    {
        public int Id { get; set; }     // CrmRelManager.Id of the team leader
        public string Text { get; set; }
        public GenericInfo Leader { get; set; }         // User.Id and User.Firstname of mentor, leader, and members
        public GenericInfo Mentor { get; set; }
        public List<GenericInfo> Members { get; set; }

    }
}
