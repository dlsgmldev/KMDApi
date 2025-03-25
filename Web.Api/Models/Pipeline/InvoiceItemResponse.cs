using KDMApi.Models.Km;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class InvoiceItemResponse
    {
        public InvoiceItemResponse()
        {
            Client = new GenericInfo();
            Branch = new GenericInfo();
            Segment = new GenericInfo();
            Contact = new GenericInfo()
            {
                Id = 0,
                Text = ""
            };
            Access = new List<GenericInfo>();
        }
        public int InvoiceId { get; set; }
        public int DealId { get; set; }
        public string DealName { get; set; }
        public GenericInfo Client { get; set; }
        public GenericInfo Branch { get; set; }
        public GenericInfo Segment { get; set; }
        public GenericInfo Contact { get; set; }
        public GenericInfo Pic { get; set; }
        public DateTime InvoiceDate { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public int Stage { get; set; }
        public long Amount { get; set; }
        public string Filename { get; set; }
        public string Remarks { get; set; }
        public List<PercentTribeResponse> Rms { get; set; }
        public List<GenericInfo> Access { get; set; }
        public List<PercentTribeResponse> Tribes { get; set; }
    }
}
