using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Km
{
    public class FileAccessInfo
    {
        public int FileId { get; set; }
        public string Name { get; set; }
        public string Action { get; set; }
        public string Filename { get; set; }
        public string FileType { get; set; }
        public string Tribe { get; set; }
        public string Client { get; set; }
        public string Year { get; set; }
        public string Project { get; set; }
        public bool Onegml { get; set; }
        public string Location { get; set; }
        public DateTime LastAccess { get; set; }
    }
}
