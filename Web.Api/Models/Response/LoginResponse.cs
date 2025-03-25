 

using Web.Api.Core.Dto;

namespace Web.Api.Models.Response
{
    public class LoginResponse
    {
        public AccessToken AccessToken { get; }
        public string RefreshToken { get; }
        public int ID { get; }
        public string UserName { get; }
        public int RoleId { get; }
        public int TribeId { get; }
        public int PlatformId { get; }
        public int SegmentId { get; }
        public int BranchId { get; }
        public string ProfilePic { get; }
        public string Email { get; }

        public LoginResponse(AccessToken accessToken, string refreshToken,int iD,int roleId, int tribeId, int platformId, int segmentId, int branchId, string userName,string profilePic,string email)
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            ID = iD;
            RoleId = roleId;
            TribeId = tribeId;
            PlatformId = platformId;
            SegmentId = segmentId;
            BranchId = branchId;
            UserName = userName;
            ProfilePic = profilePic;
            Email = email;
        }
    }
}
