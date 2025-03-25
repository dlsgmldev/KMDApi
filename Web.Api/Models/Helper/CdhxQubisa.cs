using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Helper
{
    public class CdhxQubisa
    {
        public int contentId { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string slug { get; set; }
        public string categoryURL { get; set; }
        public string metaTitle { get; set; }
        public string metaDescription { get; set; }
        public string metaKeyword { get; set; }
        public string dateStart { get; set; }
        public string dateEnd { get; set; }
        public int price { get; set; }
        public bool isOfflineEvent { get; set; }
        public string addressOfflineEvent { get; set; }
        public string directZoom { get; set; }
        public bool isDeleted { get; set; }
        public bool isPublished { get; set; }
        public string imageURL { get; set; }
        public string youtubeVideoURL { get; set; }
        public string BrochureURL { get; set; }
    }
}
