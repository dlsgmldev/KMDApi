using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class WebSurveyResponse
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public int RatingId { get; set; }           // Sebetulnya RatingItemId
        public int Ranking { get; set; }            // In descencind order!!! Angka makin besar, artinya ranking makin tinggi
        public string AnswerText { get; set; }
        public string Uuid { get; set; }
        public string GroupUUID { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime DeletedDate { get; set; }

    }
}
