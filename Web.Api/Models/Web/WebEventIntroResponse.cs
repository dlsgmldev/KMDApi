using KDMApi.Models.Crm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Web
{
    public class WebEventIntroResponse
    {
        public WebEventIntroResponse()
        {
            Errors = new List<Error>();
            Speakers = new List<WebEventSpeakerInfo>();
        }
        public WebEvent webEvent { get; set; }
        public List<Error> Errors { get; set; }
        public string ThumbnailURL { get; set; }
        public List<WebEventSpeakerInfo> Speakers { get; set; }
    }
}
