using DocumentFormat.OpenXml.Office.CoverPageProps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class WebEvent
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Intro { get; set; }
        public string Slug { get; set; }
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string Keyword { get; set; }
        public int CategoryId { get; set; }
        public int LocationId { get; set; }
        public int TopicId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Audience { get; set; }
        public string Address { get; set; }
        public bool Publish { get; set; }
        public string AddInfo { get; set; }
        public string RegistrationURL { get; set; }
        public string EmailSubject { get; set; }
        public string Email { get; set; }
        public string CdhxCategory { get; set; }
        public int TribeId { get; set; }
        public string VideoURL { get; set; }
        public string LinkZoom { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime LastUpdated { get; set; }
        public int LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public int DeletedBy { get; set; }
        public DateTime DeletedDate { get; set; }
    }
}
