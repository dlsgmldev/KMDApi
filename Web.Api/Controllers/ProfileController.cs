using AutoMapper;
using CsvHelper.Configuration;
using CsvHelper;
using KDMApi.DataContexts;
using KDMApi.Models.Crm;
using KDMApi.Models.Helper;
using KDMApi.Models.Km;
using KDMApi.Models;
using KDMApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System;
using Web.Api.Core.Interfaces.Gateways.Repositories;
using Web.Api.Infrastructure.Identity;
using Web.Api.Utils;


namespace KDMApi.Controllers
{
    [Authorize(Policy = "ApiUser")]
    [Produces("application/json")]
    [Route("v1/[controller]")]
    [ApiController]
    [EnableCors("QuBisaPolicy")]
    public class ProfileController : ControllerBase
    {
        private readonly DefaultContext _context;
        private readonly IMapper _mapper;
        private readonly UserManager<AppUser> _userManager;
        private readonly IUserRepository _userRepository;
        private DataOptions _options;

        public ProfileController(UserManager<AppUser> userManager, IUserRepository userRepository, IMapper mapper, DefaultContext context, Microsoft.Extensions.Options.IOptions<DataOptions> options)
        {
            _context = context;
            _userManager = userManager;
            _mapper = mapper;
            _userRepository = userRepository;
            _options = options.Value;
        }


        /**
         * @api {put} /Profile/changepassword Ganti password
         * @apiVersion 1.0.0
         * @apiName ChangePassword
         * @apiGroup Profile
         * @apiPermission Bearer token
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "oldPassword": "string",
         *     "newPassword": "string"
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   {
         *     "code": "ok",
         *     "description": "Password successfully changed."
         *   }
         * 
         * @apiErrorExample {json} Error-Response 
         *   {
         *     "code": "new_password",
         *     "description": "New password cannot be the same with the old password."
         *   }
         *   
         * @apiErrorExample {json} Error-Response 
         *   {
         *     "code": "old_password",
         *     "description": "The current password is not valid."
         *   }
         *   
         * @apiError NotAuthorized Token salah.
         */
        //[Authorize(Policy = "ApiUser")]
        [AllowAnonymous]
        [HttpPut("changepassword")]
        public async Task<ActionResult<Error>> ChangePassword(ChangePasswordRequest request)
        {
            if (request.oldPassword == request.newPassword)
            {
                return new Error("new_password", "New password cannot be the same with the old password.");
            }

            string token = Request.Headers["Authorization"].ToString();

            var accessToken = _context.RefreshTokens.FirstOrDefault(a => "Bearer " + a.AccessToken == token);

            if (accessToken == null)
            {
                return Unauthorized();

            }

            var users = _context.Users.FirstOrDefault(a => a.ID == accessToken.UserId);
            if (users != null)
            {
                var user = await _userRepository.FindByName(users.UserName);

                if (await _userRepository.CheckPassword(user, request.oldPassword, 1))
                {
                    // await _userRepository.ChangePassword(user, request.oldPassword, request.newPassword);
                    string newpass = _userManager.PasswordHasher.HashPassword(_mapper.Map<AppUser>(user), request.newPassword);
                    AspNetUser user1 = _context.AspNetUsers.FirstOrDefault(a => a.UserName == user.UserName);
                    user1.PasswordHash = newpass;
                    _context.Entry(user1).State = EntityState.Modified;

                    try
                    {
                        await _context.SaveChangesAsync();
                        return new Error("ok", "Password successfully changed.");
                    }
                    catch (DbUpdateConcurrencyException e)
                    {
                        return new Error("error", "Error in updating the password.");
                    }
                    
                }
                else
                {
                    return new Error("old_password", "The current password is not valid.");
                }
            }
            else
            {
                return NotFound();
            }
        }

        [Authorize(Policy = "ApiUser")]
        [HttpGet("resetall")]
        public async Task<ActionResult<Error>> ResetAllPasswords()
        {
            string defaultPassword = "GMLOneTeam911";
            List<User> users = _context.Users.Where(a => a.IsActive).ToList();
            foreach (User u in users)
            {
                await ResetPassword(u.IdentityId, defaultPassword);
            }

            return new Error("ok", "Password successfully changed.");
        }

        // GET api/profile/logout
        /**
         * @api {put} /Profile/logout Logout
         * @apiVersion 1.0.0
         * @apiName SignOut
         * @apiGroup Profile
         * @apiPermission ApiUser
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *     "code": "ok",
         *     "description": "Logout successful."
         *   }
         * 
         * @apiErrorExample {json} Error-Response 
         *   {
         *     "code": "error",
         *     "description": "Error in logging out."
         *   }
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("logout")]
        public async Task<ActionResult<Error>> SignOut()
        {
            string token = Request.Headers["Authorization"].ToString();

            var refreshTokens = _context.RefreshTokens.FirstOrDefault(a => "Bearer " + a.AccessToken == token);

            if (refreshTokens == null)
            {
                return Unauthorized();

            }
            
            refreshTokens.IsLogin = false;
            refreshTokens.LastLogin = DateTime.Now;
            refreshTokens.AccessToken = "";
            refreshTokens.AccessTokenValidity = DateTime.Now;
            refreshTokens.IsNotification = false;

            _context.Entry(refreshTokens).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException e)
            {
                return new Error("error", "Error in logging out");
                //throw;
            }

            return new Error("ok", "Logout successful");
        }

        [Authorize(Policy = "ApiUser")]
        [HttpGet("reset/{id}")]
        public async Task<ActionResult<Error>> ResetPassword(string id, string defaultPassword)     // id is IdentityId
        {
            //ser user = _context.Users.Where(a => a.ID == id).FirstOrDefault();
            var user = _userManager.Users.FirstOrDefault(a => a.Id == id);

            if(user == null)
            {
                return NotFound();
            }

            //string defaultPassword = "123456";

            string newpass = _userManager.PasswordHasher.HashPassword(_mapper.Map<AppUser>(user), defaultPassword);
            AspNetUser user1 = _context.AspNetUsers.FirstOrDefault(a => a.UserName == user.UserName);
            user1.PasswordHash = newpass;
            _context.Entry(user1).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return new Error("ok", "Password successfully changed.");
            }
            catch (DbUpdateConcurrencyException e)
            {
                return BadRequest(new { Error = "Error in updating the password." });
            }


            return new Error("ok", "Password successfully changed.");
        }

        [HttpGet("picture/{filename}")]
        public IActionResult GetProfilePicture(string filename)
        {
            try
            {
                var fileExt = System.IO.Path.GetExtension(filename).Substring(1).ToLower();
                var bytes = System.IO.File.ReadAllBytes(Path.Combine(_options.DataRootDirectory, @"images", @"profile", filename));
                var contentType = fileExt.EndsWith(".png") ? "image/png" : "image/jpeg";
                return File(bytes, contentType, "profile" + fileExt);
            }
            catch
            {
                return NotFound();
            }


        }


    }
}
