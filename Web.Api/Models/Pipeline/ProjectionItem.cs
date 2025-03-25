using KDMApi.Models.Km;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class ProjectionItem
    {
        public ProjectionItem()
        {
            Rms = new List<PercentTribeResponse>();
            Access = new List<GenericInfo>();
        }
        public int DealId { get; set; }
        public int InvoiceId { get; set; }
        public int Stage { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string Remarks { get; set; }
        public string DealName { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; }
        public int Probability { get; set; }
        public int Age { get; set; }
        public DateTime DealDate { get; set; }
        public long Value { get; set; }
        public List<PercentTribeResponse> Rms { get; set; }
        public List<GenericInfo> Access { get; set; }
    }
}
