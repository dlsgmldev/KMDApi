using KDMApi.Models.Crm;
using KDMApi.Models.Km;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class PostProposalResponse
    {
        public PostProposalResponse()
        {
            Errors = new List<Error>();
            Invoices = new List<InvoicePeriodInfo>();
            ProposalType = new GenericInfo();
        }
        public int Id { get; set; }
        public int SentById { get; set; }
        public DateTime SentDate { get; set; }
        public List<int> ContactIds { get; set; }
        public GenericInfo ProposalType { get; set; }
        public string Filename { get; set; }
        public string Url { get; set; }
        public long ProposalValue { get; set; }
        public List<InvoicePeriodInfo> Invoices { get; set; }
        public List<Error> Errors { get; set; }
    }
}
