using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class KmInsight
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string KeyWord { get; set; }
        public string Slug { get; set; }
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string OriginalFilename { get; set; }
        public string Filename { get; set; }
        public string Filetype { get; set; }
        public bool Publish { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime LastUpdated { get; set; }
        public int LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public int DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
        public string Website { get; set; }
    }
}
