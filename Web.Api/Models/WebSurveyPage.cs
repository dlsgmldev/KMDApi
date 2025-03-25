using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class WebSurveyPage
    {
        public int Id { get; set; }
        public int SurveyId { get; set; }
        public string TitleId { get; set; }
        public string IntroId { get; set; }
        public string TitleEn { get; set; }
        public string IntroEn { get; set; }
        public int PageNumber { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime LastUpdated { get; set; }
        public int LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public int DeletedBy { get; set; }
        public DateTime DeletedDate { get; set; }

    }
}
