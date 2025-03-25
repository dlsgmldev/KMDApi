using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Km
{
    public class DetailInsight
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public List<GenericInfo> Authors { get; set; }
        public List<GenericInfo> Categories { get; set; }
        public string Filename { get; set; }
        public string FileBase64 { get; set; }
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string Slug { get; set; }
        public List<string> KeyWords { get; set; }
        public string Thumbnail { get; set; }
        public DateTime LastUpdated { get; set; }
        public GenericWebsite Website { get; set; }
    }
}
