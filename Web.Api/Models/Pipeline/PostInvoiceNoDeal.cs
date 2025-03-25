using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class PostInvoiceNoDeal
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public int BranchId { get; set; }
        public string DealName { get; set; }
        public int UserId { get; set; }
        public DateTime InvoiceDate { get; set; }
        public int ContactId { get; set; }
        public int PicId { get; set; }
        public long Amount { get; set; }
        public string Remarks { get; set; }
        public string Filename { get; set; }
        public string FileBase64 { get; set; }
        public List<PercentInfo> Rms { get; set; }
        public List<PercentTribeInfo> Tribes { get; set; }

    }
}
