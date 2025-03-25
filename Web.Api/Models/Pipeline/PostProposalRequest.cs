using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class PostProposalRequest
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int DealId { get; set; }
        public int SentById { get; set; }
        public int TypeId { get; set; }
        public DateTime SentDate { get; set; }
        public List<int> ContactIds { get; set; }
        public string Filename { get; set; }
        public string FileBase64 { get; set; }
        public long ProposalValue { get; set; }
        public List<InvoicePeriodInfo> Invoices { get; set; }

    }
}
