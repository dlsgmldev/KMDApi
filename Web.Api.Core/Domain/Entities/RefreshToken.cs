using System;
using Web.Api.Core.Shared;


namespace Web.Api.Core.Domain.Entities
{
    public class RefreshToken : BaseEntity
    {
        public string Token { get; set; }
        public DateTime Expires { get; set; }
        public int UserId { get; set; }
        public bool Active => DateTime.UtcNow <= Expires;
        public string RemoteIpAddress { get; set; }
        public string OS { get; set; }
        public int VersionCode { get; set; }
        public string VersionName { get; set; }
        public bool IsLogin { get; set; }
        public bool IsNotification { get; set; }
        public string UniqueID { get; private set; }
        public DateTime LastLogin { get; set; }
        public string AccessToken { get; set; }
        public DateTime AccessTokenValidity { get; set; }
        public int DeviceID { get; set; }
        public int SourceID { get; set; }

        public RefreshToken(string token, DateTime expires, int userId, string remoteIpAddress, string accessToken, string oS, int deviceID, int sourceID, bool isLogin, bool isNotification, DateTime lastLogin, DateTime accessTokenValidity, int versionCode, string versionName)
        {
            Token = token;
            Expires = expires;
            UserId = userId;
            RemoteIpAddress = remoteIpAddress;
            AccessToken = accessToken;
            OS = oS;
            DeviceID = deviceID;
            SourceID = sourceID;
            IsLogin = isLogin;
            IsNotification = isNotification;
            LastLogin = lastLogin;
            AccessTokenValidity = accessTokenValidity;
            VersionCode = versionCode;
            VersionName = versionName;

        }


    }
}
