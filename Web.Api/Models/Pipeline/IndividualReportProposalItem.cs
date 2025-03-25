using KDMApi.Models.Km;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class IndividualReportProposalItem
    {
        public IndividualReportProposalItem()
        {
            Invoices = new List<InvoicePeriodInfo>();
            ReceiverClients = new List<GenericInfo>();
        }
        public int ProposalId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public int SentById { get; set; }
        public string SentBy { get; set; }
        public DateTime SentDate { get; set; }
        public long ProposalValue { get; set; }
        public string Filename { get; set; }
        public List<InvoicePeriodInfo> Invoices { get; set; } 
        public List<GenericInfo> ReceiverClients { get; set; }
    }

    public class IndividualReportPICProposalItem
    {
        public IndividualReportPICProposalItem()
        {
            Invoices = new List<InvoicePeriodInfo>();
            ReceiverClients = new List<GenericInfo>();
            Rms = new List<GenericInfo>();
            Segments = new List<GenericInfo>();
        }
        public int ProposalId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public int SentById { get; set; }
        public string SentBy { get; set; }
        public DateTime SentDate { get; set; }
        public long ProposalValue { get; set; }
        public string Filename { get; set; }
        public List<GenericInfo> Rms { get; set; }
        public List<GenericInfo> Segments { get; set; }
        public List<InvoicePeriodInfo> Invoices { get; set; }
        public List<GenericInfo> ReceiverClients { get; set; }
    }

}
