using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Web.Api.Core.Domain.Entities;
using Web.Api.Core.Dto;
using Web.Api.Core.Dto.GatewayResponses.Repositories;
using Web.Api.Core.Interfaces.Gateways.Repositories;
using Web.Api.Core.Specifications;
using Web.Api.Infrastructure.Identity;

namespace Web.Api.Infrastructure.Data.Repositories
{
    internal sealed class UserRepository : EfRepository<User>, IUserRepository
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IMapper _mapper;
        string urlpath = "";

        public UserRepository(UserManager<AppUser> userManager, IMapper mapper, AppDbContext appDbContext) : base(appDbContext)
        {
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task<CreateUserResponse> Create(string firstName, string email, string userName, string password)
        {
            var appUser = new AppUser { Email = email, UserName = userName };
            var identityResult = await _userManager.CreateAsync(appUser, password);

            if (!identityResult.Succeeded) return new CreateUserResponse(appUser.Id, false, identityResult.Errors.Select(e => new Error(e.Code, e.Description)));

            var user = new User(firstName, appUser.Id, appUser.UserName, email);
            user.IsDeleted = false;
            _appDbContext.Users.Add(user);
            await _appDbContext.SaveChangesAsync();

            return new CreateUserResponse(appUser.Id, identityResult.Succeeded, identityResult.Succeeded ? null : identityResult.Errors.Select(e => new Error(e.Code, e.Description)));
        }

        public async Task<User> FindByName(string userName)
        {
            if (userName.Contains("@"))
            {
                var appUser = _userManager.Users.FirstOrDefault(a => a.Email == userName /*&& a.EmailConfirmed==true*/);

                return appUser == null ? null : _mapper.Map(appUser, await GetSingleBySpec(new UserSpecification(appUser.Id)));
            }
            else
            {
                var appUser = _userManager.Users.FirstOrDefault(a => a.UserName == userName /*&& a.EmailConfirmed == true*/);
                return appUser == null ? null : _mapper.Map(appUser, await GetSingleBySpec(new UserSpecification(appUser.Id)));
            }
        }

        public async Task<bool> FindByNameDeviceID(int UserID, int deviceID,int sourceID)
        {
            RefreshToken user = _appDbContext.RefreshTokens.FirstOrDefault(a => a.UserId == UserID && a.DeviceID == deviceID && a.SourceID==sourceID);

            if (user != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public int[] FindSegmentBranchId(int userId)
        {
            CrmRelManager manager = _appDbContext.CrmRelManagers.Where(a => a.UserId == userId && a.isActive && !a.IsDeleted).FirstOrDefault();
            if(manager == null || manager.Id == 0)
            {
                return new[] { 0, 0 };
            }
            return new[] { manager.SegmentId, manager.BranchId };

        }

        

        public async Task<bool> UpdateUser(int UserID, int deviceID, int sourceID, string token, string remoteIpAddress, double secondsToExpire, string accessToken, string oS, bool isLogin, bool isNotification, DateTime accessTokenValidity, int versionCode, string versionName)
        {
            RefreshToken user = _appDbContext.RefreshTokens.FirstOrDefault(a => a.UserId == UserID && a.DeviceID == deviceID && a.SourceID==sourceID);

            user.Token = token;
            user.RemoteIpAddress = remoteIpAddress;
            user.AccessToken = accessToken;
            user.OS = oS;
            user.IsLogin = isLogin;
            user.IsNotification = isNotification;
            user.AccessTokenValidity = accessTokenValidity;
            user.VersionCode = versionCode;
            user.VersionName = versionName;
            user.SourceID = sourceID;
            user.Expires = DateTime.Now.AddSeconds(secondsToExpire);

            try
            {
                await _appDbContext.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException e)
            {
                return false;
            }
        }

        public async Task<bool> UpdateToken(int UserID, string token, string accessToken, int secondsToExpire)
        {
            RefreshToken user = _appDbContext.RefreshTokens.FirstOrDefault(a => a.UserId == UserID);

            user.Token = token;
            user.AccessToken = accessToken;
            user.Expires = DateTime.Now.AddSeconds(secondsToExpire);

            try
            {
                await _appDbContext.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException e)
            {
                return false;
            }
        }



        public async Task<bool> CheckPassword(User user, string password, int usingemail)
        {
            /*
            if (usingemail == 1)
            {
                var appUser = _userManager.Users.FirstOrDefault(a => a.UserName == (user.UserName));

                return await _userManager.CheckPasswordAsync(_mapper.Map<AppUser>(appUser), password);
            }
            else
            {
                return await _userManager.CheckPasswordAsync(_mapper.Map<AppUser>(user), password);
            }
            */
            // saya edit jadi begini aja
            return await _userManager.CheckPasswordAsync(_mapper.Map<AppUser>(user), password);
        }

        public async Task<string> FindByFile(int userid)
        {
            var files = _appDbContext.vProfileImage.FirstOrDefault(a => a.Id == userid && a.IsDeleted == false);
            if (files != null)
            {
                return files.FileURL + files.FileName;
            }
            return "";
        }
        public async Task<bool> FindRole(int userid)
        {
            var files = _appDbContext.Users.FirstOrDefault(a => a.Id == userid /*&& a.RoleID==2*/);
            if (files != null)
            {
                return true;
            }
            else
            {

                return false;
            }
        }

        

    }
}
