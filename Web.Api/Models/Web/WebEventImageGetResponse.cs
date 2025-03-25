using KDMApi.Models.Crm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Web
{
    public class WebEventImageGetResponse
    {
        public List<WebEventImageDetailResponse> images { get; set; }
        public PaginationInfo info { get; set; }
    }
}
