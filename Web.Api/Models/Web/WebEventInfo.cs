using DocumentFormat.OpenXml.Office.CoverPageProps;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Web
{
    public class WebEventInfo
    {
        public WebEventInfo()
        {
            RegistrationURL = "";
            Slug = "";
            MetaTitle = "";
            MetaDescription = "";
            Keyword = "";
            Flyer = "";
            FlyerFilename = "";
            Takeaways = new List<string>();
            EmailNotification = false;
            EmailSubject = "";
            Email = "";
            CdhxCategory = "";
            VideoURL = "";
        }
        public int Id { get; set; }
        public int profileId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; }
        public string Intro { get; set; }
        public string Slug { get; set; }
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string Keyword { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Address { get; set; }
        public int CategoryId { get; set; }     // 1. Mega seminar, 2. Public workshop, 3. Webinar, 4. Blended learning
        public int LocationId { get; set; }
        public int TopicId { get; set; }
        public bool Publish { get; set; }        
        public string Audience { get; set; }
        public List<WebEventSpeakerInfo> Speakers { get; set; }
        public string Description { get; set; }
        //    public List<IFormFile> Thumbnails { get; set; }
        //  public List<IFormFile> Brochures { get; set; }
        public string ThumbnailFilename { get; set; }
        public string Thumbnails { get; set; }
        public string BrochuleFilename { get; set; }
        public string Brochures { get; set; }
        public string FlyerFilename { get; set; }
        public string Flyer { get; set; }
        public List<WebEventAgendaInfo> Agenda { get; set; }
        public List<WebEventTestimonyInfo> Testimonies { get; set; }
        public List<WebEventInvestmentInfo> Investments { get; set; }
        public string RegistrationURL { get; set; }
        public List<string> Takeaways { get; set; }
        public bool EmailNotification { get; set; }
        public string EmailSubject { get; set; }
        public string Email { get; set; }
        public string CdhxCategory { get; set; }
        public int TribeId { get; set; }
        public string VideoURL { get; set; }
        public string LinkZoom { get; set; }

    }
}
