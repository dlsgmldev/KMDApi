using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KDMApi.DataContexts;
using KDMApi.Models;
using KDMApi.Models.Crm;
using KDMApi.Models.Helper;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using KDMApi.Models.Km;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using KDMApi.Services;
using System.IO.Compression;

namespace KDMApi.Controllers
{
    [Route("v1/[controller]")]
    [ApiController]
    [EnableCors("QuBisaPolicy")]
    public class UploadController : ControllerBase
    {
        private static string separator = "<!>";

        private readonly DefaultContext _context;
        private readonly FileService _fileService;
        private readonly ClientService _clientService;
        private DataOptions _options;

        public UploadController(DefaultContext context, Microsoft.Extensions.Options.IOptions<DataOptions> options, FileService fileService, ClientService clientService)
        {
            _context = context;
            _options = options.Value;
            _fileService = fileService;
            _clientService = clientService;
        }

        
        // POST: v1/upload/csv
        /**
         * @api {post} /upload/csv Upload CSV
         * @apiVersion 1.0.0
         * @apiName PostUpload
         * @apiGroup Upload
         * @apiPermission CanUpdateCreateClient
         * 
         * @apiParam {Form-data} files        File CSV yang akan di-upload.
         * 
         * @apiErrorExample {json} Error-Response:
         *   {
         *     "code": "invalid",
         *     "description": "Error in line 12"
         *   }
         * @apiErrorExample {json} Error-Response:
         *   {
         *     "code": "extension",
         *     "description": "Please upload CSV file only."
         *   }
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "CanUpdateCreateClient")]
        [HttpPost("csv")]
        public async Task<ActionResult<Error>> PostUpload(List<IFormFile> files)
        {
            string token = Request.Headers["Authorization"].ToString();

            var accessToken = _context.RefreshTokens.FirstOrDefault(a => "Bearer " + a.AccessToken == token);

            if (accessToken == null)
            {
                return Unauthorized();

            }

            int userId = accessToken.UserId;
            DateTime now = DateTime.Now;

            var fileName = Path.GetTempFileName();
            int lineNumber = 0;
            foreach (IFormFile formFile in files)
            {
                if (formFile.Length > 0)
                {
                    var fileExt = System.IO.Path.GetExtension(formFile.FileName).Substring(1).ToLower();
                    if (!_fileService.checkFileExtension(fileExt, new[] { "csv" }))
                    {
                        return new Error("extension", "Please upload csv file only.");
                    }

                    using (var stream = System.IO.File.Create(fileName))
                    {
                        await formFile.CopyToAsync(stream);
                    }

                    using (TextReader textReader = System.IO.File.OpenText(fileName))
                    {
                        lineNumber++;
                        try
                        {
                            CsvReader reader = new CsvReader(textReader, CultureInfo.InvariantCulture);
                            reader.Configuration.Delimiter = ",";
                            reader.Configuration.MissingFieldFound = null;
                            while (reader.Read())
                            {
                                CsvContact contact = reader.GetRecord<CsvContact>();
                                int industryId = _clientService.GetOrCreateIndustry(contact, now, userId);
                                int clientId = _clientService.UpdateOrCreateClient(contact, now, userId, industryId, "Upload");
                                int contactId = _clientService.UpdateOrCreateContact(contact, now, userId, clientId, "Upload");
                            }
                        }
                        catch
                        {
                            return new Error("invalid", string.Join(" ", new[] { "Error in line", lineNumber.ToString() }));

                        }
                    }


                    System.IO.File.Delete(fileName);
                }

            }

            // Process uploaded files
            // Don't rely on or trust the FileName property without validation.
            return new Error("ok", "Upload successful");
        }

        // POST: v1/upload/profile
        /**
         * @api {post} /upload/profile Upload profile
         * @apiVersion 1.0.0
         * @apiName PostUploadProfile
         * @apiGroup Upload
         * @apiPermission ApiUser
         * 
         * @apiParam {Form-data} files        File yang akan di-upload.
         * 
         * @apiErrorExample {json} Error-Response:
         *   {
         *     "code": "extension",
         *     "description": "Please upload PNG or JPG file only."
         *   }
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost("profile")]
        public async Task<ActionResult<Error>> PostUploadProfile(List<IFormFile> files)
        {
            string token = Request.Headers["Authorization"].ToString();

            var accessToken = _context.RefreshTokens.FirstOrDefault(a => "Bearer " + a.AccessToken == token);

            if (accessToken == null)
            {
                return Unauthorized();
            }

            int userId = accessToken.UserId;

            const int imageWidth = 200;
            const int imageHeight = 200;
            DateTime now = DateTime.Now;

            foreach (IFormFile formFile in files)
            {
                if (formFile.Length > 0)
                {
                    var fileExt = System.IO.Path.GetExtension(formFile.FileName).Substring(1).ToLower();
                    if (!_fileService.checkFileExtension(fileExt, new[] { "jpg", "jpeg", "png" }))
                    {
                        return new Error("extension", "Please upload PNG or JPG file only.");
                    }
                    string randomName = Path.GetRandomFileName() + "." + fileExt;
                    var fileName = Path.Combine(_options.DataRootDirectory, @"images", @"profile", randomName);

                    Stream stream = formFile.OpenReadStream();
                    using (var imagedata = System.Drawing.Image.FromStream(stream))
                    {
                        if (imagedata.Width > imageWidth || imagedata.Height > imageHeight)
                        {
                            _fileService.ResizeImage(imagedata, imageWidth, imageHeight, fileName, fileExt);
                        }
                    }
                    stream.Dispose();

                    vProfileImage curProfile = _context.vProfileImage.Where(a => a.Id == userId).FirstOrDefault();
                    if (curProfile != null)
                    {
                        curProfile.FileURL = _options.ProfilePictureControllerRoute; //string.Join("/", new[] { "kmdata", "images", "profile", "" });
                        curProfile.FileName = randomName;
                        curProfile.Modified = now;
                        _context.Entry(curProfile).State = EntityState.Modified;
                    }
                    else
                    {
                        vProfileImage profile = new vProfileImage();
                        profile.Id = userId;
                        profile.FileURL = _options.ProfilePictureControllerRoute; //string.Join("/", new[] { "kmdata", "images", "profile", "" });
                        profile.FileName = randomName;
                        profile.Created = now;
                        profile.Modified = now;
                        profile.IsDeleted = false;
                        _context.vProfileImage.Add(profile);
                    }
                    await _context.SaveChangesAsync();
                }
            }

            return new Error("ok", "Upload successful");
        }

        /**
         * @api {post} /upload/file Upload file
         * @apiVersion 1.0.0
         * @apiName UploadFile
         * @apiGroup Upload
         * @apiPermission ApiUser
         * @apiDescription Pakai form-data
         * 
         * @apiParam {Number} onegml        0 kalau untuk file yang bukan di OneGML, 1 untuk file OneGML.
         * @apiParam {Number} projectId     id dari project. O untuk file OneGML.
         * @apiParam {Number} parentId      id dari folder. 0 untuk ditaruh di root folder.
         * @apiParam {Number} userId        id dari user yang meng-upload file.
         * @apiParam {File} files           File yang akan di-upload.
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "code": "ok",
         *       "description": "Upload successful."
         *   }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost("file")]
        public async Task<ActionResult<Error>> UploadFile([FromForm] UploadFileRequest request)
        {
            DateTime now = DateTime.Now;

            try
            {
                string path;
                if(request.Onegml == 1)
                {
                    path = Path.Combine(_options.DataRootDirectory, @"onegml");
                    _fileService.CheckAndCreateDirectory(path);
                }
                else
                {
                    var project = _context.KmProjects.Where(a => a.Id == request.Projectid).FirstOrDefault();
                    if (project == null || project.Id == 0)
                    {
                        return NotFound(new Error("project", "Project not found."));
                    }

                    path = Path.Combine(_options.DataRootDirectory, project.TribeId.ToString());
                    _fileService.CheckAndCreateDirectory(path);
                    path = Path.Combine(path, project.ClientId.ToString());
                    _fileService.CheckAndCreateDirectory(path);
                    path = Path.Combine(path, project.YearId.ToString());
                    _fileService.CheckAndCreateDirectory(path);
                    path = Path.Combine(path, project.Id.ToString());
                    _fileService.CheckAndCreateDirectory(path);

                    if (request.ParentId > 0)
                    {
                        path = Path.Combine(path, request.ParentId.ToString());
                        _fileService.CheckAndCreateDirectory(path);
                    }

                }


                foreach (IFormFile formFile in request.Files)
                {
                    var fileExt = System.IO.Path.GetExtension(formFile.FileName).Substring(1).ToLower();

                    if (formFile.FileName.EndsWith("zip"))
                    {
                        string randomName = Path.GetRandomFileName() + "." + fileExt;

                        var fileName = Path.Combine(Path.GetTempPath(), randomName);

                        Stream stream = formFile.OpenReadStream();
                        _fileService.CopyStream(stream, fileName);
                        stream.Dispose();

                        using (var archive = ZipFile.OpenRead(Path.Combine(System.IO.Path.GetTempPath(), fileName)))
                        {
                            IDictionary<string, int> folderIds = new Dictionary<string, int>();

                            foreach (var s in archive.Entries)
                            {
                                if (s.FullName.EndsWith("/"))
                                {
                                    // It is a folder.
                                    // Folder is not actually extracted.
                                    int lastIndex = s.FullName.LastIndexOf('/', s.FullName.Length - 2);
                                    string folderName = "";
                                    if (lastIndex == -1)
                                    {
                                        // It is a top folder
                                        folderName = s.FullName.Substring(0, s.FullName.Length - 1);

                                        KmFile folder = new KmFile()
                                        {
                                            ParentId = request.ParentId,
                                            Name = folderName,
                                            Filename = "",
                                            FileType = "",
                                            IsFolder = true,
                                            RootFolder = _options.DataRootDirectory,
                                            Description = "",
                                            ProjectId = request.Projectid,
                                            Onegml = request.Onegml == 1,
                                            OwnerId = request.OwnerId,
                                            Fullpath = "",
                                            Extracted = false,
                                            CreatedDate = now,
                                            CreatedBy = request.UserId,
                                            LastUpdated = now,
                                            LastUpdatedBy = request.UserId,
                                            IsDeleted = false
                                        };
                                        _context.KmFiles.Add(folder);
                                        await _context.SaveChangesAsync();

                                        folderIds.Add(s.FullName, folder.Id);
                                    }
                                    else
                                    {
                                        folderName = s.FullName.Substring(lastIndex + 1, s.FullName.Length - 2 - lastIndex);
                                        int parentId = folderIds[s.FullName.Substring(0, lastIndex + 1)];

                                        KmFile folder = new KmFile()
                                        {
                                            ParentId = parentId,
                                            Name = folderName,
                                            Filename = "",
                                            FileType = "",
                                            IsFolder = true,
                                            RootFolder = _options.DataRootDirectory,
                                            Description = "",
                                            ProjectId = request.Projectid,
                                            Onegml = request.Onegml == 1,
                                            OwnerId = request.OwnerId,
                                            Fullpath = "",
                                            Extracted = false,
                                            CreatedDate = now,
                                            CreatedBy = request.UserId,
                                            LastUpdated = now,
                                            LastUpdatedBy = request.UserId,
                                            IsDeleted = false
                                        };
                                        _context.KmFiles.Add(folder);
                                        await _context.SaveChangesAsync();

                                        folderIds.Add(s.FullName, folder.Id);
                                    }
                                }
                                else
                                {
                                    // It is a file

                                    // Extract the file to the directory
                                    string ext = Path.GetExtension(s.FullName).Substring(1).ToLower();
                                    var temp = Path.GetRandomFileName() + @"." + ext;
                                    string destPath = Path.Combine(path, temp);
                                    s.ExtractToFile(destPath);

                                    // Put the file in the database
                                    int lastIndex = s.FullName.LastIndexOf('/');
                                    if (lastIndex == -1)
                                    {
                                        // Is in root folder
                                        KmFile kmFile = new KmFile()
                                        {
                                            ParentId = request.ParentId,
                                            Name = s.FullName,
                                            Filename = temp,
                                            FileType = ext,
                                            IsFolder = false,
                                            RootFolder = _options.DataRootDirectory,
                                            Description = "",
                                            ProjectId = request.Projectid,
                                            Onegml = request.Onegml == 1,
                                            OwnerId = 0,                        // OwnerId hanya untuk folder. 
                                            Fullpath = destPath,
                                            Extracted = false,
                                            CreatedDate = now,
                                            CreatedBy = request.UserId,
                                            LastUpdated = now,
                                            LastUpdatedBy = request.UserId,
                                            IsDeleted = false,
                                            DeletedBy = 0,
                                            DeletedDate = new DateTime(1970, 1, 1)
                                        };
                                        _context.KmFiles.Add(kmFile);
                                        await _context.SaveChangesAsync();
                                    }
                                    else
                                    {
                                        // Is in a folder
                                        int parentId = folderIds[s.FullName.Substring(0, lastIndex + 1)];
                                        KmFile kmFile = new KmFile()
                                        {
                                            ParentId = parentId,
                                            Name = s.FullName.Substring(lastIndex + 1),
                                            Filename = temp,
                                            FileType = ext,
                                            IsFolder = false,
                                            RootFolder = _options.DataRootDirectory,
                                            Description = "",
                                            ProjectId = request.Projectid,
                                            Onegml = request.Onegml == 1,
                                            OwnerId = 0,                        // OwnerId hanya untuk folder. 
                                            Fullpath = destPath,
                                            Extracted = false,
                                            CreatedDate = now,
                                            CreatedBy = request.UserId,
                                            LastUpdated = now,
                                            LastUpdatedBy = request.UserId,
                                            IsDeleted = false,
                                            DeletedBy = 0,
                                            DeletedDate = new DateTime(1970, 1, 1)
                                        };
                                        _context.KmFiles.Add(kmFile);
                                        await _context.SaveChangesAsync();
                                    }
                                }


                            }
                        }

                        System.IO.File.Delete(Path.Combine(System.IO.Path.GetTempPath(), randomName));

                    }
                    else
                    {
                        string randomName = Path.GetRandomFileName() + "." + fileExt;

                        var fileName = Path.Combine(path, randomName);

                        Stream stream = formFile.OpenReadStream();
                        _fileService.CopyStream(stream, fileName);
                        stream.Dispose();

                        KmFile kmFile = new KmFile()
                        {
                            ParentId = request.ParentId,
                            Name = formFile.FileName,
                            Filename = randomName,
                            FileType = fileExt,
                            IsFolder = false,
                            RootFolder = _options.DataRootDirectory,
                            Description = "",
                            ProjectId = request.Projectid,
                            Onegml = request.Onegml == 1,
                            OwnerId = 0,                        // OwnerId hanya untuk folder. 
                            Fullpath = fileName,
                            Extracted = false,
                            CreatedDate = now,
                            CreatedBy = request.UserId,
                            LastUpdated = now,
                            LastUpdatedBy = request.UserId,
                            IsDeleted = false,
                            DeletedBy = 0,
                            DeletedDate = new DateTime(1970, 1, 1)
                        };
                        _context.KmFiles.Add(kmFile);
                        await _context.SaveChangesAsync();
                    }
                }


                return new Error("ok", "Upload successful.");

            }
            catch (Exception e)
            {
                return new Error("error", "Error in uploading file.");
            }
        }


        /**
         * @api {post} /upload/filebase64 Upload file base64
         * @apiVersion 1.0.0
         * @apiName UploadFileBase64
         * @apiGroup Upload
         * @apiPermission ApiUser
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "onegml": 0,
         *     "ownerId": 0,
         *     "projectid": 3,
         *     "parentId": 0,
         *     "userId": 2,
         *     "files": [
         *       {
         *         "filename": "Laporan Project.pdf",
         *         "fileBase64": "... file dalam format base64"
         *       }
         *     ]
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   {
         *       "code": "ok",
         *       "description": "Upload successful."
         *   }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost("filebase64")]
        public async Task<ActionResult<Error>> UploadFileBase64(UploadFileRequestBase64 request)
        {
            try
            {
                string path;
                if (request.Onegml == 1)
                {
                    path = Path.Combine(_options.DataRootDirectory, @"onegml");
                    _fileService.CheckAndCreateDirectory(path);
                }
                else
                {
                    KmProject project = _context.KmProjects.Find(request.Projectid);
                    if (project == null || project.Id == 0)
                    {
                        return NotFound(new Error("project", "Project not found."));
                    }

                    path = Path.Combine(_options.DataRootDirectory, project.TribeId.ToString());
                    _fileService.CheckAndCreateDirectory(path);
                    path = Path.Combine(path, project.ClientId.ToString());
                    _fileService.CheckAndCreateDirectory(path);
                    path = Path.Combine(path, project.YearId.ToString());
                    _fileService.CheckAndCreateDirectory(path);
                    path = Path.Combine(path, project.Id.ToString());
                    _fileService.CheckAndCreateDirectory(path);

                    if (request.ParentId > 0)
                    {
                        path = Path.Combine(path, request.ParentId.ToString());
                        _fileService.CheckAndCreateDirectory(path);
                    }
                }

                DateTime now = DateTime.Now;

                foreach (UploadFileBase64 file in request.files)
                {
                    if(file.Filename.EndsWith("zip"))
                    {
                        Error error = SaveFileUploadBase64(file.FileBase64, file.Filename, Path.GetTempPath());
                        if(error.Code.Equals("ok"))
                        {
                            string[] names = error.Description.Split(separator);
                            if (names.Length >= 3)
                            {
                                using (var archive = ZipFile.OpenRead(Path.Combine(System.IO.Path.GetTempPath(), names[1])))
                                {
                                    IDictionary<string, int> folderIds = new Dictionary<string, int>();

                                    foreach (var s in archive.Entries)
                                    {
                                        if (s.FullName.EndsWith("/"))
                                        {
                                            // It is a folder.
                                            // Folder is not actually extracted.
                                            int lastIndex = s.FullName.LastIndexOf('/', s.FullName.Length - 2);
                                            string folderName = "";
                                            if(lastIndex == -1)
                                            {
                                                // It is a top folder
                                                folderName = s.FullName.Substring(0, s.FullName.Length - 1);

                                                KmFile folder = new KmFile()
                                                {
                                                    ParentId = request.ParentId,
                                                    Name = folderName,
                                                    Filename = "",
                                                    FileType = "",
                                                    IsFolder = true,
                                                    RootFolder = _options.DataRootDirectory,
                                                    Description = "",
                                                    ProjectId = request.Projectid,
                                                    Onegml = request.Onegml == 1,
                                                    OwnerId = request.OwnerId,
                                                    Fullpath = "",
                                                    Extracted = false,
                                                    CreatedDate = now,
                                                    CreatedBy = request.UserId,
                                                    LastUpdated = now,
                                                    LastUpdatedBy = request.UserId,
                                                    IsDeleted = false
                                                };
                                                _context.KmFiles.Add(folder);
                                                await _context.SaveChangesAsync();

                                                folderIds.Add(s.FullName, folder.Id);
                                            }
                                            else
                                            {
                                                folderName = s.FullName.Substring(lastIndex + 1, s.FullName.Length - 2 - lastIndex);
                                                int parentId = folderIds[s.FullName.Substring(0, lastIndex + 1)];

                                                KmFile folder = new KmFile()
                                                {
                                                    ParentId = parentId,
                                                    Name = folderName,
                                                    Filename = "",
                                                    FileType = "",
                                                    IsFolder = true,
                                                    RootFolder = _options.DataRootDirectory,
                                                    Description = "",
                                                    ProjectId = request.Projectid,
                                                    Onegml = request.Onegml == 1,
                                                    OwnerId = request.OwnerId,
                                                    Fullpath = "",
                                                    Extracted = false,
                                                    CreatedDate = now,
                                                    CreatedBy = request.UserId,
                                                    LastUpdated = now,
                                                    LastUpdatedBy = request.UserId,
                                                    IsDeleted = false
                                                };
                                                _context.KmFiles.Add(folder);
                                                await _context.SaveChangesAsync();

                                                folderIds.Add(s.FullName, folder.Id);
                                            }
                                        }
                                        else
                                        {
                                            // It is a file

                                            // Extract the file to the directory
                                            var fileExt = System.IO.Path.GetExtension(s.FullName).Substring(1).ToLower();

                                            var temp = Path.GetRandomFileName() + @"." + fileExt;
                                            string destPath = Path.Combine(path, temp);
                                            s.ExtractToFile(destPath);

                                            // Put the file in the database
                                            int lastIndex = s.FullName.LastIndexOf('/');
                                            if (lastIndex == -1)
                                            {
                                                // Is in root folder
                                                KmFile kmFile = new KmFile()
                                                {
                                                    ParentId = request.ParentId,
                                                    Name = s.FullName,
                                                    Filename = temp,
                                                    FileType = fileExt,
                                                    IsFolder = false,
                                                    RootFolder = _options.DataRootDirectory,
                                                    Description = "",
                                                    ProjectId = request.Projectid,
                                                    Onegml = request.Onegml == 1,
                                                    OwnerId = 0,                        // OwnerId hanya untuk folder. 
                                                    Fullpath = destPath,
                                                    Extracted = false,
                                                    CreatedDate = now,
                                                    CreatedBy = request.UserId,
                                                    LastUpdated = now,
                                                    LastUpdatedBy = request.UserId,
                                                    IsDeleted = false,
                                                    DeletedBy = 0,
                                                    DeletedDate = new DateTime(1970, 1, 1)
                                                };
                                                _context.KmFiles.Add(kmFile);
                                                await _context.SaveChangesAsync();
                                            }
                                            else
                                            {
                                                // Is in a folder
                                                int parentId = folderIds[s.FullName.Substring(0, lastIndex + 1)];
                                                KmFile kmFile = new KmFile()
                                                {
                                                    ParentId = parentId,
                                                    Name = s.FullName.Substring(lastIndex + 1),
                                                    Filename = temp,
                                                    FileType = fileExt,
                                                    IsFolder = false,
                                                    RootFolder = _options.DataRootDirectory,
                                                    Description = "",
                                                    ProjectId = request.Projectid,
                                                    Onegml = request.Onegml == 1,
                                                    OwnerId = 0,                        // OwnerId hanya untuk folder. 
                                                    Fullpath = destPath,
                                                    Extracted = false,
                                                    CreatedDate = now,
                                                    CreatedBy = request.UserId,
                                                    LastUpdated = now,
                                                    LastUpdatedBy = request.UserId,
                                                    IsDeleted = false,
                                                    DeletedBy = 0,
                                                    DeletedDate = new DateTime(1970, 1, 1)
                                                };
                                                _context.KmFiles.Add(kmFile);
                                                await _context.SaveChangesAsync();
                                            }
                                        }

                                        
                                    }
                                }

                                System.IO.File.Delete(Path.Combine(System.IO.Path.GetTempPath(), names[1]));

                            }
                        }
                    }
                    else
                    {
                        Error error = SaveFileUploadBase64(file.FileBase64, file.Filename, path);
                        if (error.Code.Equals("ok"))
                        {
                            string[] names = error.Description.Split(separator);
                            if (names.Length >= 3)
                            {
                                KmFile kmFile = new KmFile()
                                {
                                    ParentId = request.ParentId,
                                    Name = names[0],
                                    Filename = names[1],
                                    FileType = names[2],
                                    IsFolder = false,
                                    RootFolder = _options.DataRootDirectory,
                                    Description = "",
                                    ProjectId = request.Projectid,
                                    Onegml = request.Onegml == 1,
                                    OwnerId = 0,                        // OwnerId hanya untuk folder. 
                                    Fullpath = Path.Combine(path, names[1]),
                                    Extracted = false,
                                    CreatedDate = now,
                                    CreatedBy = request.UserId,
                                    LastUpdated = now,
                                    LastUpdatedBy = request.UserId,
                                    IsDeleted = false,
                                    DeletedBy = 0,
                                    DeletedDate = new DateTime(1970, 1, 1)
                                };
                                _context.KmFiles.Add(kmFile);
                                await _context.SaveChangesAsync();
                            }
                            else
                            {
                                return BadRequest(new { error = "Unknown error." });
                            }
                        }
                        else
                        {
                            return BadRequest(new { error = "Error in uploading file." });
                        }

                    }

                }


            }
            catch (Exception e)
            {
                return BadRequest(new { error = "Error in uploading file." });
            }


            return new Error("ok", "Upload successful.");

        }

        /**
         * @api {post} /upload/updatefile Update file base64
         * @apiVersion 1.0.0
         * @apiName UploadUpdateFileBase64
         * @apiGroup Upload
         * @apiPermission ApiUser
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "fileId": 5
         *     "userId": 1
         *     "filename": "Laporan Project.pdf",
         *     "fileBase64": "... file dalam format base64"
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   {
         *       "code": "ok",
         *       "description": "Upload successful."
         *   }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost("updatefile")]
        public async Task<ActionResult<Error>> UploadUpdateFileBase64(UpdateFileRequest request)
        {
            KmFile file = _context.KmFiles.Find(request.FileId);
            if(file == null)
            {
                return NotFound();
            }

            string path;
            if (file.Onegml)
            {
                path = Path.Combine(_options.DataRootDirectory, @"onegml");
                _fileService.CheckAndCreateDirectory(path);
            }
            else
            {
                KmProject project = _context.KmProjects.Find(file.ProjectId);
                if (project == null || project.Id == 0)
                {
                    return NotFound(new Error("project", "Project not found."));
                }

                path = Path.Combine(_options.DataRootDirectory, project.TribeId.ToString());
                _fileService.CheckAndCreateDirectory(path);
                path = Path.Combine(path, project.ClientId.ToString());
                _fileService.CheckAndCreateDirectory(path);
                path = Path.Combine(path, project.YearId.ToString());
                _fileService.CheckAndCreateDirectory(path);
                path = Path.Combine(path, project.Id.ToString());
                _fileService.CheckAndCreateDirectory(path);

                if (file.ParentId > 0)
                {
                    path = Path.Combine(path, file.ParentId.ToString());
                    _fileService.CheckAndCreateDirectory(path);
                }
            }

            DateTime now = DateTime.Now;

            try
            {
                Error error = SaveFileUploadBase64(request.FileBase64, request.Filename, path);
                if (error.Code.Equals("ok"))
                {
                    string[] names = error.Description.Split(separator);
                    if (names.Length >= 3)
                    {
                        file.Name = names[0];
                        file.Filename = names[1];
                        file.FileType = names[2];
                        file.LastUpdated = now;
                        file.LastUpdatedBy = request.UserId;

                        _context.Entry(file).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        return BadRequest(new { error = "Unknown error." });
                    }
                }
                else
                {
                    return BadRequest(new { error = "Error in uploading file." });
                }

            }
            catch
            {
                return BadRequest(new { error = "Error in uploading file." });
            }

            return new Error("ok", "Upload successful.");
        }

        /**
         * @api {post} /upload/ckeditor Upload file dari CKEditor
         * @apiVersion 1.0.0
         * @apiName PostUploadCKEditor
         * @apiGroup Upload
         * @apiPermission ApiUser
         * 
         * @apiParam {Form-data} upload        File PNG atau JPG yang akan di-upload.
         * 
         * @apiErrorExample {json} Error-Response:
         *   {
         *     "code": "extension",
         *     "description": "Please upload PNG or JPG file only."
         *   }
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost("ckeditor")]
        public async Task<ActionResult<Object>> PostUploadCKEditor(List<IFormFile> upload)
        {
            DateTime now = DateTime.Now;

            foreach (IFormFile formFile in upload)
            {
                if (formFile.Length > 0)
                {
                    var fileExt = System.IO.Path.GetExtension(formFile.FileName).Substring(1).ToLower();
                    if (!_fileService.checkFileExtension(fileExt, new[] { "jpg", "jpeg", "png", "gif" }))
                    {
                        return new { message = "Please upload file JPG, JPEG, PNG or GIF only" };
                    }
                    string randomName = Path.GetRandomFileName() + "." + fileExt;
                    var fileName = Path.Combine(_options.AssetsRootDirectory, @"events", @"images", randomName);

                    Stream stream = formFile.OpenReadStream();
                    /*
                    using (var imagedata = System.Drawing.Image.FromStream(stream))
                    {
                        _fileService.ResizeImage(imagedata, imagedata.Width, imagedata.Height, fileName, fileExt);
                    }
                    */
                    _fileService.SaveFromStream(stream, fileName);
                    stream.Dispose();

