using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class WebImage
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Filename { get; set; }
        public string FileType { get; set; }
        public string MobileName { get; set; }
        public string MobileFilename { get; set; }
        public string MobileFileType { get; set; }
        public int BannerId { get; set; }
        public bool Publish { get; set; }
        public string Link { get; set; }
        public string Category { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime LastUpdated { get; set; }
        public int LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public int DeletedBy { get; set; }
        public DateTime DeletedDate { get; set; }

    }
}
