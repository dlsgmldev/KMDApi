using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Web
{
    public class WebBannerResponse
    {
        public int BannerId { get; set; }
        public string Filename { get; set; }
        public string URL { get; set; }
        public string MobileFilename { get; set; }
        public string MobileURL { get; set; }
        public bool Publish { get; set; }
        public string Link { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string CreatedDate { get; set; }
        public string LastUpdated { get; set; }

    }
}