                    WebEventImage image = new WebEventImage()
                    {
                        Name = formFile.FileName,
                        Filename = randomName,
                        FileType = fileExt,
                        EventId = -1,                   // -1 means it is an image used in the description, linked by url only
                        DescriptionId = 1,
                        FrameworkId = 0,
                        DocumentationId = 0,
                        TestimonyId = 0,
                        ThumbnailId = 0,
                        CreatedDate = now,
                        CreatedBy = 0,
                        LastUpdated = now,
                        LastUpdatedBy = 0,
                        IsDeleted = false,
                        DeletedBy = 0
                    };
                    _context.WebEventImages.Add(image);
                    await _context.SaveChangesAsync();

                    return new { url = _options.AssetsBaseURL + "/events/images/" + randomName };       // The directory is used also for other purpose, such as insight

                }
            }

            return new { message = "Unknown error" };
        }

        private Error SaveFileUploadBase64(string base64String, string filename, string fileDir)
        {
            try
            {
                // base64String = base64String.Substring(n);
                var fileExt = System.IO.Path.GetExtension(filename).Substring(1).ToLower();

                string randomName = Path.GetRandomFileName() + "." + fileExt;
                if (_fileService.CheckAndCreateDirectory(fileDir))
                {
                    var fileName = Path.Combine(fileDir, randomName);
                    _fileService.SaveByteAsFile(fileName, base64String);
                    return new Error("ok", string.Join(separator, new[] { filename, randomName, fileExt }));
                }
                else
                {
                    return new Error("error", "Error in saving file.");
                }
            }
            catch
            {
                return new Error("error", "Error in saving file.");
            }
        }

    }
}
