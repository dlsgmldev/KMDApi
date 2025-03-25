using KDMApi.Models.Km;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class DealDetail
    {
        public DealDetail()
        {
            ClientCompany = new GenericInfo();
            ClientContact = new GenericInfo();
            ClientMembers = new List<GenericInfo>();
            Tribes = new List<PercentTribeResponse>();
            Segment = new GenericInfo();
            Branch = new GenericInfo();
            State = new GenericInfo();
            Stage = new GenericInfo();
            Pic = new GenericInfo();
            Rms = new List<RMInfo>();
            Consultants = new List<GenericInfo>();
        }
        public int Id { get; set; }
        public string Name { get; set; }
        public int Probability { get; set; }
        public int Age { get; set; }
        public List<int> Stages { get; set; }
        public GenericInfo ClientCompany { get; set; }
        public GenericInfo ClientContact { get; set; }          // Id nya itu ContactId
        public List<GenericInfo> ClientMembers { get; set; }    // Id nya itu ContactId
        public List<PercentTribeResponse> Tribes { get; set; }
        public GenericInfo Pic { get; set; }
        public GenericInfo Segment { get; set; }
        public GenericInfo Branch { get; set; }
        public GenericInfo State { get; set; }
        public GenericInfo Stage { get; set; }
        public List<RMInfo> Rms { get; set; }                   // Id nya itu UserId
        public List<GenericInfo> Consultants { get; set; }      // Id nya itu UserId
        public PostProposalResponse proposal { get; set; }
        public PostPricingResponse pricing { get; set; }
        public PostPricingResponse agreement { get; set; }
        public List<HistoryItem> history { get; set; }
    }
}
