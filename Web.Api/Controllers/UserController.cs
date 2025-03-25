using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KDMApi.DataContexts;
using KDMApi.Models.Km;
using KDMApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KDMApi.Controllers
{
    [Route("v1/[controller]")]
    [ApiController]
    [EnableCors("QuBisaPolicy")]
    public class UserController : ControllerBase
    {
        // Kalau user itu RM, dia punya SegmentId dan BranchId
        // User lainnya punya TribeId atau PlatformId

        private readonly DefaultContext _context;
        public UserController(DefaultContext context)
        {
            _context = context;
        }

        [Authorize(Policy = "ApiUser")]
        [HttpGet("id")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            if(!UserExists(id))
            {
                return NotFound();
            }

            return await _context.Users.FindAsync(id);
        }

        /**
         * @api {post} /user/trainer POST trainer
         * @apiVersion 1.0.0
         * @apiName PostTrainer
         * @apiGroup User
         * @apiPermission ApiUser
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "name": "Rafdi",
         *     "userId" 16
         *   }
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *     "id": 84,
         *     "text": "Rafdi"
         *   }
         * 
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost("trainer")]
        public async Task<ActionResult<GenericInfo>> PostTrainer(AddTrainer trainer)
        {
            DateTime now = DateTime.Now;

            User user = new User()
            {
                UserName = "",
                IdentityId = "",
                IdNumber = "",
                IsActive = true,
                JobTitle = "Trainer",
                TribeId = 0,
                PlatformId = 0,
                Created = now,
                Modified = now,
                LastUpdatedBy = trainer.UserId.ToString(),
                IsDeleted = false,
                DeletedBy = 0.ToString(),
                DeletedDate = new DateTime(1970, 1, 1),
                FirstName = trainer.Name,
                Email = "",
                Phone = "",
                Address = "",
                CurrentAddress = "",
                Gender = false,
                FileID = 1,
                RoleID = 0
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return new GenericInfo()
            {
                Id = user.ID,
                Text = user.FirstName
            };
        }

        /**
         * @api {get} /user/list/employee/{search} GET employees
         * @apiVersion 1.0.0
         * @apiName GetEmployeeList
         * @apiGroup User
         * @apiPermission ApiUser
         * 
         * @apiParam {string} search          Tanda bintang (*) untuk tidak menggunakan search, atau kata yang mau di-search di nama employee.
         * 
         * @apiSuccessExample Success-Response:
         *   [
         *       {
         *           "id": 3,
         *           "text": "Daniel"
         *       },
         *       {
         *           "id": 13,
         *           "text": "Srie Tjandra"
         *       }
         *   ]
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("list/employee/{search}")]
        public List<GenericInfo> GetEmployeeList(string search)
        {
            search = search.Trim();
            IQueryable<GenericInfo> q;
            if(search.Equals("*")) {
                q = from u in _context.Users
                    where u.IsActive == true && u.IsDeleted == false
                    orderby u.FirstName
                    select new GenericInfo()
                    {
                        Id = u.ID,
                        Text = u.FirstName
                    };
            }
            else
            {
                q = from u in _context.Users
                    where u.IsActive == true && u.IsDeleted == false && u.FirstName.Contains(search)
                    orderby u.FirstName
                    select new GenericInfo()
                    {
                        Id = u.ID,
                        Text = u.FirstName
                    };
            }

            return q.ToList();
        }

        /**
         * @api {get} /user/list/rm/{search} GET RMs
         * @apiVersion 1.0.0
         * @apiName GetRMList
         * @apiGroup User
         * @apiPermission ApiUser
         * 
         * @apiParam {string} search          Tanda bintang (*) untuk tidak menggunakan search, atau kata yang mau di-search di nama RM.
         * 
         * @apiSuccessExample Success-Response:
         *   [
         *       {
         *           "id": 5,
         *           "text": "Leviana Wijaya"
         *       },
         *       {
         *           "id": 6,
         *           "text": "Meilliana Nasution"
         *       }
         *   ]
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("list/rm/{search}")]
        public List<GenericInfo> GetRMList(string search)
        {
            search = search.Trim();
            IQueryable<GenericInfo> q;
            if (search.Equals("*"))
            {
                q = from u in _context.Users
                    join rm in _context.CrmRelManagers
                    on u.ID equals rm.UserId
                    where u.IsActive == true && u.IsDeleted == false && rm.IsDeleted == false && rm.isActive == true
                    select new GenericInfo()
                    {
                        Id = u.ID,
                        Text = u.FirstName
                    };
            }
            else
            {
                q = from u in _context.Users
                    join rm in _context.CrmRelManagers
                    on u.ID equals rm.UserId
                    where u.IsActive == true && u.IsDeleted == false && rm.IsDeleted == false && rm.isActive == true && u.FirstName.Contains(search)
                    select new GenericInfo()
                    {
                        Id = u.ID,
                        Text = u.FirstName
                    };
            }

            return q.ToList();
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.ID == id);
        }

    }
}