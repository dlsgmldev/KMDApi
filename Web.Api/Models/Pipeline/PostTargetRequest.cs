using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class PostTargetRequest
    {
        public int Id { get; set; }                 // User.Id dari RM
        public string Type { get; set; }            // "tribe", "segment", "rm", or "branch"
        public int UserId { get; set; }
        public bool Reject { get; set; }            // if !Reject && !Approve --> posting by self
        public bool Approve { get; set; }
        public List<TargetItem> items { get; set; }
    }

    public class GetTargetResponse
    {
        public int Id { get; set; }                 // User.Id dari RM
        public string Type { get; set; }            // "tribe", "segment", "rm", or "branch"
        public string Status { get; set; }
        public List<TargetItem> items { get; set; }
    }
}
