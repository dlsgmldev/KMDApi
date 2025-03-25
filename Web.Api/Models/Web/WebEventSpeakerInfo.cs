using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Web
{
    public class WebEventSpeakerInfo
    {
        public WebEventSpeakerInfo()
        {
            Profile = "";
            ProfileFilename = "";
        }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Company { get; set; }
        public string ProfileFilename { get; set; }
        public string Profile { get; set; }
    }

    public class WebEventSpeakerSimple
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Company { get; set; }
        public string ProfileFilename { get; set; }
        public string Profile { get; set; }
        public int UserId { get; set; }
    }
}
