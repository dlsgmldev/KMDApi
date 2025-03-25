using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class WebEventRegistration
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public string Company { get; set; }
        public string CompanyType { get; set; }
        public string StatusPPN { get; set; }
        public string NPWP { get; set; }
        public string Address { get; set; }
        public string ContactPerson { get; set; }
        public string Telephone { get; set; }
        public string Fax { get; set; }
        public string Handphone { get; set; }
        public string Email { get; set; }
        public string MailAddress { get; set; }
        public string City { get; set; }
        public string Kecamatan { get; set; }
        public string PostCode { get; set; }
        public string Voucher { get; set; }
        public string Reference { get; set; }
        public string JenisPelatihan { get; set; }
        public string KeteranganPembayaran { get; set; }
        public string Signature { get; set; }
        public int Payment { get; set; }            
        public DateTime LastUpdated { get; set; }
        public int LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public int DeletedBy { get; set; }
        public DateTime DeletedDate { get; set; }
    }
}
