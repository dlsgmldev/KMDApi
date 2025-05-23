﻿using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class File
    {
        [Key]
        public Int32 FileID { get; set; }
        public int FileTypeID  { get; set; }
        public int FileDirectoryID { get; set; }
        public FileType FileType { get; set; }
        public FileDirectory FileDirectory { get; set; }
        public int? LinkID { get; set; }
        public string FileURL { get; set; }
        public string FileName { get; set; }
        public string FileSize { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public string DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
        public Device Device { get; set; }
        public int DeviceID { get; set; }
    }

    public class File_Request
    {
        public IFormFile files { get; set; }
        public int LinkID { get; set; }
        public int DeviceID { get; set; }
        public int LinkTypeID { get; set; }
        public string AccessToken { get; set; }
        public string CID { get; set; }
    }


    public class File_Response
    {
        [Key]
        public int Id { get; set; }
        public FileType_Response File_type { get; set; }
        public FileDirectory_Response File_directory { get; set; }
        public string URL { get; set; }
        public string Name { get; set; }
        public string Size { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Fullpath { get; set; }

    }
    public class File_List_Response
    {
        public Response_Status Response_status { get; set; }
        public List<File_Response> Files{ get; set; }
    }
}
