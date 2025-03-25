using KDMApi.Models.Km;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class PipelineMasterResponse
    {
        public List<GenericInfo> Segments { get; set; }
        public List<GenericInfo> Tribes { get; set; }
        public List<GenericInfo> Branches { get; set; }
        public List<RMInfo> Rms { get; set; }
        public List<GenericInfo> Stages { get; set; }
        public List<GenericInfo> States { get; set; }
        public List<GenericInfo> ProposalTypes { get; set; }
    }
}
