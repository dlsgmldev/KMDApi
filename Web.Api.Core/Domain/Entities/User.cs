using System;
using System.Collections.Generic;
using System.Linq;
using Web.Api.Core.Shared;


namespace Web.Api.Core.Domain.Entities
{
    public class User : BaseEntity
    {
        public string FirstName { get; private set; } // EF migrations require at least private setter - won't work on auto-property
        public string IdentityId { get; private set; }
        public string UserName { get; set; } // Required by automapper
        public string Email { get; private set; }
        public string PasswordHash { get; private set; }
        public int RoleID { get; private set; }
        public int TribeId { get; set; }
        public int PlatformId { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsActive { get; set; }


        private readonly List<RefreshToken> _refreshTokens = new List<RefreshToken>();
        public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

        internal User() { /* Required by EF */ }

        internal User(string firstName, string identityId, string userName, string email)
        {
            FirstName = firstName;
            IdentityId = identityId;
            UserName = userName;
            Email = email;
        }

        public bool HasValidRefreshToken(string refreshToken)
        {
            RefreshToken token = _refreshTokens.First(rt => rt.Token == refreshToken);
            return token.Active;
            //return _refreshTokens.Any(rt => rt.Token == refreshToken && rt.Active);
        }

        public void AddRefreshToken(string token, int userId, string remoteIpAddress, int secondsToExpire, string accessToken, string oS, int deviceID, int sourceID, bool isLogin, bool isNotification, DateTime lastLogin, DateTime accessTokenValidity, int versionCode, string versionName)
        {
            _refreshTokens.Add(new RefreshToken(token, DateTime.UtcNow.AddSeconds(secondsToExpire), userId, remoteIpAddress, accessToken, oS, deviceID, sourceID, isLogin, isNotification, lastLogin, accessTokenValidity, versionCode, versionName));
        }

        public void RemoveRefreshToken(string refreshToken)
        {
            _refreshTokens.Remove(_refreshTokens.First(t => t.Token == refreshToken));
        }
    }
   
}
