using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class CrmDealHistory
    {
        public int Id { get; set; }
        public int DealId { get; set; }
        public int TypeId { get; set; }
        public string PrevData { get; set; }
        public string CurData { get; set; }
        public DateTime ActionDate { get; set; }
        public int ActionBy { get; set; }
        public string Header1 { get; set; }
        public string Header2 { get; set; }
        public string Header3 { get; set; }
        public int HeaderId1 { get; set; }
        public int HeaderId2 { get; set; }
        public int HeaderId3 { get; set; }
        public string Remarks { get; set; }
        public long RemarksValue { get; set; }
        public string ReservedText { get; set; }
        public int ReservedId { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
    }
}
