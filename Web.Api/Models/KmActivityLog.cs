using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class KmActivityLog
    {
        public int Id { get; set; }
        public string Action { get; set; }
        public int UserId { get; set; }
        public int FileId { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
