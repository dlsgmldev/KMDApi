using System.Collections.Generic;
using Web.Api.Core.Interfaces;

namespace Web.Api.Core.Dto.UseCaseResponses
{
    public class LoginResponse : UseCaseResponseMessage
    {
        public AccessToken AccessToken { get; }
        public string RefreshToken { get; }
        public int ID { get; }
        public int RoleId { get; }
        public int TribeId { get; }
        public int PlatformId { get; }
        public int SegmentId { get; }
        public int BranchId { get; }
        public string UserName { get; }
        public string FullPath { get; }
        public string Email { get; }
        public IEnumerable<Error> Errors { get; }

        public LoginResponse(IEnumerable<Error> errors, bool success = false, string message = null) : base(success, message)
        {
            Errors = errors;
        }

        public LoginResponse(AccessToken accessToken, string refreshToken,int iD, int roleId, int tribeId, int platformId, int segmentId, int branchId, string userName,string fullPath,string email, bool success = false, string message = null) : base(success, message)
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
            FullPath=fullPath;
            Email = email;

        }
    }
}
