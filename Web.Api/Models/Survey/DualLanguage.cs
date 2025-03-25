using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Survey
{
    public class DualLanguage
    {
        public DualLanguage(string en, string id)
        {
            En = en;
            Id = id;
        }
        public string En { get; set; }
        public string Id { get; set; }
    }
}
