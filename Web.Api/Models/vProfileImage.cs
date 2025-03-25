using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class vProfileImage
    {
        public int Id { get; set; }                 // User.ID
        public bool IsDeleted { get; set; }
        public string FileURL { get; set; }
        public string FileName { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
    }
}
