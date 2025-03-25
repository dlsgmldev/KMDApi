using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Web
{
    public class WebEventFrameworkInfo
    {
        public int Id { get; set; }
        public string Caption { get; set; }
        public List<IFormFile> FrameworkImages { get; set; }
    }
}
