using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Km
{
    public class UploadFileRequestBase64
    {
        public int Onegml { get; set; }
        public int OwnerId { get; set; }
        public int Projectid { get; set; }
        public int ParentId { get; set; }
        public int UserId { get; set; }
        public List<UploadFileBase64> files { get; set; }
    }
}
