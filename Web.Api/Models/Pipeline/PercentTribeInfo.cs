using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class PercentTribeInfo
    {
        public PercentTribeInfo()
        {
            Nominal = 0;
            UsePercent = true;
        }
        public int TribeId { get; set; }
        public bool UsePercent { get; set; }
        public long Nominal { get; set; }
        public double Percent { get; set; }
    }
}
