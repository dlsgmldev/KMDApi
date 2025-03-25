using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Km
{
    public class ProjectListInfo
    {
        public int Id { get; set; }             // Project Id or Workshop Id
        public string Name { get; set; }        // Workshop Name or Workshop Name
        public int Type { get; set; }           // 1 = project, 2 = workshop
        public int TribeId {get; set;}
        public string Tribe { get; set; }
        public int YearId { get; set; }
        public string Year { get; set; }
        public int ClientId { get; set; }
        public string Client { get; set; }
        public string Status { get; set; }
        public string ClientOwner { get; set; }
        public int WorkshopTypeId { get; set; }
    }
}
