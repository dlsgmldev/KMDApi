using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Crm
{
    public class GetClientResponse
    {
        public CrmClient Client { get; set; }
        public IndustryInfo Industry { get; set; }
        public List<ContactInfo> Contacts { get; set; }
        public List<RelManagerInfo> RelManagers { get; set; }
        public IEnumerable<Error> Errors { get; }

        public GetClientResponse()
        {
            Errors = new[] { new Error("0", "") };
        }

        public GetClientResponse(IEnumerable<Error> errors)
        {
            Errors = errors;
        }
    }
}
