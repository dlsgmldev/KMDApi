using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Web.Api.Core.Domain.Entities;
using Web.Api.Core.Dto;
using Web.Api.Core.Interfaces.Services;
using Web.Api.Infrastructure.Interfaces;

namespace Web.Api.Infrastructure.Auth
{
    internal sealed class JwtFactory : IJwtFactory
    {
        private readonly IJwtTokenHandler _jwtTokenHandler;
        private readonly JwtIssuerOptions _jwtOptions;

        internal JwtFactory(IJwtTokenHandler jwtTokenHandler, IOptions<JwtIssuerOptions> jwtOptions)
        {
            _jwtTokenHandler = jwtTokenHandler;
            _jwtOptions = jwtOptions.Value;
            ThrowIfInvalidOptions(_jwtOptions);
        }

        public async Task<AccessToken> GenerateEncodedToken(string id, string userName, int roleId)
        {
            
            var identity = GenerateClaimsIdentity(id, userName);

            switch (roleId)
            {
                case 1:         // superadmin
                    var claims1 = new Claim[]
                    {
                         new Claim(JwtRegisteredClaimNames.Sub, userName),
                         new Claim(JwtRegisteredClaimNames.Jti, await _jwtOptions.JtiGenerator()),
                         new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(_jwtOptions.IssuedAt).ToString(), ClaimValueTypes.Integer64),
                         new Claim("SuperAdminOnly", "1"),
                         new Claim("CanDownloadClient", "1"),
                         new Claim("CanUpdateCreateClient", "1"),
                         new Claim("CanReadClient", "1"),
                         new Claim("CanDeleteClient", "1"),
                         new Claim("CanDownloadArticle", "1"),
                         new Claim("CanUpdateCreateArticle", "1"),
                         new Claim("CanReadArticle", "1"),
                         new Claim("CanDeleteArticle", "1"),
                         new Claim("CanDownloadProject", "1"),
                         new Claim("CanUpdateCreateProject", "1"),
                         new Claim("CanReadProject", "1"),
                         new Claim("CanDeleteProject", "1"),
                         identity.FindFirst(Helpers.Constants.Strings.JwtClaimIdentifiers.Rol),
                         identity.FindFirst(Helpers.Constants.Strings.JwtClaimIdentifiers.Id)
                    };
                    var jwt1 = new JwtSecurityToken(
                        _jwtOptions.Issuer,
                        _jwtOptions.Audience,
                        claims1,
                        _jwtOptions.NotBefore,
                        _jwtOptions.Expiration,
                        _jwtOptions.SigningCredentials);

                    return new AccessToken(_jwtTokenHandler.WriteToken(jwt1), (int)_jwtOptions.ValidFor.TotalSeconds);

                case 2:         // chief of tribe
                    var claims2 = new Claim[]
                    {
                         new Claim(JwtRegisteredClaimNames.Sub, userName),
                         new Claim(JwtRegisteredClaimNames.Jti, await _jwtOptions.JtiGenerator()),
                         new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(_jwtOptions.IssuedAt).ToString(), ClaimValueTypes.Integer64),
                         new Claim("SuperAdminOnly", "0"),
                         new Claim("CanDownloadClient", "0"),
                         new Claim("CanUpdateCreateClient", "1"),
                         new Claim("CanReadClient", "1"),
                         new Claim("CanDeleteClient", "0"),
                         new Claim("CanDownloadArticle", "1"),
                         new Claim("CanUpdateCreateArticle", "1"),
                         new Claim("CanReadArticle", "1"),
                         new Claim("CanDeleteArticle", "0"),
                         new Claim("CanDownloadProject", "1"),
                         new Claim("CanUpdateCreateProject", "1"),
                         new Claim("CanReadProject", "1"),
                         new Claim("CanDeleteProject", "0"),
                         identity.FindFirst(Helpers.Constants.Strings.JwtClaimIdentifiers.Rol),
                         identity.FindFirst(Helpers.Constants.Strings.JwtClaimIdentifiers.Id)
                    };
                    var jwt2 = new JwtSecurityToken(
                        _jwtOptions.Issuer,
                        _jwtOptions.Audience,
                        claims2,
                        _jwtOptions.NotBefore,
                        _jwtOptions.Expiration,
                        _jwtOptions.SigningCredentials);

                    return new AccessToken(_jwtTokenHandler.WriteToken(jwt2), (int)_jwtOptions.ValidFor.TotalSeconds);

