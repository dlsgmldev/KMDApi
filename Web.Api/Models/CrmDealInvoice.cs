using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class CrmDealInvoice
    {
        public int Id { get; set; }
        public int DealId { get; set; }
        public long Amount { get; set; }
        public int PeriodId { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string Filename { get; set; }
        public string OriginalFilename { get; set; }
        public string RootFolder { get; set; }
        public string Remarks { get; set; }
        public bool IsToBe { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? LastUpdated { get; set; }
        public int LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public int DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }

    }
}
