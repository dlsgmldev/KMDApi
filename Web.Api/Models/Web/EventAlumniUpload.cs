using KDMApi.Models.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Web
{
    public class EventAlumniUpload
    {
        public int EventId { get; set; }
        public string Filename { get; set; }
        public string FileBase64 { get; set; }
        public List<EmailAddress> Experts { get; set; }
        public int UserId { get; set; }
    }
}
