
namespace Web.Api.Models.Request
{
    public class LoginRequest
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string OS { get; set; }
        public int DeviceID { get; set; }
        public int SourceID { get; set; }
        public int VersionCode { get; set; }
        public string VersionName { get; set; }



    }
}
