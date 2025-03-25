using KDMApi.Models.Crm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Web
{
    public class GetBannerResponse
    {
        public List<WebBannerResponse> banners { get; set; }
        public PaginationInfo info { get; set; }
    }
}
