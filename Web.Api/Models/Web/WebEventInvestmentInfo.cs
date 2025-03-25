using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Web
{
    public class WebEventInvestmentInfo
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public int Nominal { get; set; }
        public bool ppn { get; set; }
        public int ppnpercent { get; set; }
        public string paymenturl { get; set; }
    }
}
