using KDMApi.Models.Crm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Web
{
    public class WebEventImageDetailResponse
    {
        public int Id { get; set; }
        public string Caption { get; set; }
        public string ImageURL { get; set; }
        public int EventId { get; set; }
        public string ImageName { get; set; }
        public string Event { get; set; }
        public int Publish { get; set; }
        public List<Error> Errors { get; set; }

        public WebEventImageDetailResponse()
        {
            Errors = new List<Error>();
        }
    }
}
