using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class InvoicePeriodInfo
    {
        public int Id { get; set; }
        public DateTime InvoiceDate { get; set; }
        public long InvoiceAmount { get; set; }
        public string Remarks { get; set; }
    }
}
