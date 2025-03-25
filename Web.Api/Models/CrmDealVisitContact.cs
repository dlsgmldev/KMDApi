using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class CrmDealVisitContact
    {
        public int Id { get; set; }
        public int VisitId { get; set; }
        public int ContactId { get; set; }
    }
}
