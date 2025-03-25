using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;


namespace KDMApi.Models.Web
{
    public class WebEventTestimonyInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Company { get; set; }
        public string Testimony { get; set; }
        public string PhotoFilename { get; set; }
        public string TestimonyPhotos { get; set; }
    }
}
