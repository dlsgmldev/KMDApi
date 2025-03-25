using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Web
{
    public class WebEventImageRequest
    {
        public int Id { get; set; }
        public string Caption { get; set; }
        public List<IFormFile> Image { get; set; }
        public int EventId { get; set; }
        public int Publish { get; set; }
        public int UserId { get; set; }
    }
}
