using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KDMApi.DataContexts;
using KDMApi.Models;
using Microsoft.AspNetCore.Authorization;
using KDMApi.Models.Web;
using Microsoft.AspNetCore.Cors;

namespace KDMApi.Controllers
{
    [Route("v1/[controller]")]
    [ApiController]
    [EnableCors("QuBisaPolicy")]
    public class CareerController : ControllerBase
    {
        private readonly DefaultContext _context;

        public CareerController(DefaultContext context)
        {
            _context = context;
        }

        // GET: v1/Career
        /**
         * @api {get} /Career Get careers 
         * @apiVersion 1.0.0
         * @apiName GetWebCareers
         * @apiGroup Web
         * @apiPermission ApiUser
         * @apiDescription Mendapatkan list career yang tersedia
         * 
         * @apiSuccessExample Success-Response:
         *   [
         *     {
         *       "id": 1,
         *       "title": "3D Animator",
         *       "description": "Deskripsi 3D Animator adalah ini",
         *       "location": "Jakarta, Medan",
         *       "publish": 1,
         *       "createdDate": "2020-03-25T10:22:08.6662787",
         *       "createdBy": 3,
         *       "lastUpdated": "2020-03-25T10:23:16.523977",
         *       "lastUpdatedBy": 3,
         *       "isDeleted": false,
         *       "deletedBy": 0,
         *       "deletedDate": null
         *     }
         *   ]
         * 
         * @apiError NotAuthorized Token salah.
         * 
         */
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WebCareer>>> GetWebCareers()
        {
            return await _context.WebCareers.Where(a => !a.IsDeleted).ToListAsync();
        }

        // GET: v1/Career/5
        /**
         * @api {get} /Career/{id} Get career by id 
         * @apiVersion 1.0.0
         * @apiName GetWebCareer
         * @apiGroup Web
         * @apiPermission ApiUser
         * @apiDescription Mendapatkan career dengan id tertentu
         *     
         * @apiParam {Number} id            Id dari career yang ingin didapat
         * 
         * @apiSuccessExample Success-Response:         
         * {
         *   "id": 1,
         *   "title": "3D Animator",
         *   "description": "Deskripsi 3D Animator adalah ini",
         *   "location": "Jakarta, Medan",
         *   "publish": 1,
         *   "createdDate": "2020-03-25T10:22:08.6662787",
         *   "createdBy": 3,
         *   "lastUpdated": "2020-03-25T10:23:16.523977",
         *   "lastUpdatedBy": 3,
         *   "isDeleted": false,
         *   "deletedBy": 0,
         *   "deletedDate": null
         * }
         * 
         * @apiError NotFound Id salah.
         * 
         */
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<WebCareer>> GetWebCareer(int id)
        {
            var webCareer = await _context.WebCareers.FindAsync(id);

            if (webCareer == null)
            {
                return NotFound();
            }

            return webCareer;
        }

