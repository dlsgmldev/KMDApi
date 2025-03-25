using KDMApi.Models.Km;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class IndividualExportProposalItem
    {
        public int No { get; set; }
        public int ProposalId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Client { get; set; }
        public int SentById { get; set; }
        public string SentBy { get; set; }
        public DateTime SentDate { get; set; }
        public long ProposalValue { get; set; }
        public string Filename { get; set; }
    }

    public class IndividualExportProposalItemResponse
    {
        public List<IndividualExportProposalItem> Items;
        public int Total;
    }

    public class IndividualExportPICProposalItem
    {
        public IndividualExportPICProposalItem(IndividualExportProposalItem item, int n)
        {
            No = n;
            ProposalId = item.ProposalId;
            Name = item.Name;
            Type = item.Type;
            SentById = item.SentById;
            SentBy = item.SentBy;
            SentDate = item.SentDate;
            ProposalValue = item.ProposalValue;
            Filename = item.Filename;
            Rms = "";
            Segments = "";
        }
        public int No { get; set; }
        public int ProposalId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public int SentById { get; set; }
        public string SentBy { get; set; }
        public DateTime SentDate { get; set; }
        public long ProposalValue { get; set; }
        public string Filename { get; set; }
        public String Rms { get; set; }
        public String Segments { get; set; }
    }

}
