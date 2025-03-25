using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class PostInvoiceRequest
    {
        public int DealId { get; set; }
        public int InvoiceId { get; set; }
        public int userId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public long Amount { get; set; }
        public string Remarks { get; set; }
    }
}
