using KDMApi.Models.Crm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class CrmClient
    {
        public int Id { get; set; }
        public string Company { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string Phone { get; set; }
        public string Fax { get; set; }
        public string Website { get; set; }
        public string Remarks { get; set; }
        public string Source { get; set; }
        public int CrmIndustryId { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime LastUpdated { get; set; }
        public int LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public int DeletedBy { get; set; }
        public DateTime DeletedDate { get; set; }

        /*public virtual CrmIndustry CrmIndustry { get; set; }
        public virtual ICollection<CrmContact> Contacts { get; set; }
        public virtual ICollection<CrmClientRelManager> ClientRelManagers { get; set; }
        */
    }
}
