using DocumentFormat.OpenXml.Office.CoverPageProps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Km
{
    public class FilterProjectInfo
    {
        public FilterProjectInfo()
        {
            Trainers = new List<GenericInfo>();
            ProjectAdvisor = new GenericInfo();
            ProjectLeader = new GenericInfo();
            ProjectMembers = new List<GenericInfo>();
            ClientProjectOwner = new GenericInfo();
            ClientProjectLeader = new GenericInfo();
            ClientProjectMembers = new List<GenericInfo>();
            Products = new List<GenericInfo>();
            Tribe = new GenericInfo();
            Client = new GenericInfo();
        }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public string Venue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string KeyWordStr { get; set; }
        public List<string> KeyWord { get; set; }
        public int workshopTypeId { get; set; }
        public int Year { get; set; }
        public int YearId { get; set; }
        public List<GenericInfo> Trainers { get; set; }
        public GenericInfo ProjectAdvisor { get; set; }
        public GenericInfo ProjectLeader { get; set; }
        public List<GenericInfo> ProjectMembers { get; set; }
        public GenericInfo ClientProjectOwner { get; set; }
        public GenericInfo ClientProjectLeader { get; set; }
        public List<GenericInfo> ClientProjectMembers { get; set; }
        public List<GenericInfo> Products { get; set; }
        public GenericInfo Tribe { get; set; }
        public GenericInfo Client { get; set; }
        public int ClientId { get; set; }
        public int TribeId { get; set; }

    }
}
