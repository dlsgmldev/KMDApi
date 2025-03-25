using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class InvoiceResponse
    {
        public InvoiceResponse()
        {
            Invoice = new List<InvoiceItemResponse>();
        }
        public List<MonthStageInfo> Stages { get; set; }
        public List<InvoiceItemResponse> Invoice { get; set; }
    }
}
