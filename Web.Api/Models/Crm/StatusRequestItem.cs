using KDMApi.Models.Km;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Crm
{
    public class StatusItem
    {
        public int Id { get; set; }
        public int DealId { get; set; }
        public string Status { get; set; }
        public int UserId { get; set; }
    }

    public class StatusListItem
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public int UserId { get; set; }
        public string Firstname { get; set; }
        public DateTime? LastUpdated { get; set; }

    }
}