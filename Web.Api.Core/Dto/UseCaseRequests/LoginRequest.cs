using Web.Api.Core.Dto.UseCaseResponses;
using Web.Api.Core.Interfaces;

namespace Web.Api.Core.Dto.UseCaseRequests
{
    public class LoginRequest : IUseCaseRequest<LoginResponse>
    {
        public string UserName { get; }
        public string Password { get; }
        public string RemoteIpAddress { get; }
        public string OS { get; }
        public int DeviceID { get; }
        public int VersionCode { get; }
        public string VersionName { get; }
        public int SourceID { get; }

        public LoginRequest(string userName, string password, string remoteIpAddress, string oS, int deviceID, int sourceID, int versionCode, string versionName)
        {
            UserName = userName;
            Password = password;
            RemoteIpAddress = remoteIpAddress;
            OS = oS;
            DeviceID = deviceID;
            VersionCode = versionCode;
            VersionName = versionName;
            SourceID = sourceID;
        }
    }
}
