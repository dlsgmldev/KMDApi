using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Web
{
    public class WebBannerRequest
    {
        public WebBannerRequest()
        {
            Category = "";
        }
        public int BannerId { get; set; }           // Digunakan juga untuk sorting
        public List<IFormFile> Image { get; set; }
        public List<IFormFile> MobileImage { get; set; }
        public string Link { get; set; }
        public int UserId { get; set; }
        public int Publish { get; set; }
        public string Category { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }
}
