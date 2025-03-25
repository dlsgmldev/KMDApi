using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Web
{
    public class WebContactRegister
    {
        public WebContactRegister()
        {
            ReferenceFrom = "";
            LastUpdated = new DateTime(1970, 1, 1);
            IsDeleted = false;
            DeletedBy = 0;
            DeletedDate = new DateTime(1970, 1, 1);
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string CompanyName { get; set; }
        public string JobTitle { get; set; }
        public string Department { get; set; }
        public string Message { get; set; }
        public string Action { get; set; }
        public string Voucher { get; set; }
        public string ReferenceFrom { get; set; }
        public DateTime? LastUpdated { get; set; }
        public int LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public int DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
    }
}
