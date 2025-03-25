using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class KmProject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public string Venue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string KeyWord { get; set; }
        public int YearId { get; set; }
        public int ClientId { get; set; }
        public int TribeId { get; set; }
        public int WorkshopTypeId { get; set; }     // 0 = Project, 1 = In-house, 2 = Public
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? LastUpdated { get; set; }
        public int LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public int DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }

    }
}
