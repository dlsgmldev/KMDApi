using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class CrmDealVisitUser
    {
        // Internal users who participate in visits
        public int Id { get; set; }
        public int VisitId { get; set; }
        public int Userid { get; set; }
        public bool IsRm { get; set; }
        public bool IsConsultant { get; set; }
    }
}
