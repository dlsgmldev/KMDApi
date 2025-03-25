using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
namespace KDMApi.Models
{
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public bool IsLogin { get; set; }
        public DateTime LastLogin { get; set; }
        public string AccessToken { get; set; }
        public DateTime AccessTokenValidity { get; set; }
        public bool IsNotification { get; set; }
        public int DeviceID { get; set; }
    }

    public class RefreshToken_Response
    {
        [Key]
        public int UserId { get; set; }
        public int DeviceID { get; set; }
    }

    public class Token_Request
    {
        [Key]
        public string AccessToken{ get; set; }
        
    }
}
