using KDMApi.Models.Km;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class PipelineItem
    {
        public PipelineItem()
        {
            Rms = new List<PercentTribeResponse>();
            Access = new List<GenericInfo>();
        }
        public int DealId { get; set; }
        public string DealName { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; }
        public int Probability { get; set; }
        public int Age { get; set; }
        public int Stage { get; set; }
        public DateTime DealDate { get; set; }
        public int ProposalId { get; set; }
        public long ProposalValue { get; set; }
        public int InvoicePeriod { get; set; }

        public List<PercentTribeResponse> Rms { get; set; }
        public List<GenericInfo> Access { get; set; }
        public DateTime? StatusDate { get; set; }
    }
}
