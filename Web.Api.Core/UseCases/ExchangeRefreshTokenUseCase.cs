using System;
using System.Linq;
using System.Threading.Tasks;
using Web.Api.Core.Dto.UseCaseRequests;
using Web.Api.Core.Dto.UseCaseResponses;
using Web.Api.Core.Interfaces;
using Web.Api.Core.Interfaces.Gateways.Repositories;
using Web.Api.Core.Interfaces.Services;
using Web.Api.Core.Interfaces.UseCases;
using Web.Api.Core.Specifications;


namespace Web.Api.Core.UseCases
{
    public sealed class ExchangeRefreshTokenUseCase : IExchangeRefreshTokenUseCase
    {
        private readonly IJwtTokenValidator _jwtTokenValidator;
        private readonly IUserRepository _userRepository;
        private readonly IJwtFactory _jwtFactory;
        private readonly ITokenFactory _tokenFactory;


        public ExchangeRefreshTokenUseCase(IJwtTokenValidator jwtTokenValidator, IUserRepository userRepository, IJwtFactory jwtFactory, ITokenFactory tokenFactory)
        {
            _jwtTokenValidator = jwtTokenValidator;
            _userRepository = userRepository;
            _jwtFactory = jwtFactory;
            _tokenFactory = tokenFactory;
        }

        public async Task<bool> Handle(ExchangeRefreshTokenRequest message, IOutputPort<ExchangeRefreshTokenResponse> outputPort)
        {
            var myTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            var currentDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, myTimeZone);
            var cp = _jwtTokenValidator.GetPrincipalFromToken(message.AccessToken, message.SigningKey);

            // invalid token/signing key was passed and we can't extract user claims
            if (cp != null)
            {
                var id = cp.Claims.First(c => c.Type == "id");
                var user = await _userRepository.GetSingleBySpec(new UserSpecification(id.Value));

                if (user.HasValidRefreshToken(message.RefreshToken))
                {
                    var jwtToken = await _jwtFactory.GenerateEncodedToken(user.IdentityId, user.UserName, user.RoleID);
                    var refreshToken = _tokenFactory.GenerateToken();
                    //user.RemoveRefreshToken(message.RefreshToken); // delete the token we've exchanged
                    //var secondsToExpire = 28800;
                    //user.AddRefreshToken(refreshToken, user.Id, "", secondsToExpire, jwtToken.Token.ToString(), message.OS, message., jwtToken.Token,false,DateTime.Now,DateTime.Now.AddSeconds(jwtToken.ExpiresIn),0,null); // add the new one
                    //(string token, int userId, string remoteIpAddress, int secondsToExpire, string accessToken, string oS, int deviceID, int sourceID, bool isLogin, bool isNotification, DateTime lastLogin, DateTime accessTokenValidity, int versionCode, string versionName)


                    //  (user.Id, message.DeviceID, message.SourceID, refreshToken, message.RemoteIpAddress, secondsToExpire, accessToken.Token.ToString(), message.OS, true, false, currentDateTime.AddSeconds(accessToken.ExpiresIn), message.VersionCode, message.VersionName);

                    var secondsToExpire = 28800;        // 8 hours
                    await _userRepository.UpdateToken(user.Id, refreshToken, jwtToken.Token.ToString(), secondsToExpire);
                    outputPort.Handle(new ExchangeRefreshTokenResponse(jwtToken, refreshToken, true));
                    return true;
                }
            }
            outputPort.Handle(new ExchangeRefreshTokenResponse(false, "Invalid token."));
            return false;
        }
    }
}
