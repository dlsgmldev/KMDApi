using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Helper
{
    public class UsernamePassword
    {
        public UsernamePassword(string u, string p, int did, string o)
        {
            Username = u;
            Password = p;
            DeviceId = did;
            Os = o;
        }
        public string Username { get; set; }
        public string Password { get; set; }
        public int DeviceId { get; set; }
        public string Os { get; set; }
    }
}
