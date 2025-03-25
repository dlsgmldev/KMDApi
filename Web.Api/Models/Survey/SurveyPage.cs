using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Survey
{
    public class SurveyPage
    {
        public SurveyPage()
        {
            Items = new List<SurveyItem>();
        }
        public int Id { get; set; }                 // PageId
        public int PageNumber { get; set; }
        public DualLanguage Title { get; set; }
        public DualLanguage Intro { get; set; }
        public string ItemType { get; set; }
        public List<SurveyItem> Items { get; set; }
    }
}
