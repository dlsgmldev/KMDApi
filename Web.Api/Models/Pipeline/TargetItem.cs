using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class TargetItem
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public List<GenericAmount> Targets { get; set; }
    }
}
