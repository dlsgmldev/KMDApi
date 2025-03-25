using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class User
    {
        [Key]
        public int ID { get; set; }
        public string UserName { get; set; }
        public string IdentityId { get; set; }
        public string IdNumber { get; set; }
        public bool IsActive { get; set; }
        public string JobTitle { get; set; }
        public int TribeId { get; set; }
        public int PlatformId { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Modified { get; set; }
        public string LastUpdatedBy { get; set; }
        public bool? IsDeleted { get; set; }
        public string DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
        public string FirstName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string CurrentAddress { get; set; }
        public bool? Gender { get; set; }
        public int? FileID { get; set; }
        public int RoleID { get; set; }
    }
    

}
