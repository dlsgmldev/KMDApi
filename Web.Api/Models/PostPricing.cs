using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class PostPricing
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int DealId { get; set; }
        public string Filename { get; set; }
        public string FileBase64 { get; set; }
    }
}
