using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Insight
{
    public class PostInsight
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public List<int> AuthorIds { get; set; }
        public List<int> CategoryIds { get; set; }
        public string Filename { get; set; }
        public string FileBase64 { get; set; }
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string Slug { get; set; }
        public List<string> KeyWords { get; set; }
        public int Publish { get; set; }
        public string Website { get; set; }
    }
}