                case 3:         // admin
                    var claims3 = new Claim[]
                    {
                         new Claim(JwtRegisteredClaimNames.Sub, userName),
                         new Claim(JwtRegisteredClaimNames.Jti, await _jwtOptions.JtiGenerator()),
                         new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(_jwtOptions.IssuedAt).ToString(), ClaimValueTypes.Integer64),
                         new Claim("SuperAdminOnly", "0"),
                         new Claim("CanDownloadClient", "0"),
                         new Claim("CanUpdateCreateClient", "1"),
                         new Claim("CanReadClient", "1"),
                         new Claim("CanDeleteClient", "0"),
                         new Claim("CanDownloadArticle", "1"),
                         new Claim("CanUpdateCreateArticle", "1"),
                         new Claim("CanReadArticle", "1"),
                         new Claim("CanDeleteArticle", "1"),
                         new Claim("CanDownloadProject", "1"),
                         new Claim("CanUpdateCreateProject", "1"),
                         new Claim("CanReadProject", "1"),
                         new Claim("CanDeleteProject", "1"),
                         identity.FindFirst(Helpers.Constants.Strings.JwtClaimIdentifiers.Rol),
                         identity.FindFirst(Helpers.Constants.Strings.JwtClaimIdentifiers.Id)
                    };
                    var jwt3 = new JwtSecurityToken(
                        _jwtOptions.Issuer,
                        _jwtOptions.Audience,
                        claims3,
                        _jwtOptions.NotBefore,
                        _jwtOptions.Expiration,
                        _jwtOptions.SigningCredentials);

                    return new AccessToken(_jwtTokenHandler.WriteToken(jwt3), (int)_jwtOptions.ValidFor.TotalSeconds);

