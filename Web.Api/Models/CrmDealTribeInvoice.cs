using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class CrmDealTribeInvoice
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public long Amount { get; set; }
        public double Percentage { get; set; }
        public int TribeId { get; set; }
        public bool UsePercent { get; set; }
    }
}
