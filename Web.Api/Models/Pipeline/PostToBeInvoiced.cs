using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class PostToBeInvoiced
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public int DealId { get; set; }
        public int UserId { get; set; }
        public int PicId { get; set; }
        public DateTime InvoiceDate { get; set; }
        public long Amount { get; set; }
        public string Remarks { get; set; }
    }
}
