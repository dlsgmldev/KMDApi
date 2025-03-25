using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class AspNetUser
    {
        [Key]
        public string Id { get; set; }
        public string UserName  { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
    }
}
