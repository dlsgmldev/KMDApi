using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Km
{
    public class UpdateFileRequest
    {
        public int FileId { get; set; }
        public int UserId { get; set; }
        public string Filename { get; set; }
        public string FileBase64 { get; set; }
    }
}
