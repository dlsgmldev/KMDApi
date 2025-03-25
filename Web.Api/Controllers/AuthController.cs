using System;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using KDMApi.Models.Crm;
using Web.Api.Core.Dto.UseCaseRequests;
using Web.Api.Core.Interfaces.UseCases;
using Web.Api.Models.Settings;
using Web.Api.Presenters;
using Microsoft.AspNetCore.Cors;

namespace Web.Api.Controllers
{
    [Route("v1/[controller]")]
    [ApiController]
    [EnableCors("QuBisaPolicy")]
    public class AuthController : ControllerBase
    {
        private readonly ILoginUseCase _loginUseCase;
        private readonly LoginPresenter _loginPresenter;
        private readonly IExchangeRefreshTokenUseCase _exchangeRefreshTokenUseCase;
        private readonly ExchangeRefreshTokenPresenter _exchangeRefreshTokenPresenter;
        private readonly AuthSettings _authSettings;

        public AuthController(ILoginUseCase loginUseCase, LoginPresenter loginPresenter, IExchangeRefreshTokenUseCase exchangeRefreshTokenUseCase, ExchangeRefreshTokenPresenter exchangeRefreshTokenPresenter, IOptions<AuthSettings> authSettings)
        {

            _loginUseCase = loginUseCase;
            _loginPresenter = loginPresenter;
            _exchangeRefreshTokenUseCase = exchangeRefreshTokenUseCase;
            _exchangeRefreshTokenPresenter = exchangeRefreshTokenPresenter;
            _authSettings = authSettings.Value;
        }

        // POST v1/auth/login
        /**
         * @api {post} /auth/login User login
         * @apiVersion 1.0.0
         * @apiName Login
         * @apiGroup Auth
         * @apiPermission Basic authentication 
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "userName": "rifky@gmlperformance.co.id",
         *     "password": "123456",
         *     "os": "Windows",
         *     "deviceID": 1,
         *     "sourceID": 1,
         *     "versionCode": 1,
         *     "versionName": "string"
         *   }
         * 
         * @apiSuccessExample Success-Response:
         *    {
         *      "accessToken": {
         *        "token": "XYZ…",
         *        "expiresIn": 28800
         *      },
         *      "refreshToken": "Abc…",
         *      "id": 1,
         *      "userName": "rifky@gmlperformance.co.id",
         *      "roleId": 0,
         *      "profilePic": "",
         *      "email": "rifky@gmlperformance.co.id"
         *    }
         * 
         * @apiError NotAuthorized Username dan password di header salah.
         * @apiErrorExample {json} Error-Response
         *    [
         *      {
         *        "code": "login_failure",
         *        "description": "Invalid username or password."
         *      }
         *    ]
         */
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] Models.Request.LoginRequest request)
        {
            if (Request.Headers["Authorization"].ToString() != "" && Request.Headers["Authorization"].ToString().StartsWith("Basic "))
            {
                var authHeader = Request.Headers["Authorization"].ToString();
                authHeader = authHeader.Trim();
                string encodedCredentials = authHeader.Substring(6);
                var credentialBytes = Convert.FromBase64String(encodedCredentials);
                var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
                var username = credentials[0];
                var password = credentials[1];
                if (username == "onegmlapi" && password == "O1n6e0G4M7L")
                {
                    if (!ModelState.IsValid) { return BadRequest(ModelState); }
                    await _loginUseCase.Handle(new LoginRequest(request.UserName, request.Password, Request.HttpContext.Connection.RemoteIpAddress?.ToString(), request.OS, request.DeviceID, request.SourceID, request.VersionCode, request.VersionName), _loginPresenter);
                    var result = _loginPresenter.ContentResult;
                    return _loginPresenter.ContentResult;
                }
            }

            return Unauthorized();
        }

        // POST api/auth/refreshtoken
        /**
         * @api {post} /auth/refreshtoken Refresh token setelah token expires
         * @apiVersion 1.0.0
         * @apiName RefreshToken
         * @apiGroup Auth
         * @apiPermission Basic authentication 
         * @apiDescription API ini dipanggil ketika token sudah expires dengan response 401 Unauthorized.
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "accessToken": "eydjaisja...",
         *     "refreshToken": "dJW..."
         *   }
         * 
         * @apiSuccessExample Success-Response:
         *  {
         *    "accessToken": {
         *      "token": "ey...",
         *      "expiresIn": 28800
         *    },
         *    "refreshToken": "ABC..."
         *  }
         * 
         * @apiError NotAuthorized Username dan password di header salah.
         */
        [AllowAnonymous]
        [HttpPost("refreshtoken")]
        public async Task<ActionResult> RefreshToken([FromBody] Models.Request.ExchangeRefreshTokenRequest request)
        {
            if (Request.Headers["Authorization"].ToString() != "" && Request.Headers["Authorization"].ToString().StartsWith("Basic "))
            {
                var authHeader = Request.Headers["Authorization"].ToString();
                authHeader = authHeader.Trim();
                string encodedCredentials = authHeader.Substring(6);
                var credentialBytes = Convert.FromBase64String(encodedCredentials);
                var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
                var username = credentials[0];
                var password = credentials[1];
                if (username == "onegmlapi" && password == "O1n6e0G4M7L")
                {
                    if (!ModelState.IsValid) { return BadRequest(ModelState); }
                    await _exchangeRefreshTokenUseCase.Handle(new ExchangeRefreshTokenRequest(request.AccessToken, request.RefreshToken, _authSettings.SecretKey), _exchangeRefreshTokenPresenter);
                    return _exchangeRefreshTokenPresenter.ContentResult;
                }
            }
            return Unauthorized();
        }


    }
}
