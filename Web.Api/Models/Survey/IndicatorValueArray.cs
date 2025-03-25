using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Survey
{
    public class IndicatorValueArray
    {
        public int Id { get; set; }
        public string Indicator { get; set; }
        public List<Double> Value { get; set; }
        public Double Weight { get; set; }
    }
}
