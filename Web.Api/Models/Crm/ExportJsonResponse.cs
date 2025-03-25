using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Crm
{
    public class ExportJsonResponse
    {
        public List<string> Headers { get; set; }
        public List<ExportContactInfo> Items { get; set; }
    }
}
