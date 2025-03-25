using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class PercentTribeResponse
    {
        public PercentTribeResponse()
        {
            Nominal = 0;
            UsePercent = true;
        }
        public int Id { get; set; }
        public string Text { get; set; }
        public double Percent { get; set; }
        public bool UsePercent { get; set; }
        public long Nominal { get; set; }
    }
}
