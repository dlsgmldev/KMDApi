using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class CompanyInfo
    {
        public int ClientId { get; set; }
        public int DealId { get; set; }
        public int UserId { get; set; }
        public string CompanyName { get; set; }
        public int ContactId { get; set; }
        public List<int> MemberIds { get; set; }
    }
}
