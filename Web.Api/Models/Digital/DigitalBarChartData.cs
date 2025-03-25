using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Digital
{
    public class DigitalBarChartData
    {
        public List<DigitalBarItem> Items { get; set; }
        public List<string> Legends { get; set; }
    }
}
