using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Km
{
    public class FolderContent
    {
        public FolderContent()
        {
            Id = 0;
            Name = "Root";
            Date = new DateTime(1970, 1, 1);
            Location = "";
            Owner = "";
            OwnerId = 0;
            folders = new List<FileFolderInfo>();
            files = new List<FileFolderInfo>();
        }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public DateTime Date { get; set; }
        public string Owner { get; set; }
        public int OwnerId { get; set; }
        public List<FileFolderInfo> folders { get; set; }
        public List<FileFolderInfo> files { get; set; }
    }
}
