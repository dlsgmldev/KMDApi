using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class WebEventImage
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Filename { get; set; }
        public string FileType { get; set; }
        public int EventId { get; set; }
        public int DescriptionId { get; set; }
        public int FrameworkId { get; set; }
        public int DocumentationId { get; set; }
        public int TestimonyId { get; set; }
        public int ThumbnailId { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime LastUpdated { get; set; }
        public int LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public int DeletedBy { get; set; }
        public DateTime DeletedDate { get; set; }
    }
}
