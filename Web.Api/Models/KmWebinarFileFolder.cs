using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class KmWebinarFileFolder
    {
        public int Id { get; set; }
        public string RootFolder { get; set; }
        public string FolderFileName { get; set; }
        public bool IsFolder { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
    }
}
