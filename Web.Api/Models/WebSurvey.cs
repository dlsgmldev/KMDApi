using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class WebSurvey
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Intro { get; set; }
        public string Description { get; set; }
        public int CategoryId { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool Publish { get; set; }
        public string AddInfo { get; set; }
        public string EmailToUserIds { get; set; }
        public bool Grouping { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime LastUpdated { get; set; }
        public int LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public int DeletedBy { get; set; }
        public DateTime DeletedDate { get; set; }

    }
}
