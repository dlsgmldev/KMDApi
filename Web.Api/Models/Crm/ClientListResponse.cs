using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Crm
{
    public class ClientListResponse
    {
        public List<ClientInfo> clients { get; set; }
        public List<IndustryInfo> industries { get; set; }
        public List<RelManagerInfo> relManagers { get; set; }
        public List<SegmentInfo> segments { get; set; }
        public IEnumerable<Error> Errors { get; }
        public PaginationInfo info { get; set; }
        public ClientListResponse(IEnumerable<Error> errors)
        {
            Errors = errors;
        }
        public ClientListResponse()
        {
            Errors = new[] { new Error("0", "") };
        }
    }
}
