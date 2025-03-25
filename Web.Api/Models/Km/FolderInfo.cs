using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Km
{
    public class FolderInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int ParentId { get; set; }
        public int ProjectId { get; set; }
        public int Onegml { get; set; }
        public int OwnerId { get; set; }
        public int UserId { get; set; }
    }
}
