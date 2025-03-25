using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace KDMApi.Models.Km
{
    public class UploadFileRequest
    {
        public int Onegml { get; set; }
        public int OwnerId { get; set; }
        public int Projectid { get; set; }
        public int ParentId { get; set; }
        public int UserId { get; set; }
        public List<IFormFile> Files { get; set; }
       
    }
}
