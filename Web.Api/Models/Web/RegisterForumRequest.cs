using KDMApi.Models.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Web.Api.Models.Request;

namespace KDMApi.Models.Web
{
    public class RegisterForumRequest
    {
        public int UserId { get; set; }
        public int ChannelId { get; set; }
        public string ForumName { get; set; }
        public string Description { get; set; }
        public List<RegisterUserRequest> Users { get; set; }
        public List<RegisterUserRequest> Experts { get; set; }
    }
}
