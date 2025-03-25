using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class FileType
    {
        [Key]
        public int FileTypeID { get; set; }
        public string TypeName { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public string DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
    }

    public class FileType_Response
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
