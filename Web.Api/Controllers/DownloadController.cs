using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using KDMApi.DataContexts;
using KDMApi.Models.Helper;
using KDMApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KDMApi.Controllers
{
    [Route("v1/[controller]")]
    [ApiController]
    [EnableCors("QuBisaPolicy")]
    public class DownloadController : ControllerBase
    {
        private readonly DefaultContext _context;
        private readonly FileService _fileService;
        private DataOptions _options;
        public DownloadController(DefaultContext context, Microsoft.Extensions.Options.IOptions<DataOptions> options, FileService fileService)
        {
            _context = context;
            _options = options.Value;
            _fileService = fileService;
        }

        /*
         * @api {get} /download/{dir}/{id}/{filename}/{ori} Download file
         * @apiVersion 1.0.0
         * @apiName DownloadFile
         * @apiGroup Download
         * @apiPermission ApiUser
         * @apiDescription Endpoint ini tinggal dipakai aja sesuai URL yang diberikan
         * 
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("{dir}/{id}/{filename}/{ori}")]
        public async Task<IActionResult> DownloadFile(string dir, string id, string filename, string ori)
        {            
            string fullpath = Path.Combine(_options.DataRootDirectory, dir, id, filename);
            string contentType = "";
            try
            {
                contentType = GetContentType(fullpath);
            }
            catch
            {
                contentType = "application/octet-stream";
            }

            var memory = new MemoryStream();
            try
            {
                using (var stream = new FileStream(fullpath, FileMode.Open))
                {
                    await stream.CopyToAsync(memory);
                }
            }
            catch
            {
                return NotFound();
            }

            memory.Position = 0;
            return File(memory, contentType, ori);
        }

        private string GetContentType(string path)
        {
            var types = GetMimeTypes();
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return types[ext];
        }

        private Dictionary<string, string> GetMimeTypes()
        {
            return new Dictionary<string, string>
            {
                {".txt", "text/plain"},
                {".pdf", "application/pdf"},
                {".png", "image/png"},
                {".jpg", "image/jpeg"},
                {".jpeg", "image/jpeg"},
                {".gif", "image/gif"},
                {".csv", "text/csv"},
                {".doc", "application/msword"},
                {".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"},
                {".xls", "application/vnd.ms-excel"},
                {".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
                {".ppt", "application/vnd.ms-powerpoint"},
                {".pot", "application/vnd.ms-powerpoint"},
                {".pps", "application/vnd.ms-powerpoint"},
                {".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation"}
            };
        }
    }
}
 