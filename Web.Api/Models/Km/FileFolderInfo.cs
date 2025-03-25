using DocumentFormat.OpenXml.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Km
{
    public class FileFolderInfo
    {
        public FileFolderInfo()
        {
            Owner = "";
            OwnerId = 0;
            FileType = "";
        }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public string FileType { get; set; }
        public string Description { get; set; }
        public string Owner { get; set; }
        public int OwnerId { get; set; }
        public DateTime Date { get; set; }
    }
}
