using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class ViewUserRM
    {
        public int Id { get; set; }     // CrmRelManager.Id
        public string JobTitle { get; set; }
        public int UserId { get; set; }
        public int SegmentId { get; set; }
        public int BranchId { get; set; }
        public int PlatformId { get; set; }
        public string FirstName { get; set; }
        public string Branch { get; set; }
        public string Platform { get; set; }
        public string Segment { get; set; }
        public string IdNumber { get; set; }
        public string FileURL { get; set; }
        public string Filename { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
    }
}
