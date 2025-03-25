using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Km
{
    public class EditProjectRequest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public string Venue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<string> KeyWords { get; set; }
        public int ClientId { get; set; }
        public int TribeId { get; set; }
        public int WorkshopTypeId { get; set; }
        public int UserId { get; set; }
        public List<int> TrainerIds { get; set; }
        public int ProjectAdvisorId { get; set; }
        public int ProjectLeaderId { get; set; }
        public List<int> ProjectMemberIds { get; set; }
        public int ClientProjectOwnerId { get; set; }
        public int ClientProjectLeaderId { get; set; }
        public List<int> ClientProjectMemberIds { get; set; }
        public List<int> ProductIds { get; set; }
    }
}
