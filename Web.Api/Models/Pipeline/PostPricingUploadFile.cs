using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class PostPricingUploadFile
    {
        public int DocumentType { get; set; }
        public int UserId { get; set; }
        public int DealId { get; set; }
        public List<IFormFile> Files { get; set; }
    }
}
