using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Crm
{
    public class PostTeamRequest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int LeaderId { get; set; }           // User.id dari leader
        public int MentorId { get; set; }           // User.id dari mentor
        public List<int> Members { get; set; }      // List User.id dari members
        public int UserId { get; set; }
    }
}
