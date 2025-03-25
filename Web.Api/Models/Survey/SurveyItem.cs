using KDMApi.Models.Km;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Survey
{
    public class SurveyItem
    {
        public int Id { get; set; }                 // ItemId
        public string ItemType { get; set; }
        public DualLanguage Title { get; set; }
        public DualLanguage Text { get; set; }
        public List<DualLanguageId> Options { get; set; }      // RatingId dan teks nya
    }
}
