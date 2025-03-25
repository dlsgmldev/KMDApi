using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Survey
{
    public class IndicatorValue
    {
        public int Id { get; set; }
        public string Indicator { get; set; }
        public Double Value { get; set; }
        public Double Weight { get; set; }
    }
}
