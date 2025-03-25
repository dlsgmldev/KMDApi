using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Helper
{
    public class DataOptions
    {
        public string ProfilePictureControllerRoute { get; set; }
        public string DataRootDirectory { get; set; }
        public string AssetsBaseURL { get; set; }
        public string AssetsRootDirectory { get; set; }
        public string DocViewerBaseURL { get; set; }
        public string QuBisaAPIBaseURL { get; set; }
        public string QuBisaAPIUsername { get; set; }
        public string QuBisaAPIPassword { get; set; }
        public string QuBisaBasicUsername { get; set; }
        public string QuBisaBasicPassword { get; set; }
        public string QuBisaForumLoginURL { get; set; }
        public int ChannelId { get; set; }
        public string Environment { get; set; }
        public string ElasticUsername { get; set; }
        public string ElasticPassword { get; set; }
    }
}
