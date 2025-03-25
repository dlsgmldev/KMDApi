using KDMApi.Models.Crm;
using KDMApi.Models.Helper;
using KDMApi.Models.Km;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Web
{
    public class WebEventResponse
    {
        // Deskripsi dan framework dimasukkan ke dalam description string
        public WebEvent Event { get; set; }
        public GenericInfo Category { get; set; }
        public GenericInfo Topic { get; set; }
        public GenericInfo Location { get; set; }
        public string Description { get; set; }
        public WebEventBrochureInfo Thumbnail { get; set; }
        public List<WebEventSpeakerInfo> Speakers { get; set; }
        public WebEventBrochureInfo Brochure { get; set; }
        public WebEventBrochureInfo Flyer { get; set; }
        public List<WebEventAgendaInfo> Agenda { get; set; }
        public List<WebEventTestimonyResponse> Testimonies { get; set; }
        public List<WebEventInvestmentInfo> Investments { get; set; }
        public List<string> Takeaways { get; set; }
        public bool EmailNotification { get; set; }
        public string EmailSubject { get; set; }
        public string Email { get; set; }
        public List<Error> Errors { get; set; }
        public CdhxQubisa PayloadQubisa { get; set; }
        public WebEventResponse()
        {
            Errors = new List<Error>();
        }
        public WebEventResponse(WebEvent webEvent)
        {
            Errors = new List<Error>();
            Event = webEvent;
        }
    }
}
