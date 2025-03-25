using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Web
{
    public class WebEventTestimonyResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Company { get; set; }
        public string Testimony { get; set; }
        public WebEventBrochureInfo Photo { get; set; }
    }
}
