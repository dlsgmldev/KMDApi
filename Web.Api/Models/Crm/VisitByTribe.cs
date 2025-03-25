using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Crm
{
    public class VisitByTribe
    {
        public int Visit { get; set; }
        public int Id { get; set; }
        public string Firstname { get; set; }
        public int TribeId { get; set; }
    }
}
