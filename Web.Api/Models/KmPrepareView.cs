using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class KmPrepareView
    {
        public int Id { get; set; }
        public string Filename { get; set; }
        public string DisplayFilename { get; set; }
        public string Filetype { get; set; }
        public int UserId { get; set; }
        public int FileId { get; set; }
        public int RandomId { get; set; }
        public DateTime Expired { get; set; }
        public bool PublicAccess { get; set; }
        public string Drive { get; set; }
        public string Path1 { get; set; }
        public string Path2 { get; set; }
        public string Path3 { get; set; }
        public string Path4 { get; set; }
        public string Path5 { get; set; }
        public string Path6 { get; set; }
        public string Fullpath { get; set; }
    }
}
