using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Survey
{
    public class XY
    {
        public XY(double x, double y)
        {
            X = x;
            Y = y;
        }
        public XY()
        {
            X = 0.0d;
            Y = 0.0d;
        }
        public double X { get; set; }
        public double Y { get; set; }
    }
}
