using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Web
{
    public class WebBannerBaseRequest
    {
        public int BannerId { get; set; }           // Digunakan juga untuk sorting
        public string Filename { get; set; }
        public string FileBase64 { get; set; }
        public int Publish { get; set; }

    }
}
