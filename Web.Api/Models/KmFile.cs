using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class KmFile
    {
        public int Id { get; set; }
        public int ParentId { get; set; }
        public string Name { get; set; }
        public string Filename { get; set; }
        public string FileType { get; set; }
        public bool IsFolder { get; set; }
        public string RootFolder { get; set; }
        public string Description { get; set; }
        public int ProjectId { get; set; }
        public bool Onegml { get; set; }
        public int OwnerId { get; set; }                // Platform yang memiliki folder ini. Hanya untuk OneGML
        public string Fullpath { get; set; }
        public bool Extracted { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? LastUpdated { get; set; }
        public int LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public int DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }

    }
}
