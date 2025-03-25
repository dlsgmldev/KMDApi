using System;
using System.Threading.Tasks;
using Web.Api.Core.Domain.Entities;
using Web.Api.Core.Dto.GatewayResponses.Repositories;

namespace Web.Api.Core.Interfaces.Gateways.Repositories
{
    public interface IUserRepository : IRepository<User>
    {
        Task<CreateUserResponse> Create(string firstName,  string email, string userName, string password);
        Task<User> FindByName(string userName);
        Task<bool> FindByNameDeviceID(int UserID, int deviceID,int sourceID);
        Task<bool> UpdateUser(int UserID, int deviceID, int SourceID, string token, string remoteIpAddress, double daysToExpire, string accessToken, string oS, bool isLogin, bool isNotification, DateTime accessTokenValidity, int versionCode, string versionName);
        Task<bool> UpdateToken(int UserID, string token, string accessToken, int secondsToExpire);
        Task<bool> CheckPassword(User user, string password, int usingemail);
        Task<string> FindByFile(int userid);
        Task<bool> FindRole(int userid);
        int[] FindSegmentBranchId(int userId);
    }
}
