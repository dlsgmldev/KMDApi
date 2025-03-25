using KDMApi.Models.Crm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class PostPricingResponse
    {
        public int Id { get; set; }
        public int DealId { get; set; }
        public string Filename { get; set; }

        public List<Error> Errors { get; set; }
        public PostPricingResponse()
        {
            Errors = new List<Error>();
        }
    }
}
