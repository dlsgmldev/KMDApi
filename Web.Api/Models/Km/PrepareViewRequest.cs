using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Km
{
    public class PrepareViewRequest
    {
        public int UserId { get; set; }
        public List<int> FileIds { get; set; }
    }
}
