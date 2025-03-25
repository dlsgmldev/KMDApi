using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Km
{
    public class GenericWebsite
    {
        public GenericWebsite()
        {
            label = "";
            value = "";
        }
        public string label { get; set; }
        public string value { get; set; }
    }
}
