using System;
using System.Threading.Tasks;
using Web.Api.Core.Dto;
using Web.Api.Core.Dto.UseCaseRequests;
using Web.Api.Core.Dto.UseCaseResponses;
using Web.Api.Core.Interfaces;
using Web.Api.Core.Interfaces.Gateways.Repositories;
using Web.Api.Core.Interfaces.Services;
using Web.Api.Core.Interfaces.UseCases;

namespace Web.Api.Core.UseCases
{
    public sealed class LoginUseCase : ILoginUseCase
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtFactory _jwtFactory;
        private readonly ITokenFactory _tokenFactory;
        public LoginUseCase(IUserRepository userRepository, IJwtFactory jwtFactory, ITokenFactory tokenFactory)
        {
            _userRepository = userRepository;
            _jwtFactory = jwtFactory;
            _tokenFactory = tokenFactory;
        }

        public async Task<bool> Handle(LoginRequest message, IOutputPort<LoginResponse> outputPort)
        {
            var myTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var currentDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, myTimeZone);
            int usingemail = 0;
            if (!string.IsNullOrEmpty(message.UserName) && !string.IsNullOrEmpty(message.Password))
            {
                // ensure we have a user with the given user name
                var user = await _userRepository.FindByName(message.UserName);
                if (user != null && user.IsActive)
                {
                    // validate password
                    if (message.UserName.Contains("@"))
                    {
                        usingemail = 1;
                    }
                    else
                    {
                        usingemail = 0;
                    }

                    var secondsToExpire = 28800;        // 8 hours

                    if (await _userRepository.CheckPassword(user, message.Password, usingemail))
                    {
                        if (await _userRepository.FindByNameDeviceID(user.Id, message.DeviceID,message.SourceID))
                        {
                            var refreshToken = _tokenFactory.GenerateToken();
                            var accessToken = await _jwtFactory.GenerateEncodedToken(user.IdentityId, user.UserName, user.RoleID);


                            var files = await _userRepository.FindByFile(user.Id);

                            /*
                            var roleid = await _userRepository.FindRole(user.Id);
                            if (roleid==false)
                            {
                                outputPort.Handle(new LoginResponse(new[] { new Error("login_failure", "You Need to login using Admin Role") }));
                                return false;
                            }
                            */
                            int[] sb = _userRepository.FindSegmentBranchId(user.Id);
                            await _userRepository.UpdateUser(user.Id, message.DeviceID, message.SourceID, refreshToken, message.RemoteIpAddress, secondsToExpire, accessToken.Token.ToString(), message.OS, true, false, currentDateTime.AddSeconds(accessToken.ExpiresIn), message.VersionCode, message.VersionName);

                            // generate access token
                            outputPort.Handle(new LoginResponse(accessToken, refreshToken, user.Id, user.RoleID, user.TribeId, user.PlatformId, sb[0], sb[1], user.UserName, files, user.Email, true));
                        }
                        else
                        {
                            // generate refresh token
                            var refreshToken = _tokenFactory.GenerateToken();
                            var accessToken = await _jwtFactory.GenerateEncodedToken(user.IdentityId, user.UserName, user.RoleID);
                            user.AddRefreshToken(refreshToken, user.Id, message.RemoteIpAddress, secondsToExpire, accessToken.Token.ToString(), message.OS, message.DeviceID, message.SourceID, true, false, currentDateTime, currentDateTime.AddSeconds(accessToken.ExpiresIn), message.VersionCode, message.VersionName);
                            await _userRepository.Update(user);
                            var files = await _userRepository.FindByFile(user.Id);
                            int[] sb = _userRepository.FindSegmentBranchId(user.Id);

                            // generate access token
                            outputPort.Handle(new LoginResponse(accessToken, refreshToken, user.Id, user.RoleID, user.TribeId, user.PlatformId, sb[0], sb[1], user.UserName, files, user.Email, true));
                        }

                        return true;
                    }
                }
            }
            outputPort.Handle(new LoginResponse(new[] { new Error("login_failure", "Invalid username or password.") }));
            return false;
        }
    }
}
