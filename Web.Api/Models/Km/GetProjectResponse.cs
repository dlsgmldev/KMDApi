using KDMApi.Models.Crm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Km
{
    public class GetProjectResponse
    {
        public KmProject Project { get; set; }
        public List<GenericInfo> Trainers { get; set; }
        public GenericInfo ProjectAdvisor { get; set; }
        public GenericInfo ProjectLeader { get; set; }
        public List<GenericInfo> ProjectMembers { get; set; }
        public GenericInfo ClientProjectOwner { get; set; }
        public GenericInfo ClientProjectLeader { get; set; }
        public List<GenericInfo> ClientProjectMembers { get; set; }
        public List<GenericInfo> Products { get; set; }
        public List<GenericInfo> Participants { get; set; }
        public GenericInfo Tribe { get; set; }
        public GenericInfo Client { get; set; }
        public int Year { get; set; }
        public FolderContent Content { get; set; }
        public List<GenericInfo> Breadcrump { get; set; }
        public IEnumerable<Error> Errors { get; }

        public GetProjectResponse()
        {
            Errors = new[] { new Error("0", "") };
            Trainers = new List<GenericInfo>();
            ProjectMembers = new List<GenericInfo>();
            ClientProjectMembers = new List<GenericInfo>();
            Products = new List<GenericInfo>();
            Breadcrump = new List<GenericInfo>();
        }

        public GetProjectResponse(IEnumerable<Error> errors)
        {
            Errors = errors;
            Trainers = new List<GenericInfo>();
            ProjectMembers = new List<GenericInfo>();
            ClientProjectMembers = new List<GenericInfo>();
            Products = new List<GenericInfo>();
            Breadcrump = new List<GenericInfo>();
        }

    }
}
