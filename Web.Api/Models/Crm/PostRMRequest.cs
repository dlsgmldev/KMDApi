using KDMApi.Models.Km;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Crm
{
    public class PostRMRequest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string NIK { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string JobTitle { get; set; }
        public int PlatformId { get; set; }
        public int SegmentId { get; set; }
        public int BranchId { get; set; }
        public string FileBase64 { get; set; }
        public string Filename { get; set; }
        public int UserId { get; set; }
    }
    public class GetRMResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string NIK { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string JobTitle { get; set; }
        public GenericInfo Platform { get; set; }
        public GenericInfo Segment { get; set; }
        public GenericInfo Branch { get; set; }
        public string FileUrl { get; set; }
    }
}
