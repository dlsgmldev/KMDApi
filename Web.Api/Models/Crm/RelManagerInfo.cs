using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Crm
{
    public class RelManagerInfo
    {
        public int Id { get; set; }             // User.ID
        public int RelManagerId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Segment { get; set; }
        public string Branch { get; set; }

    }
}