                case 4:         // consultant
                    var claims4 = new Claim[]
                    {
                         new Claim(JwtRegisteredClaimNames.Sub, userName),
                         new Claim(JwtRegisteredClaimNames.Jti, await _jwtOptions.JtiGenerator()),
                         new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(_jwtOptions.IssuedAt).ToString(), ClaimValueTypes.Integer64),
                         new Claim("SuperAdminOnly", "0"),
                         new Claim("CanDownloadClient", "0"),
                         new Claim("CanUpdateCreateClient", "1"),
                         new Claim("CanReadClient", "1"),
                         new Claim("CanDeleteClient", "0"),
                         new Claim("CanDownloadArticle", "1"),
                         new Claim("CanUpdateCreateArticle", "1"),
                         new Claim("CanReadArticle", "1"),
                         new Claim("CanDeleteArticle", "0"),
                         new Claim("CanDownloadProject", "1"),
                         new Claim("CanUpdateCreateProject", "1"),
                         new Claim("CanReadProject", "1"),
                         new Claim("CanDeleteProject", "0"),
                         identity.FindFirst(Helpers.Constants.Strings.JwtClaimIdentifiers.Rol),
                         identity.FindFirst(Helpers.Constants.Strings.JwtClaimIdentifiers.Id)
                    };
                    var jwt4 = new JwtSecurityToken(
                        _jwtOptions.Issuer,
                        _jwtOptions.Audience,
                        claims4,
                        _jwtOptions.NotBefore,
                        _jwtOptions.Expiration,
                        _jwtOptions.SigningCredentials);

                    return new AccessToken(_jwtTokenHandler.WriteToken(jwt4), (int)_jwtOptions.ValidFor.TotalSeconds);

                case 5:         // sales
                    var claims5 = new Claim[]
                    {
                         new Claim(JwtRegisteredClaimNames.Sub, userName),
                         new Claim(JwtRegisteredClaimNames.Jti, await _jwtOptions.JtiGenerator()),
                         new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(_jwtOptions.IssuedAt).ToString(), ClaimValueTypes.Integer64),
                         new Claim("SuperAdminOnly", "0"),
                         new Claim("CanDownloadClient", "0"),
                         new Claim("CanUpdateCreateClient", "1"),
                         new Claim("CanReadClient", "1"),
                         new Claim("CanDeleteClient", "1"),
                         new Claim("CanDownloadArticle", "1"),
                         new Claim("CanUpdateCreateArticle", "1"),
                         new Claim("CanReadArticle", "1"),
                         new Claim("CanDeleteArticle", "0"),
                         new Claim("CanDownloadProject", "1"),
                         new Claim("CanUpdateCreateProject", "1"),
                         new Claim("CanReadProject", "1"),
                         new Claim("CanDeleteProject", "0"),
                         identity.FindFirst(Helpers.Constants.Strings.JwtClaimIdentifiers.Rol),
                         identity.FindFirst(Helpers.Constants.Strings.JwtClaimIdentifiers.Id)
                    };
                    var jwt5 = new JwtSecurityToken(
                        _jwtOptions.Issuer,
                        _jwtOptions.Audience,
                        claims5,
                        _jwtOptions.NotBefore,
                        _jwtOptions.Expiration,
                        _jwtOptions.SigningCredentials);

                    return new AccessToken(_jwtTokenHandler.WriteToken(jwt5), (int)_jwtOptions.ValidFor.TotalSeconds);


                case 6:         // regular
                    var claims6 = new Claim[]
                    {
                         new Claim(JwtRegisteredClaimNames.Sub, userName),
                         new Claim(JwtRegisteredClaimNames.Jti, await _jwtOptions.JtiGenerator()),
                         new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(_jwtOptions.IssuedAt).ToString(), ClaimValueTypes.Integer64),
                         new Claim("SuperAdminOnly", "0"),
                         new Claim("CanDownloadClient", "0"),
                         new Claim("CanUpdateCreateClient", "1"),
                         new Claim("CanReadClient", "1"),
                         new Claim("CanDeleteClient", "0"),
                         new Claim("CanDownloadArticle", "1"),
                         new Claim("CanUpdateCreateArticle", "1"),
                         new Claim("CanReadArticle", "1"),
                         new Claim("CanDeleteArticle", "0"),
                         new Claim("CanDownloadProject", "1"),
                         new Claim("CanUpdateCreateProject", "1"),
                         new Claim("CanReadProject", "1"),
                         new Claim("CanDeleteProject", "0"),
                         identity.FindFirst(Helpers.Constants.Strings.JwtClaimIdentifiers.Rol),
                         identity.FindFirst(Helpers.Constants.Strings.JwtClaimIdentifiers.Id)
                    };
                    var jwt6 = new JwtSecurityToken(
                        _jwtOptions.Issuer,
                        _jwtOptions.Audience,
                        claims6,
                        _jwtOptions.NotBefore,
                        _jwtOptions.Expiration,
                        _jwtOptions.SigningCredentials);

                    return new AccessToken(_jwtTokenHandler.WriteToken(jwt6), (int)_jwtOptions.ValidFor.TotalSeconds);


                case 7:         // kcaadmin
                    var claims7 = new Claim[]
                    {
                         new Claim(JwtRegisteredClaimNames.Sub, userName),
                         new Claim(JwtRegisteredClaimNames.Jti, await _jwtOptions.JtiGenerator()),
                         new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(_jwtOptions.IssuedAt).ToString(), ClaimValueTypes.Integer64),
                         new Claim("SuperAdminOnly", "0"),
                         new Claim("CanDownloadClient", "1"),
                         new Claim("CanUpdateCreateClient", "1"),
                         new Claim("CanReadClient", "1"),
                         new Claim("CanDeleteClient", "0"),
                         new Claim("CanDownloadArticle", "1"),
                         new Claim("CanUpdateCreateArticle", "1"),
                         new Claim("CanReadArticle", "1"),
                         new Claim("CanDeleteArticle", "0"),
                         new Claim("CanDownloadProject", "1"),
                         new Claim("CanUpdateCreateProject", "1"),
                         new Claim("CanReadProject", "1"),
                         new Claim("CanDeleteProject", "0"),
                         identity.FindFirst(Helpers.Constants.Strings.JwtClaimIdentifiers.Rol),
                         identity.FindFirst(Helpers.Constants.Strings.JwtClaimIdentifiers.Id)
                    };
                    var jwt7 = new JwtSecurityToken(
                        _jwtOptions.Issuer,
                        _jwtOptions.Audience,
                        claims7,
                        _jwtOptions.NotBefore,
                        _jwtOptions.Expiration,
                        _jwtOptions.SigningCredentials);

                    return new AccessToken(_jwtTokenHandler.WriteToken(jwt7), (int)_jwtOptions.ValidFor.TotalSeconds);
            }

            // Default
            var claims = new[]
            {
                 new Claim(JwtRegisteredClaimNames.Sub, userName),
                 new Claim(JwtRegisteredClaimNames.Jti, await _jwtOptions.JtiGenerator()),
                 new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(_jwtOptions.IssuedAt).ToString(), ClaimValueTypes.Integer64),
                 identity.FindFirst(Helpers.Constants.Strings.JwtClaimIdentifiers.Rol),
                 identity.FindFirst(Helpers.Constants.Strings.JwtClaimIdentifiers.Id)
             };

            // Create the JWT security token and encode it.
            var jwt = new JwtSecurityToken(
                _jwtOptions.Issuer,
                _jwtOptions.Audience,
                claims,
                _jwtOptions.NotBefore,
                _jwtOptions.Expiration,
                _jwtOptions.SigningCredentials);
          
            return new AccessToken(_jwtTokenHandler.WriteToken(jwt), (int)_jwtOptions.ValidFor.TotalSeconds);
        }

        private static ClaimsIdentity GenerateClaimsIdentity(string id, string userName)
        {
            return new ClaimsIdentity(new GenericIdentity(userName, "Token"), new[]
            {
                new Claim(Helpers.Constants.Strings.JwtClaimIdentifiers.Id, id),
                new Claim(Helpers.Constants.Strings.JwtClaimIdentifiers.Rol, Helpers.Constants.Strings.JwtClaims.ApiAccess)
            });
        }

        /// <returns>Date converted to seconds since Unix epoch (Jan 1, 1970, midnight UTC).</returns>
        private static long ToUnixEpochDate(DateTime date)
          => (long)Math.Round((date.ToUniversalTime() -
                               new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero))
                              .TotalSeconds);

        private static void ThrowIfInvalidOptions(JwtIssuerOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (options.ValidFor <= TimeSpan.Zero)
            {
                throw new ArgumentException("Must be a non-zero TimeSpan.", nameof(JwtIssuerOptions.ValidFor));
            }

            if (options.SigningCredentials == null)
            {
                throw new ArgumentNullException(nameof(JwtIssuerOptions.SigningCredentials));
            }

            if (options.JtiGenerator == null)
            {
                throw new ArgumentNullException(nameof(JwtIssuerOptions.JtiGenerator));
            }
        }


    }
}
