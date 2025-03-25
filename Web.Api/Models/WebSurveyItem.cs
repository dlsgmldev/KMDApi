using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class WebSurveyItem
    {
        public int Id { get; set; }

        public string TitleTextId { get; set; }
        public string ItemTextId { get; set; }
        public string TitleTextEn { get; set; }
        public string ItemTextEn { get; set; }
        public int RatingId { get; set; }
        public int PageId { get; set; }
        public int TypeId { get; set; }
        public double Weight { get; set; }
        public bool ShowInCover { get; set; }
        public bool GroupReport { get; set; }
        public int OrderNumber { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime LastUpdated { get; set; }
        public int LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public int DeletedBy { get; set; }
        public DateTime DeletedDate { get; set; }

    }
}
