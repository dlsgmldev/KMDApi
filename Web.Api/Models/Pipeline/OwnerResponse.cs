using KDMApi.Models.Crm;
using KDMApi.Models.Km;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class OwnerResponse
    {
        public OwnerResponse()
        {
            Branch = new GenericInfo();
            Segment = new GenericInfo();
            Consultants = new List<GenericInfo>();
            Rms = new List<RMInfo>();
            Tribes = new List<GenericInfo>();
            Errors = new List<Error>();
        }

        public int DealId { get; set; }
        public GenericInfo Branch { get; set; }
        public GenericInfo Segment { get; set; }
        public List<GenericInfo> Consultants { get; set; }
        public List<RMInfo> Rms { get; set; }
        public List<GenericInfo> Tribes { get; set; }
        public List<Error> Errors { get; set; }
    }
}
