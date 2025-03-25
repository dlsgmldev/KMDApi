using System;
using Web.Api.Core.Shared;



namespace Web.Api.Core.Domain.Entities
{
    public class Image : BaseEntity
    {
        public int FileTypeID { get; set; }
        public int FileDirectoryID { get; set; }
        public int? LinkID { get; set; }
        public int LinkTypeID { get; set; }
        public string FileURL { get; set; }
        public string FileName { get; set; }
        public string FileSize { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public string DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
       public int DeviceID { get; set; }
       public int FileID { get; private set; }
      
    }
}
