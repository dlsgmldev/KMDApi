using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class WebSurveyDimension
    {
        public int Id { get; set; }
        public string ItemText { get; set; }
        public string Description { get; set; }
        public int Parent { get; set; }
        public int SurveyId { get; set; }
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
