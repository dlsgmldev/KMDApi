using KDMApi.Models.Km;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Survey
{
    public class QuadrantData
    {
        public QuadrantData()
        {
            Quadrant = new List<GenericInfoString>();
            Data = new List<XY>();
        }
        public List<XY> Data { get; set; }
        public List<GenericInfoString> Quadrant { get; set; }
    }
}