        // PUT: v1/Career/5
        /**
         * @api {put} /Career/{id} Put career  
         * @apiVersion 1.0.0
         * @apiName PutWebCareer
         * @apiGroup Web
         * @apiPermission ApiUser
         * @apiDescription Mengedit career dengan id tertentu. Id yang ada di parameter harus sama dengan id yang ada di JSON.
         * 
         * @apiParam {Number} id            Id dari career yang ingin diedit
         *     
         * @apiParamExample {json} Request-Example:
         * {
         *   "id": 1,
         *   "title": "3D Animator",
         *   "description": "Deskripsi 3D Animator adalah ini",
         *   "location": "Jakarta",
         *   "publish": 1,
         *   "userId": 3
         * }
         * 
         * @apiSuccessExample Success-Response:         
         * 204 No content
         * 
         * @apiError BadResponse id di parameter berdeda dengan yang di JSON.
         * @apiError NotFound    id salah
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutWebCareer(int id, WebCareerRequest request)
        {
            if (id != request.Id)
            {
                return BadRequest();
            }

            try
            {
                DateTime now = DateTime.Now;

                var webCareer = await _context.WebCareers.FindAsync(id);
                webCareer.Title = request.Title;
                webCareer.Location = request.Location;
                webCareer.Description = request.Description;
                webCareer.Publish = request.Publish;
                webCareer.LastUpdatedBy = request.UserId;
                webCareer.LastUpdated = now;

                _context.Entry(webCareer).State = EntityState.Modified;
                await _context.SaveChangesAsync();

            }
            catch
            {
                if (!WebCareerExists(id))
                {
                    return NotFound();
                }
                else
                {
                    return BadRequest();
                }
            }


            return NoContent();
        }

        // POST: v1/Career
        /**
         * @api {post} /Career Post career  
         * @apiVersion 1.0.0
         * @apiName PostWebCareer
         * @apiGroup Web
         * @apiPermission ApiUser
         * @apiDescription Menambah career baru.
         * 
         * @apiParamExample {json} Request-Example:
         * {
         *   "title": "3D Animator",
         *   "description": "Deskripsi 3D Animator adalah ini",
         *   "location": "Jakarta",
         *   "publish": 1,
         *   "userId": 3
         * }
         * 
         * @apiSuccessExample Success-Response:               
         * {
         *   "id": 2,
         *   "title": "3D Animator",
         *   "description": "Deskripsi 3D Animator adalah ini",
         *   "location": "Jakarta, Medan",
         *   "publish": 0,
         *   "createdDate": "2020-03-25T10:38:26.0467174+07:00",
         *   "createdBy": 3,
         *   "lastUpdated": "2020-03-25T10:38:26.0467174+07:00",
         *   "lastUpdatedBy": 3,
         *   "isDeleted": false,
         *   "deletedBy": 0,
         *   "deletedDate": null
         * }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost]
        public async Task<ActionResult<WebCareer>> PostWebCareer(WebCareerRequest request)
        {
            DateTime now = DateTime.Now;

            WebCareer webCareer = new WebCareer()
            {
                Title = request.Title,
                Description = request.Description,
                Location = request.Location,
                Publish = request.Publish,
                CreatedBy = request.UserId,
                CreatedDate = now,
                LastUpdatedBy = request.UserId,
                LastUpdated = now,
                IsDeleted = false
            };

            _context.WebCareers.Add(webCareer);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetWebCareer", new { id = webCareer.Id }, webCareer);
        }

        // DELETE: v1/Career/5/3
        /**
         * @api {delete} /Career/{id}/{userId} Delete career  
         * @apiVersion 1.0.0
         * @apiName DeleteWebCareer
         * @apiGroup Web
         * @apiPermission ApiUser
         * @apiDescription Menghapus career dengan id tertentu. 
         * 
         * @apiParam {Number} id            Id dari career yang ingin dihapus
         * @apiParam {Number} userId        userId dari user yang login
         * @apiSuccessExample Success-Response:               
         * {
         *   "id": 2,
         *   "title": "3D Animator",
         *   "description": "Deskripsi 3D Animator adalah ini",
         *   "location": "Jakarta, Medan",
         *   "publish": 1,
         *   "createdDate": "2020-03-25T10:38:26.0467174",
         *   "createdBy": 3,
         *   "lastUpdated": "2020-03-25T10:43:30.629769",
         *   "lastUpdatedBy": 3,
         *   "isDeleted": true,
         *   "deletedBy": 3,
         *   "deletedDate": "2020-03-25T10:44:20.3078145+07:00"
         * }
         * 
         * @apiError NotFound    id salah
         * @apiError BadRequest  Error dalam penulisan ke database (misal karena userID salah).
         */
        [Authorize(Policy = "ApiUser")]
        [HttpDelete("{id}/{userId}")]
        public async Task<ActionResult<WebCareer>> DeleteWebCareer(int id, int userId)
        {
            var webCareer = await _context.WebCareers.FindAsync(id);
            if (webCareer == null)
            {
                return NotFound();
            }

            try
            {
                DateTime now = DateTime.Now;
                webCareer.DeletedBy = userId;
                webCareer.DeletedDate = now;
                webCareer.IsDeleted = true;

                _context.Entry(webCareer).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return webCareer;

            }
            catch
            {
                return BadRequest();
            }
        }

        // GET: v1/Career/publish/5/1/3
        /**
         * @api {get} /Career/publish/{id}/{publish}/{userId} Publish career  
         * @apiVersion 1.0.0
         * @apiName PublishWebCareer
         * @apiGroup Web
         * @apiPermission ApiUser
         * @apiDescription Mem-publish career dengan id tertentu. 
         * 
         * @apiParam {Number} id            Id dari career yang ingin di-publish
         * @apiParam {Number} publish       1 untuk publish, 0 untuk tidak mem-publish         
         * @apiParam {Number} userId        userId dari user yang login
         * @apiSuccessExample Success-Response:        
         * {
         *   "id": 2,
         *   "title": "3D Animator",
         *   "description": "Deskripsi 3D Animator adalah ini",
         *   "location": "Jakarta, Medan",
         *   "publish": 1,
         *   "createdDate": "2020-03-25T10:38:26.0467174",
         *   "createdBy": 3,
         *   "lastUpdated": "2020-03-25T10:43:30.629769+07:00",
         *   "lastUpdatedBy": 3,
         *   "isDeleted": false,
         *   "deletedBy": 0,
         *   "deletedDate": null
         * }
         * 
         * @apiError NotFound    id salah
         * 
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("publish/{id}/{publish}/{userId}")]
        public async Task<ActionResult<WebCareer>> PublishWebCareer(int id, int publish, int userId)
        {
            var webCareer = await _context.WebCareers.FindAsync(id);
            if (webCareer == null)
            {
                return NotFound();
            }
            DateTime now = DateTime.Now;
            webCareer.LastUpdated = now;
            webCareer.LastUpdatedBy = userId;
            webCareer.Publish = publish;

            _context.Entry(webCareer).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return webCareer;
        }

        private bool WebCareerExists(int id)
        {
            return _context.WebCareers.Any(e => e.Id == id);
        }
    }
}
