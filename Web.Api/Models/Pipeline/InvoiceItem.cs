using KDMApi.Models.Km;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class InvoiceItem
    {
        public int InvoiceId { get; set; }
        public int DealId { get; set; }
        public string DealName { get; set; }
        public int BranchId { get; set; }
        public string BranchName { get; set; }
        public int SegmentId { get; set; }
        public String SegmentName { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; }
        public DateTime InvoiceDate { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public int Stage { get; set; }
        public long Amount { get; set; }
        public string Filename { get; set; }
        public string Remarks { get; set; }
    }
}
