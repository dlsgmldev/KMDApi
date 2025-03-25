using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KDMApi.DataContexts;
using KDMApi.Models.Web;
using KDMApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using KDMApi.Services;
using KDMApi.Models.Helper;
using System.IO;
using KDMApi.Models.Crm;
using System.Text;
using KDMApi.Models.Km;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Cors;
using System.Drawing.Imaging;
using System.Net.Http;
using CsvHelper;
using System.Globalization;
using ExcelDataReader;
using System.Data;
using Web.Api.Models.Request;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Logging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Authentication;
using System.Net.Http.Headers;
using Org.BouncyCastle.Asn1.Ocsp;
using Swashbuckle.AspNetCore.Swagger;
using Nest;
using Newtonsoft.Json;

namespace KDMApi.Controllers
{
    [Route("v1/[controller]")]
    [ApiController]
    [EnableCors("QuBisaPolicy")]
    public class EventController : ControllerBase
    {
        private static string separator = "<!>";
        private static int IMAGE_DRAFT = 1;
        private static int IMAGE_PUBLISH = 2;
        private static int BANNER_WIDTH = 720;
        private static int BANNER_HEIGHT = 360;

        private readonly DefaultContext _context;
        private readonly FileService _fileService;
        private readonly ClientService _clientService;
        private readonly IEmailService _emailService;
        private DataOptions _options;

        public EventController(DefaultContext context, Microsoft.Extensions.Options.IOptions<DataOptions> options, FileService fileService, ClientService clientService, IEmailService emailService)
        {
            _context = context;
            _options = options.Value;
            _fileService = fileService;
            _clientService = clientService;
            _emailService = emailService;
        }


        [Authorize(Policy = "ApiUser")]
        [HttpPost("intro")]
        public async Task<ActionResult<WebEventIntroResponse>> PostEventIntro([FromForm] WebEventIntro request)
        {
            DateTime now = DateTime.Now;

            WebEvent newEvent = new WebEvent()
            {
                Title = request.Title,
                Intro = request.Intro,
                CategoryId = request.CategoryId,
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Address = "",
                Publish = false,
                Audience = "",
                AddInfo = "",
                RegistrationURL = "",
                Email = "",
                EmailSubject = "",
                CreatedDate = now,
                CreatedBy = request.UserId,
                LastUpdated = now,
                LastUpdatedBy = request.UserId,
                IsDeleted = false,
                DeletedBy = 0
            };

            try
            {
                _context.WebEvents.Add(newEvent);
                await _context.SaveChangesAsync();
            }
            catch
            {
                return BadRequest();
            }

            WebEventIntroResponse response = new WebEventIntroResponse();
            response.webEvent = await _context.WebEvents.FindAsync(newEvent.Id);

            int imageWidth = 3125;
            int imageHeight = 1250;

            foreach (IFormFile formFile in request.Thumbnails)
            {
                if (formFile.Length > 0)
                {
                    var fileExt = System.IO.Path.GetExtension(formFile.FileName).Substring(1).ToLower();
                    if (!_fileService.checkFileExtension(fileExt, new[] { "jpg", "jpeg", "png" }))
                    {
                        response.Errors.Add(new Error("extension", "Please upload PNG or JPG file only."));
                    }
                    else
                    {
                        string randomName = Path.GetRandomFileName() + "." + fileExt;
                        string fileDir = Path.Combine(_options.AssetsRootDirectory, @"events", newEvent.Id.ToString());
                        if (_fileService.CheckAndCreateDirectory(fileDir))
                        {
                            var fileName = Path.Combine(fileDir, randomName);

                            Stream stream = formFile.OpenReadStream();
                            using (var imagedata = System.Drawing.Image.FromStream(stream))
                            {
                                _fileService.ResizeImage(imagedata, imageWidth, imageHeight, fileName, fileExt);
                            }
                            stream.Dispose();

                            Error e = await SaveImageToDb(new string[] { formFile.FileName, randomName, fileExt }, newEvent.Id, 0, 0, 0, 0, 1, now, request.UserId);
                            if (!e.Code.Equals("ok"))
                            {
                                response.Errors.Add(e);
                            }
                            else
                            {
                                response.ThumbnailURL = getAssetsUrl(newEvent.Id, randomName, "");
                            }
                        }
                        else
                        {
                            response.Errors.Add(new Error("directory", "Error in creating directory."));
                        }

                    }
                }
            }

            return response;
        }

        /**
         * @api {get} /event/topic GET list topik
         * @apiVersion 1.0.0
         * @apiName WebEventTopics
         * @apiGroup Event
         * @apiPermission Basic Authentication
         * 
         * @apiSuccessExample Success-Response:
         * [
         *     {
         *         "id": 1,
         *         "text": "Strategy"
         *     },
         *     {
         *         "id": 2,
         *         "text": "Process"
         *     },
         *     {
         *         "id": 3,
         *         "text": "Performance"
         *     },
         *     {
         *         "id": 4,
         *         "text": "People"
         *     },
         *     {
         *         "id": 5,
         *         "text": "Culture"
         *     }
         * ]
         */
        [AllowAnonymous]
        [HttpGet("topic")]
        public async Task<ActionResult<List<GenericInfo>>> WebEventTopics()
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            authHeader = authHeader.Trim();
            if (authHeader.Equals(""))
            {
                return Unauthorized();
            }

            string encodedCredentials = authHeader.Substring(6);
            var credentialBytes = Convert.FromBase64String(encodedCredentials);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
            var username = credentials[0];
            var password = credentials[1];
            if (username != "onegmlapi" || password != "O1n6e0G4M7L")
            {
                return Unauthorized();
            }

            var query = from topic in _context.WebTopicCategories
                        where !topic.IsDeleted
                        select new GenericInfo()
                        {
                            Id = topic.Id,
                            Text = topic.Category
                        };

            return query.ToList<GenericInfo>();
        }

        /**
         * @api {get} /event/holiday/{month}/{year} GET hari libur
         * @apiVersion 1.0.0
         * @apiName GetHolidayInfo
         * @apiGroup Event
         * @apiPermission Basic Authentication
         * 
         * @apiSuccessExample Success-Response:
         * [
         *     {
         *         "id": 1,
         *         "description": "Tahun Baru 2021 Masehi ",
         *         "date": "2021-01-01T00:00:00",
         *         "type": "national-holiday"
         *     }
         * ]
         */
        [AllowAnonymous]
        [HttpGet("holiday/{month}/{year}")]
        public async Task<ActionResult<List<HolidayInfo>>> GetHolidayInfo(int month, int year)
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            authHeader = authHeader.Trim();
            if (authHeader.Equals(""))
            {
                return Unauthorized();
            }

            string encodedCredentials = authHeader.Substring(6);
            var credentialBytes = Convert.FromBase64String(encodedCredentials);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
            var username = credentials[0];
            var password = credentials[1];
            if (username != "onegmlapi" || password != "O1n6e0G4M7L")
            {
                return Unauthorized();
            }

            DateTime start = new DateTime(year, month, 1);
            DateTime end = new DateTime(year, month, DateTime.DaysInMonth(year, month));

            var query = from holiday in _context.WebEventHolidays
                        join t in _context.WebEventHolidayTypes on holiday.TypeId equals t.Id
                        where holiday.Date >= start && holiday.Date <= end && !holiday.IsDeleted && !t.IsDeleted
                        select new HolidayInfo()
                        {
                            Id = holiday.Id,
                            Date = holiday.Date,
                            Description = holiday.Description.Trim(),
                            Type = t.Type
                        };

            return await query.ToListAsync();
        }

        /**
         * @api {get} /event/holiday/{year} GET hari libur setahun
         * @apiVersion 1.0.0
         * @apiName GetHolidayInfoYear
         * @apiGroup Event
         * @apiPermission Basic Authentication
         * 
         * @apiSuccessExample Success-Response:
         * [
         *     {
         *         "id": 1,
         *         "description": "Tahun Baru 2021 Masehi ",
         *         "date": "2021-01-01T00:00:00",
         *         "type": "national-holiday"
         *     }
         * ]
         */
        [AllowAnonymous]
        [HttpGet("holiday/{year}")]
        public async Task<ActionResult<List<HolidayInfo>>> GetHolidayInfoYear(int month, int year)
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            authHeader = authHeader.Trim();
            if (authHeader.Equals(""))
            {
                return Unauthorized();
            }

            string encodedCredentials = authHeader.Substring(6);
            var credentialBytes = Convert.FromBase64String(encodedCredentials);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
            var username = credentials[0];
            var password = credentials[1];
            if (username != "onegmlapi" || password != "O1n6e0G4M7L")
            {
                return Unauthorized();
            }

            DateTime start = new DateTime(year, 1, 1);
            DateTime end = new DateTime(year, 12, 31).AddHours(24);

            var query = from holiday in _context.WebEventHolidays
                        join t in _context.WebEventHolidayTypes on holiday.TypeId equals t.Id
                        where holiday.Date >= start && holiday.Date <= end && !holiday.IsDeleted && !t.IsDeleted
                        select new HolidayInfo()
                        {
                            Id = holiday.Id,
                            Date = holiday.Date,
                            Description = holiday.Description.Trim(),
                            Type = t.Type
                        };

            return await query.ToListAsync();
        }

        /**
         * @api {get} /event/location GET list location
         * @apiVersion 1.0.0
         * @apiName WebEventLocation
         * @apiGroup Event
         * @apiPermission Basic Authentication
         * 
         * @apiSuccessExample Success-Response:
         * [
         *     {
         *         "id": 1,
         *         "text": "Virtual"
         *     },
         *     {
         *         "id": 2,
         *         "text": "Jakarta"
         *     },
         *     {
         *         "id": 3,
         *         "text": "Medan"
         *     },
         *     {
         *         "id": 4,
         *         "text": "Surabaya"
         *     },
         *     {
         *         "id": 5,
         *         "text": "Makassar"
         *     }
         * ]
         */
        [AllowAnonymous]
        [HttpGet("location")]
        public async Task<ActionResult<List<GenericInfo>>> WebEventLocation()
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            authHeader = authHeader.Trim();
            if (authHeader.Equals(""))
            {
                return Unauthorized();
            }

            string encodedCredentials = authHeader.Substring(6);
            var credentialBytes = Convert.FromBase64String(encodedCredentials);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
            var username = credentials[0];
            var password = credentials[1];
            if (username != "onegmlapi" || password != "O1n6e0G4M7L")
            {
                return Unauthorized();
            }

            var query = from topic in _context.WebEventLocations
                        where !topic.IsDeleted
                        select new GenericInfo()
                        {
                            Id = topic.Id,
                            Text = topic.Location
                        };

            return query.ToList<GenericInfo>();
        }

        /**
         * @api {get} /event/cdhx/categories GET list CDHX categories
         * @apiVersion 1.0.0
         * @apiName GetEventCdhxCategories
         * @apiGroup Event
         * @apiPermission Basic Authentication
         * 
         * @apiSuccessExample Success-Response:
         * [
         *      {
         *          "id": "hr-academy",
         *          "text": "HR Academy"
         *      },
         *      {
         *          "id": "sales-academy",
         *          "text": "Sales Academy Professional"
         *      }
         * ]
         */
        [AllowAnonymous]
        [HttpGet("cdhx/categories")]
        public async Task<ActionResult<List<GenericString>>> GetEventCdhxCategories()
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            authHeader = authHeader.Trim();
            if (authHeader.Equals(""))
            {
                return Unauthorized();
            }

            string encodedCredentials = authHeader.Substring(6);
            var credentialBytes = Convert.FromBase64String(encodedCredentials);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
            var username = credentials[0];
            var password = credentials[1];
            if (username != "onegmlapi" || password != "O1n6e0G4M7L")
            {
                return Unauthorized();
            }

            return await GetListCdhxCategories();
        }

        private async Task<List<GenericString>> GetListCdhxCategories()
        {
            var query = from cat in _context.WebEventCdhxCategories
                        where !cat.IsDeleted
                        select new GenericString()
                        {
                            Id = cat.Slug,
                            Text = cat.Category
                        };

            return await query.ToListAsync();
        }

        /**
         * @api {post} /event/cdhx/categories} POST list CDHX categories
         * @apiVersion 1.0.0
         * @apiName PostEventCdhxCategories
         * @apiGroup Event
         * @apiPermission ApiUser
         * 
         * @apiParamExample {json} Request-Example:
         * {
         *   "userId": 35,
         *   "items": [
         *     {
         *         "id": "sales-academy",
         *         "text": "Sales Academy"
         *     },
         *     {
         *         "id": "",
         *         "text": "HR Academy"
         *     }
         *   ]
         * }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost("cdhx/categories")]
        public async Task<ActionResult> PostEventCdhxCategories(PostCdhxCategories post)
        {
            List<WebEventCdhxCategory> curCategories = await _context.WebEventCdhxCategories.Where(a => !a.IsDeleted).ToListAsync();

            List<GenericString> usedCats = new List<GenericString>();

            DateTime now = DateTime.Now;

            foreach(GenericString cat in post.Items)
            {
                if(string.IsNullOrEmpty(cat.Id.Trim()))
                {
                    string slug = GenerateSlug(cat.Text);
                    WebEventCdhxCategory newCat = new WebEventCdhxCategory()
                    {
                        Slug = slug,
                        Category = cat.Text,
                        CreatedDate = now,
                        CreatedBy = post.UserId,
                        LastUpdated = now,
                        LastUpdatedBy = post.UserId,
                        IsDeleted = false,
                        DeletedBy = 0
                    };
                    _context.WebEventCdhxCategories.Add(newCat);
                    usedCats.Add(new GenericString()
                    {
                        Id = slug,
                        Text = cat.Text
                    });
                }
                else
                {
                    WebEventCdhxCategory curCat = curCategories.Where(a => a.Slug.Equals(cat.Id)).FirstOrDefault();
                    if (curCat == null)
                    {
                        WebEventCdhxCategory newCat = new WebEventCdhxCategory()
                        {
                            Slug = cat.Id,
                            Category = cat.Text,
                            CreatedDate = now,
                            CreatedBy = post.UserId,
                            LastUpdated = now,
                            LastUpdatedBy = post.UserId,
                            IsDeleted = false,
                            DeletedBy = 0
                        };
                        _context.WebEventCdhxCategories.Add(newCat);
                        usedCats.Add(new GenericString()
                        {
                            Id = cat.Id,
                            Text = cat.Text
                        });
                    }
                    else
                    {
                        if(!curCat.Category.Equals(cat.Text))
                        {
                            curCat.Category = cat.Text;
                            curCat.LastUpdated = now;
                            curCat.LastUpdatedBy = post.UserId;
                            _context.Entry(curCat).State = EntityState.Modified;
                        }

                        usedCats.Add(new GenericString()
                        {
                            Id = curCat.Slug,
                            Text = curCat.Category
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();

            List<WebEventCdhxCategory> newCats = await _context.WebEventCdhxCategories.Where(a => !a.IsDeleted).ToListAsync();
            foreach(WebEventCdhxCategory newCat in newCats)
            {
                if (usedCats.Where(a => a.Id.Equals(newCat.Slug) && a.Text.Equals(newCat.Category)).Any()) continue;
                newCat.IsDeleted = true;
                newCat.DeletedBy = post.UserId;
                newCat.DeletedDate = now;
                _context.Entry(newCat).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /**
         * @api {post} /event/register POST registrasi 
         * @apiVersion 1.0.0
         * @apiName PostEventRegister
         * @apiGroup Event
         * @apiPermission Basic Auth
         * 
         * @apiParamExample {json} Request-Example:
         * {
         *   "id": 0,
         *   "eventId": 262,
         *   "company": "PMLA",
         *   "companyType": "BUMN",
         *   "statusPPN": "Pemungut PPN",
         *   "npwp": "123.45678.909-1",
         *   "address": "Jl. Kota Baru, Surakarta",
         *   "contactPerson": "Jane Doe",
         *   "telephone": "0245-78787879",
         *   "fax": "0245-78787878",
         *   "handphone": "0898-8989-8989",
         *   "email": "janedoe@pmla.com",
         *   "mailAddress": "Jl. Kota Baru No. 1, Surakarta",
         *   "city": "Surakarta",
         *   "kecamatan": "Surakarta Timur",
         *   "postCode": "14115",
         *   "voucher": "ABCDEF",
         *   "reference": "FB",
         *   "jenisPelatihan": "Hybrid"
         *   "participants": [
         *     {
         *       "name": "John Doe",
         *       "jobTitle": "CEO",
         *       "department": "Head Office",
         *       "handphone": "0898-09090909",
         *       "email": "johndoe@pmla.com",
         *       "gender": "L"
         *     }
         *   ]
         * }
         */
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult<WebEventRegistration>> PostEventRegister(WebEventRegister register)
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            authHeader = authHeader.Trim();
            if (authHeader.Equals(""))
            {
                return Unauthorized();
            }

            string encodedCredentials = authHeader.Substring(6);
            var credentialBytes = Convert.FromBase64String(encodedCredentials);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
            var username = credentials[0];
            var password = credentials[1];
            if (username != "onegmlapi" || password != "O1n6e0G4M7L")
            {
                return Unauthorized();
            }

            WebEventRegistration registration = new WebEventRegistration();
            registration.EventId = register.EventId;
            registration.Company = register.Company;
            registration.CompanyType = register.CompanyType;
            registration.StatusPPN = register.StatusPPN;
            registration.NPWP = register.NPWP;
            registration.Address = register.Address;
            registration.ContactPerson = register.ContactPerson;
            registration.Telephone = register.Telephone;
            registration.Fax = register.Fax;
            registration.Handphone = register.Handphone;
            registration.Email = register.Email;
            registration.MailAddress = register.MailAddress;
            registration.City = register.City;
            registration.Kecamatan = register.Kecamatan;
            registration.PostCode = register.PostCode;
            registration.Voucher = register.Voucher;
            registration.Reference = register.Reference;
            registration.JenisPelatihan = register.JenisPelatihan;
            registration.KeteranganPembayaran = register.KeteranganPembayaran;
            registration.Payment = 0;
            registration.LastUpdated = DateTime.Now;
            registration.LastUpdatedBy = 0;
            registration.IsDeleted = false;
            registration.DeletedBy = 0;
            registration.DeletedDate = new DateTime(1970, 1, 1);
            _context.WebEventRegistrations.Add(registration);
            await _context.SaveChangesAsync();

            if(register.Participants != null && register.Participants.Count() > 0)
            {
                foreach (WebEventPart part in register.Participants)
                {
                    WebEventRegParticipant p = new WebEventRegParticipant()
                    {
                        RegistrationId = registration.Id,
                        Participant = part.Name,
                        JobTitle = part.JobTitle,
                        Department = part.Department,
                        Handphone = part.Handphone,
                        Email = part.Email,
                        Gender = part.Gender
                    };
                    _context.WebEventRegParticipants.Add(p);
                }
                await _context.SaveChangesAsync();
            }

            return registration;
        }

        [AllowAnonymous]
        [HttpPost("UploadESign")]
        public async Task<ActionResult<WebEventImageResponse>> UploadESign([FromForm] WebEventSignatureRequest request)
        {
            WebEventImageResponse response = new WebEventImageResponse();

            if (request.RegistrationId == 0)
            {
                response.Errors.Add(new Error("event", "Registration id cannot be null or 0."));
            }
            else if (!RegistrationExists(request.RegistrationId))
            {
                response.Errors.Add(new Error("event", "Invalid registration id."));
            }
            else
            {

                // string tes = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAMoAAABgCAYAAABc4u0gAAAAAXNSR0IArs4c6QAAEJJJREFUeF7tnQesLUUZx//PYAmIigKikigIYlTsBsWKQSUWVFQSKypGRcWK0ViimCj4VKxIMBoLookFRVTAXqMGsRB7FxHsjRgbUfO7b777vjtnz9md3Tlny5lJbnjcuzs788385+vfbFN9213SWyTtI+lGknaT9H5Jn5T04frXyxOFAuOnwLaaKdxE0vkBIPMefbekEyT9dvzkKDMoFKimQB1Qvi7pdg2J9yhJZzZ8tjxWKDAqCiwCykslvSSazeckfTOIX0+smOnRQSwbFRHKYAsF6iiwCCj/i15+qqQzJF0efo++8nZJB0ja1z17mCQAVVqhwGQoMA8o95D02WiWi0B1qqSnhOd/IelxBSyT2SNlIpJyAWUvSe+TBMBo/5R0J0nfKlQuFJgCBVJErzpl/dqStks61nGW/aZApDKHQoFFQPmppP0difCdoKwvaleXdK6ku4SHLpR0+0LmQoGxU2ARUJ4j6dXRBJsq6qdLMqsYij3vlVYoMFoK1PlRYstXyqb3YGnCjUZLxDLw6VOgDihVvpSmvhIU+yMlPSuQEVEOD34Je5n+vprcDOuAgoJ+QaSrXCLp0Qnm3xhsAAXz8V8mR80yoclSoA4oTLyKq3wlKPaApkl7bHBO+mefKen1TV4uzxQK9E2BJkBhjKc4EcrGnGrRepCk10m6oZs0fha4S/G39L0TyvcXUqApUOZxltdKenYCjW8t6R2SbhW9gziGLoNXv7RCgcFRIAUoDJ6wFvO+22TIVXlSwsyuFcDywOgddBZAdGLRXxKoWR5NoQDxiQT6fj4cyo1jElOBwqAeFsJV/ADfLImgyZSGjgJHihtiGNyl8SRSPjqRZ8kTun6hUfJq/jrQzV5kj71NEjlVC1sboNAhCj6hKj5q+HkhhKXum/7vcCe4iNdb7O/8Hv2ltB1cnJ/rOUcudPmupDdJOlvSZYVQCymA2E+KSFV7TXBdzO2gLVDoEM89G/nmoXdEJ/4/1U+CKAbwnlExSnSWB6+xsg84iI6oS577jaTfSfpQ4DKFG1dv+SrVwZ78QJCWKt/sAhQ6PCSkCl+zI1h4HasYXMT6sgEDQEQx/rZO7WaSviyJgyS1nSPprDWkWRM6IbY+IjwYS0VzI0+6AoXvsZDoFSY+sbHhAm1ONfrChHxMNGNT9M3L34QgY34GpfNHkq4cTeJSSfiuoDei1gMk3XbBRM+T9Pgils2lEOLYx4NIaw9VhlvlAEpusNAfDkoAE3OXn0g6cMwIaDh2MkehgbU/hMQ4FtG3q0q6hSTEhitCtmn8CTJS8YO9J4Cv4RDW5jHEWsRbb83F8oo6sNlyAcXAgk7hNzecJVVnscFxqiJu3T1asosDx2nDscay+n+StIcb7AskndRg8EcFLuNBZq+h+GNp/FSDftbxERIPseha2xLTmBMofIDNzQb2YlgX/WKRoo8cTtDl1Bq6CZvaWmoEBO9xSn5G0jUi4iC24ewFiKXNUsAr+7+XtLc9khso9IvcBxfxJl+sYV2UcU7IN0oiMSxu95rYKZlar2DRhn+DpOOjBzjIOC3ZCKVtpQAH/UedJXfTP7gMoFRxFn4HZ0HvaNvuKuk0NwnrhwXH2RnL722/M4T34jygLuuErE3hD+oaWPtChUg7hHkPYQw+gJe9BQe+rMsC1E0KdMJZfFzXjJJU10nF32NZ0h65b0hDbtHl4F75okunZnBNM0vnTaQqmqKL/jg4gmUekBfBNvbsMoFinCUGSw6Pe1XoP4aE+0zEsgONvIm866amfjRiBAVCrKVkq2beh4PvLj5Y9ls2UKAICjlg8dYrRLCuPpGqUxI95umDX4b6ASJmIh5Z6yq20g+KPQeMp/v9gh+hfkTr9QR79seS9gzTfugqgGIkBhw+TGVZnOWgCXAVxNafu735zsiv0nbbclh5s3rhKvMpyaH7tPDnV64SKHyTMBXikayR4Yhtv0v7VRScmYNbdRlPrne9Qk9YeJze0PY7cbzTqvdA23Gv+j1M7BSpp23vg0hxxDBiGTJ42xaLYLlO37bjyfUep72Jq4TweAdkl288JHjyrQ9qsBFTVtpWChwe7gDityf2ARQ+jK+Fk982AmIYsnObghOEnhMDZa1NbswQNwkHiE9uAyht6BPPjfixP4cbCfjbESGwdYg06HNM3mB0WF9AgQBxpmNbsMRxUV1NqX0ujv92bNm7TcZ0A2Lmbhw+RnYqWaqlbaWAF1G39QkUGxY6ysmSCPBLBUsMEpyOhE7b1RRjXvy4ck1XE7GnBbkr5oDsGjUxZhovGvt3gnObikOHDgEoDNbnojSt+/XIKIWzTUzUkBc5DmXJ4ay1+XpDwVQ4cO61/KMk6tp9g9i5oQCFSXolv64qC+bTD7pcDHI3ECGmFFGMaIouYS2HhZC+KLxO1U5r3DhQqt/Mwszy6zfyU4YElBgsbHrEgqpFjOuM3VHS13IfKQPoj7lbcGkuEzGOR5K+dg1RxBSp+NcA5jqkIXhuvuFrGhpQDCxwFPJayORDNDBrD7ZtquT7+yOHOIdci+5DWXKZiL0zE8AAlNK2UmAUQIk5C2B5RVCs/OWrRHYyoe9NeJVjhT6HidgDBY5VLnua3UAeKCsJiuyyh71j7L+SruQ6m0pMVx19Yj0lh+XLO2inZgCpo2fTv/sDasMqOESxBTSzmFjCYrEAkYzc7ynlntQtnhe/cij0iK3cXUPDI2+3o9WNY53+7mm0YRXsEyjXkXSwJMrHoHvcO0S4YpLzjYtTrxZ+kSOQcmwL7uPjEENxPHZp3pFZgiKrKemdjUsHCmID8jANxfyekrDfXzckc3Fr8KIG1zhf0kck3dkFU67j4nrrV1dzrt8E60jLJodMFqAgGrHhLZqVuC0r0gYwDBxNBmTPEKtFpKaVvCSLMVbS/Um4bpzF12nuGnLinY05nZgp6z30Z9mLSDkU+aAc1Nx75m0isH1SeQEDAOC/XdsZkqhTdVGw5X9a0g8bduqVrNRwl4afGORjHETfl7RPSLQi4apNQ8z1tObGZhT60nZSwFu8Ng/kKh3FCkIf58u1dKTkt0McF8F4VLno0vzpuk6ig098ayt+3S1ceQD9CdG4QXE2zmxFL7lsclyAApfg+gWC5HZrKTrFX/trCCfBSsVPjvBw/w3A7J2SmE2nHobhq7G3DWT0myCXA7PLoTfEd33xwU1Gwj9IOW2jV/hJwjGwyPDDKb+Kq+Z8bBggYfNMKdarahNBV0ThttYvr6SWK81nKezFri9JonbBRgMoFqrOZiOkAWsU/yYWCFPtLuFZwMDv7YfFsn/3dTLEZVenfoGqFzvbRP36tOm2XKmvtV7Fd73/hALom2qCsRaQNO80ZjMOXaxBpGATYYbumlq8igVp+w0ffpKa8hyH7VM0kGzQ0nZSwFsEt+iBfToccy+QF8WQvzkRYJ9Ta5YizBxZzKb6HxEND3fEoK5uKau6kyDEEVoF+5l08ikBhSlbUe/HhGIMWIoI+xg6R0wBs/fUNxWf4vJH3Fv4hJSPrsGzC5PZpgYUW09/TTcgwczXpUj40PYJXMTSEJqEtMQp08V/snVFTw31mfltpRN2qkAx7oLeYqH5dVmTQwPDovF4M2+dTyW+fTn1uvMx0aXNWG8pCUOVNe4knUndmDJQPHexayg4iRHHODXG3Joq9ehtLw5xdsyXMqF450vbSQFAAlholOMlhWOmrQNQbNL+FEYc4/+xHI21WYG8RY7DuPI/JW25M6W0HRSYSdCaR5h1Ago0iP0u+IIovDdGR+VMclG0yHFdMK6kIxnubwUlmxQwLzwHJ3fInFuAspUCnCSIYHZ3y1g5jIXfxym9MUio5kIBDqrVlDbLTWp9b+vGUeJNgqkVwFilEzYc1jFEsjGYlKs89WSHMiefHVrC6WePB7usiQo0VKb596ITZN2BYrRBjOHH3+ECYBDJhqzH4Deym5gZ61kVOsg6RVg35Zb+QtkXhuIlC98tQNlKHnQYTmlAY9eAsxFhzZzKTb3gTRcsx3NVt49Zv5QD5ertMepgOWgzrw9Ps0pzcPxiAcr85UAsAzC+ojyAOXtJqQNtNwbgJhnrKlEHJTp4PkXR2eDGZ0bX9c19owClfntCUJR/OI2JZnCZj4V7Ri6Q9Pf6bpb2RNXlr3CQo6KSrEsbwMg6fpWkE0KVTKyAjbhtAUraKhMaA5eB21Akg+ownNx4ctFpVmkAsIS7+CYuUoaRwUubpcD9Jb0rxAEm1YYrQGm/nQgqpKK+bVTLzaFGcFVxjPZf2vrmgeG7vmpm3HddWEuusYytHyu8TdEIOG5jc3kBSvel5vSGy3DPPYqhNYBDXvoPQkKciWcWyp3yZa4Ff3KQq6vucvyHpOdKenkwQqTmqqSMZYzPIj6/N9wuxviTk94KUPIuO6A5OvhlbhqcfFVfAESfCHFXLCANBZPSOEdGqdSAcFHDZ3JaOB29t75wlR1UAyRETyMu01if5EqjBSh5gRL3Blj2DTWiUBzvkOlzWN6wdGGFwwTsm8WA8YxtjkyfHWU3viRta05bgLL6tQcsB0g6KFjRuJLPTLsUXatqPwuRv19toP/4QL+c9z6unlLdv+hB8ssuRVQKULovRs4erNKmt57xu0YmTDcQ2yDr6pWPxS1AgpWwtcO4ACXnNh9OX/66iGTFdTjTaDUSDhZ0EjN6UGOOf3cqoVWA0motRvGSBUyuE1cBJNQu83XqshwUBSij2POtB2mK/TpYwDBcwEmsYDychN+liq2VxC5Aab0HR/GiKfaku1L18PJRjDp9kL4yDW+jkzD3bJESBSjpizK2N94q6dhQcnZKlWhsHeLiGRwKgKS14l61wAUoY9v26eMlYuBlknYPRQEbh22kf2rlb8SXwRI+BHfJChJmVYCy8rXt5YPbQ4gLhSUoMDGFtjKQFKBMYbs0mwPpARR5o23cIDXyFivuRCEAnOycxOhUOMrId0zC8C2rjzgnautmsQYlfD/Xo+gfVEuxC3BXEqpTgJJr+cbRzymuPBNgSQ4O7HmacA0K+u0fxrESkBTRq+dV7+nzdpkQptPjw1V1YzEbc0s016zT4hJNSyVn4ShLJe9gOzfOwgBPl3RyTp/Dkmbtbwtb+SFfgLKkVR1Bt+gsz5dE9PLQK2bGlWa6XiGevDwFKMkkm9QL3lmHxYiSTCSCDa1x4dGeYVC9xK4VoAxtS6x+PHG1TLz3ACZb+EfHKcVVZnq5Uq8ApeMqTuR1f/GSKcpcSd4pND0DbSgPC1CsYaUDKCu/Uq8AJcNqTqQLQtPRBY5x80E0I312aY68BbTzt2DZY71lbBagTGSXZ5xGHGTYh6KPQ/EIN6dLJHHvZJsKNllIU4CShYyT6yQWxZggSjS6yzI8+jgSKed0uKRD3HUcRtiVW7niFS1AmdwezzohuAunuBUsp3M4DJaxtlX+95K0iyQ2P0XoDl4wYnSRF0ni3sleWwFKr+QfxcfJGAQw/HjAYBU7T9JxbhaUZqLKDBzJ6pNZWu4eFZxiEQHgMIdKumgIVCpAGcIqjGcMiEhwGLt4iZFfGjIKKbk0r9xS6gwR8ahZ1rfVbXPcBSipS1iehwLxbcNtqYKSfoWkkyRdKOniPky/TQZfgNKESuWZRRSAy+wqae+w0c8J0b2IaWRTWknY/4QsS2oxmzNzGYaBpazW/wHD6RjSO9RpEwAAAABJRU5ErkJggg==";

                string randomname = Path.GetRandomFileName() + ".png";
                string filedir = Path.Combine(_options.AssetsRootDirectory, @"registrations", request.RegistrationId.ToString());
                if (_fileService.CheckAndCreateDirectory(filedir))
                {
                    var filename = Path.Combine(filedir, randomname);
                    var bytess = Convert.FromBase64String(request.Image.Substring(22));
                    using (var imageFile = new FileStream(filename, FileMode.Create))
                    {
                        imageFile.Write(bytess, 0, bytess.Length);
                        imageFile.Flush();
                    }
                    await UpdateSignature( randomname, request.RegistrationId);
                }
            }

            return response;
        }

        /**
         * @api {get} /event/register/{eventId}/{page}/{perPage}/{search} GET list registrasi
         * @apiVersion 1.0.0
         * @apiName GetEventRegistrations
         * @apiGroup Event
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} eventId         0 untuk semua event, atau id dari event yang diinginkan
         * @apiParam {Number} page            Halaman yang ditampilkan. 
         * @apiParam {Number} perPage         Jumlah data per halaman.  
         * @apiParam {String} search          Kata yang mau dicari di nama Contact Person atau nama Perusahaan
         * 
         * @apiSuccessExample Success-Response:
         * {
         *     "items": [
         *         {
         *             "id": 1,
         *             "event": {
         *                 "id": 262,
         *                 "text": "Agile 4.0 Organization Design"
         *             },
         *             "company": "PMLA",
         *             "contactPerson": "Jane Doe",
         *             "telephone": "0245-78787879",
         *             "fax": "0245-78787878",
         *             "handphone": "0898-8989-8989",
         *             "email": "janedoe@pmla.com",
         *             "payment": 0,
         *             "participants": 1
         *         }
         *     ],
         *     "info": {
         *         "page": 1,
         *         "perPage": 10,
         *         "total": 1
         *     }
         * }         
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("register/{eventId}/{page}/{perPage}/{search}")]
        public async Task<ActionResult<ListWebEventRegister>> GetEventRegistrations(int eventId, int page, int perPage, string search)
        {
            search = search.Trim().ToLower();
            Func<WebEvent, bool> WherePredicateEvent = e => {
                return eventId == 0 || eventId == e.Id;
            };

            Func<WebEventRegistration, bool> WherePredicateReg = reg => {
                return search.Equals("*") || reg.ContactPerson.ToLower().Contains(search) || reg.Company.ToLower().Contains(search);
            };

            var query = from reg in _context.WebEventRegistrations
                        join ev in _context.WebEvents on reg.EventId equals ev.Id
                        where WherePredicateEvent(ev) && WherePredicateReg(reg) && !reg.IsDeleted
                        orderby reg.LastUpdated descending
                        select new
                        {
                            reg.Id,
                            reg.EventId,
                            ev.Title,
                            reg.Company,
                            reg.ContactPerson,
                            reg.Telephone,
                            reg.Fax,
                            reg.Handphone,
                            reg.Email,
                            reg.Payment,
                            reg.LastUpdated
                        };
            var objs = await query.Skip(perPage * (page - 1)).Take(perPage).ToListAsync();

            ListWebEventRegister response = new ListWebEventRegister();
            response.items = new List<WebEventRegisterItem>();
            response.info = new PaginationInfo(page, perPage, query.Count());

            foreach(var obj in objs)
            {
                WebEventRegisterItem item = new WebEventRegisterItem();
                item.Id = obj.Id;
                item.Event = new GenericInfo()
                {
                    Id = obj.EventId,
                    Text = obj.Title
                };
                item.Company = obj.Company;
                item.ContactPerson = obj.ContactPerson;
                item.Telephone = obj.Telephone;
                item.Fax = obj.Fax;
                item.Handphone = obj.Handphone;
                item.Email = obj.Email;
                item.Payment = obj.Payment;
                item.Participants = _context.WebEventRegParticipants.Where(a => a.RegistrationId == obj.Id).Count();
                item.RegistrationDate = obj.LastUpdated;
                item.Free = !_context.WebEventInvestments.Where(a => a.EventId == obj.EventId && a.Nominal > 0 && !a.IsDeleted).Any();
                response.items.Add(item);
            }
            return response;
        }

        /**
         * @api {get} /event/register/{registrationId} GET detail registrasi
         * @apiVersion 1.0.0
         * @apiName GetEventRegistrationDetail
         * @apiGroup Event
         * @apiPermission ApiUser
         * 
         * @apiSuccessExample Success-Response:
         * {
         *     "id": 1,
         *     "event": {
         *         "id": 262,
         *         "text": "Agile 4.0 Organization Design"
         *     },
         *     "company": "PMLA",
         *     "companyType": "BUMN",
         *     "statusPPN": "Pemungut PPN",
         *     "npwp": "123.45678.909-1",
         *     "address": "Jl. Kota Baru, Surakarta",
         *     "contactPerson": "Jane Doe",
         *     "telephone": "0245-78787879",
         *     "fax": "0245-78787878",
         *     "handphone": "0898-8989-8989",
         *     "email": "janedoe@pmla.com",
         *     "mailAddress": "Jl. Kota Baru No. 1, Surakarta",
         *     "city": "Surakarta",
         *     "postCode": "14115",
         *     "voucher": "ABCDEF",
         *     "reference": "FB",
         *     "payment": 0,
         *     "participants": [
         *         {
         *             "name": "John Doe",
         *             "jobTitle": "CEO",
         *             "handphone": "0898-09090909",
         *             "email": "johndoe@pmla.com",
         *             "gender": "L"
         *         }
         *     ]
         * }         
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("register/{registrationId}")]
        public async Task<ActionResult<WebEventRegisterDetail>> GetEventRegistrationDetail(int registrationId)
        {
            var query = from reg in _context.WebEventRegistrations
                        join ev in _context.WebEvents on reg.EventId equals ev.Id
                        where reg.Id == registrationId && !reg.IsDeleted
                        select new WebEventRegisterDetail
                        {
                            Id = reg.Id,
                            Event = new WebEventResponse(ev),
                            Company = reg.Company,
                            CompanyType = reg.CompanyType,
                            StatusPPN = reg.StatusPPN,
                            NPWP = reg.NPWP,
                            Address = reg.Address,
                            ContactPerson = reg.ContactPerson,
                            Telephone = reg.Telephone,
                            Fax = reg.Fax,
                            Handphone = reg.Handphone,
                            Email = reg.Email,
                            MailAddress = reg.MailAddress,
                            City = reg.City,
                            Kecamatan = reg.Kecamatan,
                            PostCode = reg.PostCode,
                            Voucher = reg.Voucher,
                            Reference = reg.Reference,
                            JenisPelatihan = reg.JenisPelatihan,
                            Payment = reg.Payment,
                            Signature = reg.Signature,
                            KeteranganPembayaran = reg.KeteranganPembayaran,
                            Participants = new List<WebEventPart>()
                        };
            WebEventRegisterDetail response = query.FirstOrDefault();
            if (response == null) return NotFound();

            if(response.Event != null)
            {
                response.Event.Category = GetEventCategory(response.Event.Event.CategoryId);
                response.Event.Topic = GetEventTopic(response.Event.Event.TopicId);
                response.Event.Location = GetEventLocation(response.Event.Event.LocationId);
                response.Event.Description = GetEventDescription(response.Event.Event.Id);
                response.Event.Thumbnail = GetEventThumbnails(response.Event.Event.Id);
                response.Event.Brochure = GetWebEventBrochures(response.Event.Event.Id);
                response.Event.Flyer = GetWebEventFlyers(response.Event.Event.Id);
                response.Event.Agenda = GetWebEventAgendas(response.Event.Event.Id);
                response.Event.Testimonies = GetWebEventTestimonies(response.Event.Event.Id);
                response.Event.Investments = GetWebEventInvestments(response.Event.Event.Id);
                response.Event.Takeaways = GetWebEventTakeaways(response.Event.Event.Id);
                response.Event.Speakers = GetWebEventSpeakers(response.Event.Event.Id);

                WebEventNotification notification = _context.WebEventNotifications.Where(a => a.EventId == response.Event.Event.Id && !a.IsDeleted).FirstOrDefault();
                if (notification == null)
                {
                    response.Event.EmailNotification = false;
                    response.Event.EmailSubject = "";
                    response.Event.Email = "";
                }
                else
                {
                    response.Event.EmailNotification = notification.EmailNotification;
                    response.Event.EmailSubject = notification.EmailSubject;
                    response.Event.Email = notification.Email;
                }

                response.Free = !_context.WebEventInvestments.Where(a => a.EventId == response.Event.Event.Id && a.Nominal > 0 && !a.IsDeleted).Any();
                response.SignatureURL = getAssetsRegistrationsUrl(registrationId, response.Signature);
            }
                
            var q = from part in _context.WebEventRegParticipants
                    where part.RegistrationId == response.Id
                    select new WebEventPart()
                    {
                        Name = part.Participant,
                        JobTitle = part.JobTitle,
                        Department = part.Department,
                        Handphone = part.Handphone,
                        Email = part.Email,
                        Gender = part.Gender
                    };

            response.Participants = await q.ToListAsync();
            return response;
        }

        /**
         * @api {get} /event/payment/{gateway}/{externalId}/{paymentId}/{amount} Payment callback
         * @apiVersion 1.0.0
         * @apiName GetPaymentCallback
         * @apiGroup Event
         * @apiPermission Basic Authentication
         */
        [AllowAnonymous]
        [HttpGet("payment/{gateway}/{externalId}/{paymentId}/{amount}")]
        public async Task<ActionResult> GetPaymentCallback(int gateway, string externalId, string paymentId, long amount)
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            authHeader = authHeader.Trim();
            if (authHeader.Equals(""))
            {
                return Unauthorized();
            }

            string encodedCredentials = authHeader.Substring(6);
            var credentialBytes = Convert.FromBase64String(encodedCredentials);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
            var username = credentials[0];
            var password = credentials[1];
            if (username != "onegmlapi" || password != "O1n6e0G4M7L")
            {
                return Unauthorized();
            }

            // invoice-43
            // 0123456789
            int idx = externalId.IndexOf("-");
            if (idx <= 0) return NotFound();
            string idstr = externalId.Substring(idx + 1);

            int regId = Convert.ToInt32(idstr);

            WebEventRegistration registration = _context.WebEventRegistrations.Where(a => a.Id == regId).FirstOrDefault();
            if (registration == null) return NotFound();

            DateTime now = DateTime.Now;
            registration.Payment = gateway;
            registration.LastUpdated = now;
            _context.Entry(registration).State = EntityState.Modified;

            WebEventRegPayment payment = _context.WebEventRegPayments.Where(a => a.RegistrationId == regId).FirstOrDefault();
            if(payment == null)
            {
                payment = new WebEventRegPayment();
                payment.RegistrationId = regId;
                payment.PaymentId = paymentId;
                payment.ExternalId = externalId;
                payment.Amount = amount;
                payment.CreatedDAte = now;
                payment.LastUpdated = now;
                _context.WebEventRegPayments.Add(payment);
            }
            else
            {
                payment.PaymentId = paymentId;
                payment.ExternalId = externalId;
                payment.Amount = amount;
                payment.LastUpdated = now;
                _context.Entry(payment).State = EntityState.Modified;
            }
            await _context.SaveChangesAsync();
            return NoContent();
        }


        /**
         * @api {delete} /event/register/{registrationId}/{userId} DELETE registrasi
         * @apiVersion 1.0.0
         * @apiName DeleteRegistration
         * @apiGroup Event
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} registrationId    Id dari registrasi yang ingin dihapus
         * @apiParam {Number} userId            Id dari user yang login
         * 
         */
        [Authorize(Policy = "ApiUser")]
        [HttpDelete("register/{registrationId}/{userId}")]
        public async Task<ActionResult<WebEventRegistration>> DeleteRegistration(int registrationId, int userId)
        {
            DateTime now = DateTime.Now;

            WebEventRegistration record = _context.WebEventRegistrations.Where(a => a.Id == registrationId && !a.IsDeleted).FirstOrDefault();
            if (record == null) return NotFound();

            record.IsDeleted = true;
            record.DeletedDate = now;
            record.DeletedBy = userId;
            _context.Entry(record).State = EntityState.Modified;

            List<WebEventRegParticipant> participants = await _context.WebEventRegParticipants.Where(a => a.RegistrationId == registrationId).ToListAsync();
            foreach(WebEventRegParticipant participant in participants)
            {
                _context.WebEventRegParticipants.Remove(participant);
            }

            await _context.SaveChangesAsync();

            return record;

        }


        /**
        * @api {get} /event/categories GET list kategori
        * @apiVersion 1.0.0
        * @apiName GetEventCategories
        * @apiGroup Event
        * @apiPermission Basic Authentication
        * 
        * @apiSuccessExample Success-Response:
        * [
        *   {
        *     "id": 1,
        *     "text": "Mega Seminar"
        *   },
        *   {
        *     "id": 2,
        *     "text": "Public Workshop"
        *   },
        *   {
        *     "id": 3,
        *     "text": "Webinar"
        *   },
        *   {
        *     "id": 4,
        *     "text": "Blended Learning"
        *   }
        * ]
        */
        [AllowAnonymous]
        [HttpGet("categories")]
        public async Task<ActionResult<List<GenericInfo>>> GetEventCategories()
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            authHeader = authHeader.Trim();
            if (authHeader.Equals(""))
            {
                return Unauthorized();
            }

            string encodedCredentials = authHeader.Substring(6);
            var credentialBytes = Convert.FromBase64String(encodedCredentials);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
            var username = credentials[0];
            var password = credentials[1];
            if (username != "onegmlapi" || password != "O1n6e0G4M7L")
            {
                return Unauthorized();
            }

            var query = from category in _context.WebEventCategories
                        where !category.IsDeleted
                        select new GenericInfo()
                        {
                            Id = category.Id,
                            Text = category.Category
                        };

            return query.ToList<GenericInfo>();
        }

        /**
         * @api {get} /event/intropage/{categoryId}/{publish}/{page}/{perPage}/{search} GET list intro dengan pagination
         * @apiVersion 1.0.0
         * @apiName GetEventIntroPage
         * @apiGroup Event
         * @apiPermission Basic authentication
         * 
         * @apiParam {Number} categoryId      0 untuk semua kategori, 1: Mega Seminar, 2: Public workshop, 3: Webinar, 4: Blended learning
         * @apiParam {Number} publish         0 untuk draft, 1 untuk publish
         * @apiParam {Number} page            Halaman yang ditampilkan. 
         * @apiParam {Number} perPage         Jumlah data per halaman.  
         * @apiParam {String} search          Kata yang mau dicari di judul. * untuk semua
         * 
         * @apiSuccessExample Success-Response:
         * {
         *     "events": [
         *         {
         *             "webEvent": {
         *                 "id": 58,
         *                 "title": "Himalaya",
         *                 "intro": "Mendaki gunung himalaya",
         *                 "slug": "Himalaya",
         *                 "metaTitle": "Mendaki gunung",
         *                 "metaDescription": "Deskripsi dari mendaki gunung",
         *                 "keyword": "mendaki, gunung, tantangan",
         *                 "categoryId": 1,
         *                 "fromDate": "0001-01-01T00:00:00",
         *                 "toDate": "2020-04-12T00:00:00",
         *                 "startTime": null,
         *                 "endTime": null,
         *                 "audience": "Semua orang",
         *                 "address": "Jakarta",
         *                 "publish": false,
         *                 "addInfo": "",
         *                 "RegistrationURL": "",
         *                 "createdDate": "2020-04-10T13:55:23.1171146",
         *                 "createdBy": 0,
         *                 "lastUpdated": "2020-04-10T13:55:23.1171146",
         *                 "lastUpdatedBy": 0,
         *                 "isDeleted": false,
         *                 "deletedBy": 0,
         *                 "deletedDate": "0001-01-01T00:00:00"
         *             },
         *             "errors": [],
         *             "thumbnailURL": null
         *         }
         *     ],
         *     "info": {
         *         "page": 1,
         *         "perPage": 10,
         *         "total": 28
         *     }
         * }         
         */
        [AllowAnonymous]
        [HttpGet("intropage/{categoryId}/{publish}/{page}/{perPage}/{search}")]
        public async Task<ActionResult<WebEventIntroPageInfo>> GetEventIntroPage(int categoryId, int publish, int page, int perPage, string search)
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            authHeader = authHeader.Trim();
            if (authHeader.Equals(""))
            {
                return Unauthorized();
            }

            string encodedCredentials = authHeader.Substring(6);
            var credentialBytes = Convert.FromBase64String(encodedCredentials);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
            var username = credentials[0];
            var password = credentials[1];
            if (username != "onegmlapi" || password != "O1n6e0G4M7L")
            {
                return Unauthorized();
            }

            List<WebEventIntroResponse> list = new List<WebEventIntroResponse>();
            search = search.Trim();

            DateTime now = DateTime.Now;

            IQueryable<WebEvent> query;
            if(perPage == 5)
            {
                int topicId = _context.WebTopicCategories.Where(a => a.Category.Equals("CDHX") && !a.IsDeleted).Select(a => a.Id).FirstOrDefault();

                // called from CDHX
                if (categoryId == 0)
                {
                    if (publish == 0)
                    {
                        if (search.Equals("*"))
                        {
                            query = from a in _context.WebEvents
                                    where !a.IsDeleted && !a.Publish && a.FromDate.AddDays(7) < now && a.TopicId == topicId
                                    orderby a.FromDate descending
                                    select a;

                        }
                        else
                        {
                            query = from a in _context.WebEvents
                                    where !a.IsDeleted && !a.Publish && a.Title.Contains(search) && a.FromDate.AddDays(7) < now && a.TopicId == topicId
                                    orderby a.FromDate descending
                                    select a;

                        }
                    }
                    else
                    {
                        if (search.Equals("*"))
                        {
                            query = from a in _context.WebEvents
                                    where !a.IsDeleted && a.Publish && a.FromDate.AddDays(7) < now && a.TopicId == topicId
                                    orderby a.FromDate descending
                                    select a;

                        }
                        else
                        {
                            query = from a in _context.WebEvents
                                    where !a.IsDeleted && a.Publish && a.Title.Contains(search) && a.FromDate.AddDays(7) < now && a.TopicId == topicId
                                    orderby a.FromDate descending
                                    select a;

                        }
                    }
                }
                else
                {
                    if (publish == 0)
                    {
                        if (search.Equals("*"))
                        {
                            query = from a in _context.WebEvents
                                    where !a.IsDeleted && a.CategoryId == categoryId && !a.Publish && a.FromDate.AddDays(7) < now && a.TopicId == topicId
                                    orderby a.FromDate descending
                                    select a;

                        }
                        else
                        {
                            query = from a in _context.WebEvents
                                    where !a.IsDeleted && a.CategoryId == categoryId && !a.Publish && a.Title.Contains(search) && a.FromDate.AddDays(7) < now && a.TopicId == topicId
                                    orderby a.FromDate descending
                                    select a;

                        }
                    }
                    else
                    {
                        if (search.Equals("*"))
                        {
                            query = from a in _context.WebEvents
                                    where !a.IsDeleted && a.Publish && a.CategoryId == categoryId && a.FromDate.AddDays(7) < now && a.TopicId == topicId
                                    orderby a.FromDate descending
                                    select a;

                        }
                        else
                        {
                            query = from a in _context.WebEvents
                                    where !a.IsDeleted && a.Publish && a.CategoryId == categoryId && a.Title.Contains(search) && a.FromDate.AddDays(7) < now && a.TopicId == topicId
                                    orderby a.FromDate descending
                                    select a;

                        }
                    }
                }
            }
            else
            {
                // From CMS
                if (categoryId == 0)
                {
                    if (publish == 0)
                    {
                        if (search.Equals("*"))
                        {
                            query = from a in _context.WebEvents
                                    where !a.IsDeleted && !a.Publish
                                    orderby a.FromDate descending
                                    select a;

                        }
                        else
                        {
                            query = from a in _context.WebEvents
                                    where !a.IsDeleted && !a.Publish && a.Title.Contains(search)
                                    orderby a.FromDate descending
                                    select a;

                        }
                    }
                    else
                    {
                        if (search.Equals("*"))
                        {
                            query = from a in _context.WebEvents
                                    where !a.IsDeleted && a.Publish
                                    orderby a.FromDate descending
                                    select a;

                        }
                        else
                        {
                            query = from a in _context.WebEvents
                                    where !a.IsDeleted && a.Publish && a.Title.Contains(search)
                                    orderby a.FromDate descending
                                    select a;

                        }
                    }
                }
                else
                {
                    if (publish == 0)
                    {
                        if (search.Equals("*"))
                        {
                            query = from a in _context.WebEvents
                                    where !a.IsDeleted && a.CategoryId == categoryId && !a.Publish
                                    orderby a.FromDate descending
                                    select a;

                        }
                        else
                        {
                            query = from a in _context.WebEvents
                                    where !a.IsDeleted && a.CategoryId == categoryId && !a.Publish && a.Title.Contains(search)
                                    orderby a.FromDate descending
                                    select a;

                        }
                    }
                    else
                    {
                        if (search.Equals("*"))
                        {
                            query = from a in _context.WebEvents
                                    where !a.IsDeleted && a.Publish && a.CategoryId == categoryId
                                    orderby a.FromDate descending
                                    select a;

                        }
                        else
                        {
                            query = from a in _context.WebEvents
                                    where !a.IsDeleted && a.Publish && a.CategoryId == categoryId && a.Title.Contains(search)
                                    orderby a.FromDate descending
                                    select a;

                        }
                    }
                }
            }

            int total = query.Count();
            List<WebEvent> events = await query.Skip(perPage * (page - 1)).Take(perPage).ToListAsync<WebEvent>();

            foreach (WebEvent e in events)
            {
                WebEventIntroResponse response = new WebEventIntroResponse();
                response.webEvent = e;

                WebEventImage image = _context.WebEventImages.Where(a => a.EventId == e.Id && a.ThumbnailId == 1 && !a.IsDeleted).FirstOrDefault();
                if (image != null && image.Id > 0)
                {
                    response.ThumbnailURL = getAssetsUrl(e.Id, image.Filename, "");
                }
                list.Add(response);
            }

            return new WebEventIntroPageInfo()
            {
                Events = list,
                Info = new PaginationInfo(page, perPage, total)
            };
        }

        /**
         * @api {get} /event/intro/{categoryId}/{locationId}/{topicId}/{publishOnly} GET list intro
         * @apiVersion 1.0.0
         * @apiName GetEventIntro
         * @apiGroup Event
         * @apiPermission Basic authentication
         * 
         * @apiParam {Number} categoryId      0 untuk semua kategori, 1: Mega Seminar, 2: Public workshop, 3: Webinar, 4: Blended learning
         * @apiParam {Number} locationId      0 untuk semua lokasi, 1: Vitual, 2: Jakarta, dst
         * @apiParam {Number} topicId         0 untuk semua topik, 1: Strategy, 2: Process, dst
         * @apiParam {Number} publishOnly     0 untuk semua, 1 untuk event yang sudah dipublish saja, -1 untuk event yang sudah di-publish (termasuk yang sudah lewat)
         * 
         * @apiSuccessExample Success-Response:
         * [
         *   {
         *     "webEvent": {
         *       "id": 1,
         *       "title": "Mega Seminar Kepemimpinan",
         *       "intro": "Kepemimpinan dan program pengembangan SDM telah mengalami perubahan besar dalam era digital saat ini. Ikuti diskusinya dalam webinar ini.",
         *       "slug": "mega-seminar-kepemimpinan",
         *       "metaTitle": "Mega seminar",
         *       "metaDescription": "Deskripsi dari mega seminar",
         *       "keyword": "seminar, kepemimpinan, ",
         *       "categoryId": 1,
         *       "locationId": 1,
         *       "topicId": 1,
         *       "fromDate": "2020-04-08T00:00:00.000Z",
         *       "toDate": "2020-04-08T00:00:00.000Z",
         *       "startTime": "08:00",
         *       "endTime": "17:00",
         *       "audience": "",
         *       "address": "Hotel Mandarin",
         *       "publish": true,
         *       "addInfo": "",
         *       "RegistrationURL": "",
         *       "createdDate": "2020-04-04T06:29:17.821Z",
         *       "createdBy": 3,
         *       "lastUpdated": "2020-04-04T06:29:17.821Z",
         *       "lastUpdatedBy": 3,
         *       "isDeleted": false,
         *       "deletedBy": 0,
         *       "deletedDate": "1970-00-00T00:00:00.000Z"
         *     },
         *     "errors": [
         *       {
         *         "code": "ok",
         *         "description": ""
         *       }
         *     ],
         *     "thumbnailURL": "URL"
         *   }
         * ]
         */
        [AllowAnonymous]
        [HttpGet("intro/{slug}/{categoryId}/{locationId}/{topicId}/{publishOnly}")]
        public async Task<ActionResult<List<WebEventIntroResponse>>> GetEventIntroBySlug(string slug, int categoryId, int locationId, int topicId, int publishOnly) 
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            authHeader = authHeader.Trim();
            if (authHeader.Equals(""))
            {
                return Unauthorized();
            }

            string encodedCredentials = authHeader.Substring(6);
            var credentialBytes = Convert.FromBase64String(encodedCredentials);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
            var username = credentials[0];
            var password = credentials[1];
            if (username != "onegmlapi" || password != "O1n6e0G4M7L")
            {
                return Unauthorized();
            }

            List<WebEventIntroResponse> list = new List<WebEventIntroResponse>();

            IQueryable<WebEvent> query;
            IQueryable<WebEvent> query2;

            Func<WebEvent, bool> WherePredicate = e => {
                bool cat = categoryId == 0 ? true : e.CategoryId == categoryId;
                bool topic = topicId == 0 ? true : e.TopicId == topicId;
                bool location = locationId == 0 ? true : e.LocationId == locationId;
                bool publish = publishOnly == 0 ? true : e.Publish;
                bool slugmatch = slug.Trim().Equals("*") ? true : e.CdhxCategory.Equals(slug);
                bool all = publishOnly == -1 || e.FromDate >= DateTime.Today;

                return all && !e.IsDeleted && cat && topic && location && publish && slugmatch;
            };

            query = from a in _context.WebEvents
                    where WherePredicate(a)
                    select a;

            List<WebEvent> events = await query.ToListAsync<WebEvent>();

            foreach (WebEvent e in events)
            {
                WebEventIntroResponse response = new WebEventIntroResponse();
                response.webEvent = e;
                WebEventImage image = _context.WebEventImages.Where(a => a.EventId == e.Id && a.ThumbnailId == a.EventId && !a.IsDeleted).FirstOrDefault();
                if (image != null && image.Id > 0)
                {
                    response.ThumbnailURL = getAssetsUrl(e.Id, image.Filename, "");
                }

                var qn = from se in _context.WebEventSpeakerEvents
                         join s in _context.WebEventSpeakerRecords on se.SpeakerId equals s.Id
                         where se.EventId == e.Id && !s.IsDeleted
                         select new WebEventSpeakerInfo()
                         {
                             Id = s.Id,
                             Name = s.Name,
                             Title = s.Title,
                             Company = s.Company,
                             Profile = string.IsNullOrEmpty(s.Profile) ? "" : getAssetsUrl(e.Id, "", s.Profile),
                             ProfileFilename = s.ProfileFilename
                         };
                response.Speakers = await qn.ToListAsync();
/*
                List < WebEventSpeaker > speakers = await _context.WebEventSpeakers.Where(a => a.EventId == e.Id && !a.IsDeleted).ToListAsync();
                foreach (WebEventSpeaker speaker in speakers)
                {
                    response.Speakers.Add(new WebEventSpeakerInfo()
                    {
                        Id = speaker.Id,
                        Name = speaker.Name,
                        Title = speaker.Title,
                        Company = speaker.Company,
                        Profile = "",
                        ProfileFilename = getAssetsUrl(e.Id, speaker.Profile)
                    });
                }
*/                
                list.Add(response);
            }

            // Update 2022_11_23
            // Event yang sedang berjalan ngga dimasukkan
            /*
            Func<WebEvent, bool> WherePredicate2 = e => {
                bool cat = categoryId == 0 ? true : e.CategoryId == categoryId;
                bool topic = topicId == 0 ? true : e.TopicId == topicId;
                bool location = locationId == 0 ? true : e.LocationId == locationId;
                bool publish = publishOnly == 0 ? true : e.Publish;
                bool slugmatch = slug.Trim().Equals("*") ? true : e.CdhxCategory.Equals(slug);
                bool all = publishOnly == -1 || (e.FromDate < DateTime.Today && e.ToDate > DateTime.Now);

                return all && !e.IsDeleted && cat && topic && location && publish && slugmatch;
            };
            query2 = from a in _context.WebEvents
                     where WherePredicate2(a)
                     select a;
            List<WebEvent> events2 = await query2.ToListAsync<WebEvent>();

            foreach (WebEvent e in events2)
            {
                WebEventIntroResponse response = new WebEventIntroResponse();
                response.webEvent = e;

                WebEventImage image = _context.WebEventImages.Where(a => a.EventId == e.Id && a.ThumbnailId == a.EventId && !a.IsDeleted).FirstOrDefault();
                if (image != null && image.Id > 0)
                {
                    response.ThumbnailURL = getAssetsUrl(e.Id, image.Filename, "");
                }

                var qn = from se in _context.WebEventSpeakerEvents
                         join s in _context.WebEventSpeakerRecords on se.SpeakerId equals s.Id
                         where se.EventId == e.Id && !s.IsDeleted
                         select new WebEventSpeakerInfo()
                         {
                             Id = s.Id,
                             Name = s.Name,
                             Title = s.Title,
                             Company = s.Company,
                             Profile = string.IsNullOrEmpty(s.Profile) ? "" : getAssetsUrl(e.Id, "", s.Profile),
                             ProfileFilename = s.ProfileFilename
                         };
                response.Speakers = await qn.ToListAsync();
            
                list.Add(response);
            }
            */
            return list;
        }

        // Update Speakers
        [Authorize(Policy = "ApiUser")]
        [HttpGet("fixspeaker")]
        public async Task<ActionResult> FixEventSpeaker()
        {
            List<WebEventSpeaker> speakers = await _context.WebEventSpeakers.ToListAsync();
            foreach(WebEventSpeaker speaker in speakers)
            {
                WebEventSpeakerRecord record = _context.WebEventSpeakerRecords.Where(a => a.Name.Contains(speaker.Name.Trim())).FirstOrDefault();
                if(record == null)
                {
                    WebEventSpeakerRecord newrecord = new WebEventSpeakerRecord()
                    {
                        Name = speaker.Name.Trim(),
                        Title = speaker.Title.Trim(),
                        Company = speaker.Company.Trim(),
                        Profile = speaker.Profile,
                        ProfileFilename = speaker.ProfileFilename,
                        CreatedDate = speaker.CreatedDate,
                        CreatedBy = speaker.CreatedBy,
                        LastUpdated = speaker.LastUpdated,
                        LastUpdatedBy = speaker.LastUpdatedBy,
                        IsDeleted = speaker.IsDeleted,
                        DeletedBy = speaker.DeletedBy,
                        DeletedDate = speaker.DeletedDate
                    };
                    _context.WebEventSpeakerRecords.Add(newrecord);
                    await _context.SaveChangesAsync();

                    await AddEventSpeaker(speaker.EventId, newrecord.Id);
                }
                else
                {
                    await AddEventSpeaker(speaker.EventId, record.Id);
                }
            }

            return NoContent();
        }

        /**
         * @api {get} /event/list/speaker/{search} GET list speaker
         * @apiVersion 1.0.0
         * @apiName WebEventSpeakers
         * @apiGroup Event
         * @apiPermission Basic Authentication
         * 
         * @apiParam {String} search          Nama speaker yang mau dicari. * untuk semua
         * 
         * @apiSuccessExample Success-Response:
         * [
         *     {
         *         "id": 22,
         *         "name": "Bayu Setiadji,",
         *         "title": "Chief of Learning Development Solutions",
         *         "company": "",
         *         "profileFilename": "",
         *         "profile": ""
         *     },
         *     {
         *         "id": 23,
         *         "name": "Mikael M. Murtaba",
         *         "title": "Senior Consultant of GML L&D Tribe",
         *         "company": "",
         *         "profileFilename": "",
         *         "profile": ""
         *     }
         * ]
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("list/speaker/{search}")]
        public async Task<ActionResult<List<WebEventSpeakerInfo>>> WebEventSpeakers(String search)
        {
            Func<WebEventSpeakerRecord, bool> WherePredicateSpeaker = e => {
                return search.Trim().Equals("*") || e.Name.Contains(search);
            };

            var qn = from s in _context.WebEventSpeakerRecords 
                     where WherePredicateSpeaker(s) && !s.IsDeleted
                     select new WebEventSpeakerInfo()
                     {
                         Id = s.Id,
                         Name = s.Name,
                         Title = s.Title,
                         Company = s.Company,
                         Profile = string.IsNullOrEmpty(s.Profile) ? "" : getAssetsUrl(0, "", s.Profile),
                         ProfileFilename = s.ProfileFilename
                     };
            return await qn.ToListAsync();
        }

        /**
         * @api {get} /event/speaker/{speakerId} GET detail speaker
         * @apiVersion 1.0.0
         * @apiName WebEventSpeaker
         * @apiGroup Event
         * @apiPermission Basic Authentication
         * 
         * @apiSuccessExample Success-Response:
         * {
         *     "id": 23,
         *     "name": "Mikael M. Murtaba",
         *     "title": "Senior Consultant of GML L&D Tribe",
         *     "company": "",
         *     "profileFilename": "",
         *     "profile": ""
         * }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("speaker/{speakerId}")]
        public async Task<ActionResult<WebEventSpeakerInfo>> WebEventSpeaker(int speakerId)
        {
            WebEventSpeakerRecord record = _context.WebEventSpeakerRecords.Where(a => a.Id == speakerId && !a.IsDeleted).FirstOrDefault();
            if (record == null) return NotFound();

            return new WebEventSpeakerInfo()
            {
                Id = record.Id,
                Name = record.Name,
                Title = record.Title,
                Company = record.Company,
                Profile = getAssetsUrl(0, "", record.Profile), 
                ProfileFilename = record.ProfileFilename
            };
        }

        /**
         * @api {post} /event/speaker         POST speaker
         * @apiVersion 1.0.0
         * @apiName PostEventSpeaker
         * @apiGroup Event
         * @apiPermission ApiUser
         * 
         * @apiParamExample {json} Request-Example:
         * {
         *   "id": 0,
         *   "name": "Anne Frank",
         *   "title": "Dutch",
         *   "company": "Dutch",
         *   "profile": "base64 untuk foto speaker",
         *   "profileFilename": "Fotopembicara.jpg",
         *   "userId": 23
         * }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost("speaker")]
        public async Task<ActionResult<WebEventSpeakerRecord>> PostEventSpeaker(WebEventSpeakerSimple request)
        {
            DateTime now = DateTime.Now;
            WebEventSpeakerRecord record = await AddSpeaker(request.Name, request.Company, request.Title, "", "", now, request.UserId);

            if (!String.IsNullOrEmpty(request.Profile))
            {
                var error = SaveImage(request.Profile, 0, request.ProfileFilename, true, true);
                if (error.Code.Equals("ok"))
                {
                    string[] names = error.Description.Split(separator);
                    if (names.Length >= 3)
                    {
                        record.Profile = names[1]; // Random file name
                        record.ProfileFilename = names[0]; // original file name
                        _context.Entry(record).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                    }
                }
            }

            return record;
        }

        /**
         * @api {put} /event/speaker/{speakerId}         PUT speaker
         * @apiVersion 1.0.0
         * @apiName PutEventSpeaker
         * @apiGroup Event
         * @apiPermission ApiUser
         * 
         * @apiParamExample {json} Request-Example:
         * {
         *   "id": 23,
         *   "name": "Anne Frank",
         *   "title": "Dutch",
         *   "company": "Dutch",
         *   "profile": "base64 untuk foto speaker",
         *   "profileFilename": "Fotopembicara.jpg",
         *   "userId": 23
         * }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPut("speaker/{speakerId}")]
        public async Task<ActionResult<WebEventSpeakerRecord>> PutEventSpeaker(int speakerId, WebEventSpeakerSimple request)
        {
            if (speakerId != request.Id) return BadRequest();

            DateTime now = DateTime.Now;

            WebEventSpeakerRecord record = _context.WebEventSpeakerRecords.Where(a => a.Id == speakerId && !a.IsDeleted).FirstOrDefault();
            if (record == null) return NotFound();

            record.Name = request.Name;
            record.Company = request.Company;
            record.Title = request.Title;
            record.LastUpdated = now;
            record.LastUpdatedBy = request.UserId;

            if (!String.IsNullOrEmpty(request.Profile))
            {
                var error = SaveImage(request.Profile, 0, request.ProfileFilename, true, true);
                if (error.Code.Equals("ok"))
                {
                    string[] names = error.Description.Split(separator);
                    if (names.Length >= 3)
                    {
                        record.Profile = names[1]; // Random file name
                        record.ProfileFilename = names[0]; // original file name
                    }
                }
            }

            _context.Entry(record).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return record;
        }

        /**
         * @api {delete} /event/speaker/{speakerId}/{userId} DELETE speaker
         * @apiVersion 1.0.0
         * @apiName DeleteSpeaker
         * @apiGroup Event
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} speakerId   Id dari speaker yang ingin dihapus
         * @apiParam {Number} userId      Id dari user yang login
         * 
         */

        [Authorize(Policy = "ApiUser")]
        [HttpDelete("speaker/{speakerId}/{userId}")]
        public async Task<ActionResult<WebEventSpeakerRecord>> DeleteSpeaker(int speakerId, int userId)
        {
            DateTime now = DateTime.Now;

            WebEventSpeakerRecord record = _context.WebEventSpeakerRecords.Where(a => a.Id == speakerId && !a.IsDeleted).FirstOrDefault();
            if (record == null) return NotFound();

            record.IsDeleted = true;
            record.DeletedDate = now;
            record.DeletedBy = userId;
            _context.Entry(record).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return record;
        }


        // END EXPERTS

        private async Task<WebEventSpeakerRecord> AddSpeaker(String name, String company, String title, String profile, String profileFilename, DateTime now, int userId)
        {
            WebEventSpeakerRecord record = _context.WebEventSpeakerRecords.Where(a => a.Name.Equals(name.Trim()) && a.Company.Equals(company.Trim()) && a.Title.Equals(title.Trim()) && !a.IsDeleted).FirstOrDefault();
            if (record == null)
            {
                WebEventSpeakerRecord newrecord = new WebEventSpeakerRecord()
                {
                    Name = name.Trim(),
                    Title = title.Trim(),
                    Company = company.Trim(),
                    Profile = profile,
                    ProfileFilename = profileFilename,
                    CreatedDate = now,
                    CreatedBy = userId,
                    LastUpdated = now,
                    LastUpdatedBy = userId,
                    IsDeleted = false,
                    DeletedBy = 0
                };
                _context.WebEventSpeakerRecords.Add(newrecord);
                await _context.SaveChangesAsync();

                return newrecord;
            }

            return record;
        }
        private async Task<int> AddEventSpeaker(int eventId, int speakerId)
        {
            try
            {
                WebEventSpeakerEvent e = _context.WebEventSpeakerEvents.Where(a => a.EventId == eventId && a.SpeakerId == speakerId).FirstOrDefault();
                if (e == null)
                {
                    WebEventSpeakerEvent speakerEvent = new WebEventSpeakerEvent()
                    {
                        EventId = eventId,
                        SpeakerId = speakerId
                    };
                    _context.WebEventSpeakerEvents.Add(speakerEvent);
                    await _context.SaveChangesAsync();
                }

            }
            catch
            {
                return 0;
            }

            return 1;

        }

        /**
         * @api {get} /event/tribe/{tribeId}/{publishOnly} GET list event by tribe
         * @apiVersion 1.0.0
         * @apiName GetEventIntroByTribe
         * @apiGroup Event
         * @apiPermission Basic authentication
         * 
         * @apiParam {Number} tribeId         0 untuk semua tribe, atau id dari tribe
         * @apiParam {Number} publishOnly     0 untuk semua, 1 untuk event yang sudah dipublish saja, -1 untuk event yang sudah di-publish (termasuk yang sudah lewat)
         * 
         * @apiSuccessExample Success-Response:
         * [
         *   {
         *     "webEvent": {
         *       "id": 1,
         *       "title": "Mega Seminar Kepemimpinan",
         *       "intro": "Kepemimpinan dan program pengembangan SDM telah mengalami perubahan besar dalam era digital saat ini. Ikuti diskusinya dalam webinar ini.",
         *       "slug": "mega-seminar-kepemimpinan",
         *       "metaTitle": "Mega seminar",
         *       "metaDescription": "Deskripsi dari mega seminar",
         *       "keyword": "seminar, kepemimpinan, ",
         *       "categoryId": 1,
         *       "locationId": 1,
         *       "topicId": 1,
         *       "fromDate": "2020-04-08T00:00:00.000Z",
         *       "toDate": "2020-04-08T00:00:00.000Z",
         *       "startTime": "08:00",
         *       "endTime": "17:00",
         *       "audience": "",
         *       "address": "Hotel Mandarin",
         *       "publish": true,
         *       "addInfo": "",
         *       "RegistrationURL": "",
         *       "createdDate": "2020-04-04T06:29:17.821Z",
         *       "createdBy": 3,
         *       "lastUpdated": "2020-04-04T06:29:17.821Z",
         *       "lastUpdatedBy": 3,
         *       "isDeleted": false,
         *       "deletedBy": 0,
         *       "deletedDate": "1970-00-00T00:00:00.000Z"
         *     },
         *     "errors": [
         *       {
         *         "code": "ok",
         *         "description": ""
         *       }
         *     ],
         *     "thumbnailURL": "URL"
         *   }
         * ]
         */
        [AllowAnonymous]
        [HttpGet("tribe/{tribeId}/{publishOnly}")]
        public async Task<ActionResult<List<WebEventIntroResponse>>> GetEventIntroByTribe(int tribeId, int locationId, int topicId, int publishOnly)
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            authHeader = authHeader.Trim();
            if (authHeader.Equals(""))
            {
                return Unauthorized();
            }

            string encodedCredentials = authHeader.Substring(6);
            var credentialBytes = Convert.FromBase64String(encodedCredentials);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
            var username = credentials[0];
            var password = credentials[1];
            if (username != "onegmlapi" || password != "O1n6e0G4M7L")
            {
                return Unauthorized();
            }

            List<WebEventIntroResponse> list = new List<WebEventIntroResponse>();

            IQueryable<WebEvent> query;
            IQueryable<WebEvent> query2;

            Func<WebEvent, bool> WherePredicate = e => {
                bool tribe = tribeId == 0 || e.TribeId == tribeId;
                bool all = publishOnly == -1 || e.FromDate >= DateTime.Today;

                return tribe && all && !e.IsDeleted;
            };
            query = from a in _context.WebEvents
                    where WherePredicate(a)
                    select a;

            List<WebEvent> events = await query.ToListAsync<WebEvent>();

            foreach (WebEvent e in events)
            {
                WebEventIntroResponse response = new WebEventIntroResponse();
                response.webEvent = e;
                WebEventImage image = _context.WebEventImages.Where(a => a.EventId == e.Id && a.ThumbnailId == a.EventId && !a.IsDeleted).FirstOrDefault();
                if (image != null && image.Id > 0)
                {
                    response.ThumbnailURL = getAssetsUrl(e.Id, image.Filename, "");
                }

                var qn = from se in _context.WebEventSpeakerEvents
                         join s in _context.WebEventSpeakerRecords on se.SpeakerId equals s.Id
                         where se.EventId == e.Id && !s.IsDeleted
                         select new WebEventSpeakerInfo()
                         {
                             Id = s.Id,
                             Name = s.Name,
                             Title = s.Title,
                             Company = s.Company,
                             Profile = string.IsNullOrEmpty(s.Profile) ? "" : getAssetsUrl(e.Id, "", s.Profile),
                             ProfileFilename = s.ProfileFilename
                         };
                response.Speakers = await qn.ToListAsync();
                /*
                                List<WebEventSpeaker> speakers = await _context.WebEventSpeakers.Where(a => a.EventId == e.Id && !a.IsDeleted).ToListAsync();
                                foreach (WebEventSpeaker speaker in speakers)
                                {
                                    response.Speakers.Add(new WebEventSpeakerInfo()
                                    {
                                        Id = speaker.Id,
                                        Name = speaker.Name,
                                        Title = speaker.Title,
                                        Company = speaker.Company,
                                        Profile = "",
                                        ProfileFilename = getAssetsUrl(e.Id, speaker.Profile)
                                    });
                                }
                */
                list.Add(response);
            }

            Func<WebEvent, bool> WherePredicate2 = e => {
                bool tribe = tribeId == 0 || e.TribeId == tribeId;
                bool all = publishOnly == -1 || (e.FromDate < DateTime.Today && e.ToDate > DateTime.Now);

                return all && tribe && !e.IsDeleted;
            };
            query2 = from a in _context.WebEvents
                     where WherePredicate2(a)
                     select a;
            List<WebEvent> events2 = await query2.ToListAsync<WebEvent>();

            foreach (WebEvent e in events2)
            {
                WebEventIntroResponse response = new WebEventIntroResponse();
                response.webEvent = e;

                WebEventImage image = _context.WebEventImages.Where(a => a.EventId == e.Id && a.ThumbnailId == a.EventId && !a.IsDeleted).FirstOrDefault();
                if (image != null && image.Id > 0)
                {
                    response.ThumbnailURL = getAssetsUrl(e.Id, image.Filename, "");
                }

                var qn = from se in _context.WebEventSpeakerEvents
                         join s in _context.WebEventSpeakerRecords on se.SpeakerId equals s.Id
                         where se.EventId == e.Id && !s.IsDeleted
                         select new WebEventSpeakerInfo()
                         {
                             Id = s.Id,
                             Name = s.Name,
                             Title = s.Title,
                             Company = s.Company,
                             Profile = string.IsNullOrEmpty(s.Profile) ? "" : getAssetsUrl(e.Id, "", s.Profile),
                             ProfileFilename = s.ProfileFilename
                         };
                response.Speakers = await qn.ToListAsync();

                /*
                                List<WebEventSpeaker> speakers = await _context.WebEventSpeakers.Where(a => a.EventId == e.Id && !a.IsDeleted).ToListAsync();
                                foreach(WebEventSpeaker speaker in speakers)
                                {
                                    response.Speakers.Add(new WebEventSpeakerInfo()
                                    {
                                        Id = speaker.Id,
                                        Name = speaker.Name,
                                        Title = speaker.Title,
                                        Company = speaker.Company,
                                        Profile = "",
                                        ProfileFilename = getAssetsUrl(e.Id, speaker.Profile)
                                    });
                                }
                */
                list.Add(response);
            }

            return list;

        }

        /**
         * @api {get} /event/intro/{categoryId}/{locationId}/{topicId}/{publishOnly} GET list intro
         * @apiVersion 1.0.0
         * @apiName GetEventIntro
         * @apiGroup Event
         * @apiPermission Basic authentication
         * 
         * @apiParam {Number} categoryId      0 untuk semua kategori, 1: Mega Seminar, 2: Public workshop, 3: Webinar, 4: Blended learning
         * @apiParam {Number} locationId      0 untuk semua lokasi, 1: Vitual, 2: Jakarta, dst
         * @apiParam {Number} topicId         0 untuk semua topik, 1: Strategy, 2: Process, dst
         * @apiParam {Number} publishOnly     0 untuk semua, 1 untuk event yang sudah dipublish saja, -1 untuk event yang sudah di-publish (termasuk yang sudah lewat)
         * 
         * @apiSuccessExample Success-Response:
         * [
         *   {
         *     "webEvent": {
         *       "id": 1,
         *       "title": "Mega Seminar Kepemimpinan",
         *       "intro": "Kepemimpinan dan program pengembangan SDM telah mengalami perubahan besar dalam era digital saat ini. Ikuti diskusinya dalam webinar ini.",
         *       "slug": "mega-seminar-kepemimpinan",
         *       "metaTitle": "Mega seminar",
         *       "metaDescription": "Deskripsi dari mega seminar",
         *       "keyword": "seminar, kepemimpinan, ",
         *       "categoryId": 1,
         *       "locationId": 1,
         *       "topicId": 1,
         *       "fromDate": "2020-04-08T00:00:00.000Z",
         *       "toDate": "2020-04-08T00:00:00.000Z",
         *       "startTime": "08:00",
         *       "endTime": "17:00",
         *       "audience": "",
         *       "address": "Hotel Mandarin",
         *       "publish": true,
         *       "addInfo": "",
         *       "RegistrationURL": "",
         *       "createdDate": "2020-04-04T06:29:17.821Z",
         *       "createdBy": 3,
         *       "lastUpdated": "2020-04-04T06:29:17.821Z",
         *       "lastUpdatedBy": 3,
         *       "isDeleted": false,
         *       "deletedBy": 0,
         *       "deletedDate": "1970-00-00T00:00:00.000Z"
         *     },
         *     "errors": [
         *       {
         *         "code": "ok",
         *         "description": ""
         *       }
         *     ],
         *     "thumbnailURL": "URL"
         *   }
         * ]
         */
        [AllowAnonymous]
        [HttpGet("intro/{categoryId}/{locationId}/{topicId}/{publishOnly}")]
        public async Task<ActionResult<List<WebEventIntroResponse>>> GetEventIntro(int categoryId, int locationId, int topicId, int publishOnly)
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            authHeader = authHeader.Trim();
            if (authHeader.Equals(""))
            {
                return Unauthorized();
            }

            string encodedCredentials = authHeader.Substring(6);
            var credentialBytes = Convert.FromBase64String(encodedCredentials);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
            var username = credentials[0];
            var password = credentials[1];
            if (username != "onegmlapi" || password != "O1n6e0G4M7L")
            {
                return Unauthorized();
            }

            List<WebEventIntroResponse> list = new List<WebEventIntroResponse>();

            IQueryable<WebEvent> query;
            IQueryable<WebEvent> query2;

            Func<WebEvent, bool> WherePredicate = e => {
                bool cat = categoryId == 0 ? true : e.CategoryId == categoryId;
                bool topic = topicId == 0 ? true : e.TopicId == topicId;
                bool location = locationId == 0 ? true : e.LocationId == locationId;
                bool publish = publishOnly == 0 ? true : e.Publish;
                bool all = publishOnly == -1 || e.FromDate >= DateTime.Today;

                return all && !e.IsDeleted && cat && topic && location && publish;
            };
            query = from a in _context.WebEvents
                    where WherePredicate(a)
                    select a;

            List<WebEvent> events = await query.ToListAsync<WebEvent>();

            foreach (WebEvent e in events)
            {
                WebEventIntroResponse response = new WebEventIntroResponse();
                response.webEvent = e;
                WebEventImage image = _context.WebEventImages.Where(a => a.EventId == e.Id && a.ThumbnailId == a.EventId && !a.IsDeleted).FirstOrDefault();
                if (image != null && image.Id > 0)
                {
                    response.ThumbnailURL = getAssetsUrl(e.Id, image.Filename, "");
                }

                var qn = from se in _context.WebEventSpeakerEvents
                         join s in _context.WebEventSpeakerRecords on se.SpeakerId equals s.Id
                         where se.EventId == e.Id && !s.IsDeleted
                         select new WebEventSpeakerInfo()
                         {
                             Id = s.Id,
                             Name = s.Name,
                             Title = s.Title,
                             Company = s.Company,
                             Profile = string.IsNullOrEmpty(s.Profile) ? "" : getAssetsUrl(e.Id, "", s.Profile),
                             ProfileFilename = s.ProfileFilename
                         };
                response.Speakers = await qn.ToListAsync();

/*
                List<WebEventSpeaker> speakers = await _context.WebEventSpeakers.Where(a => a.EventId == e.Id && !a.IsDeleted).ToListAsync();
                foreach (WebEventSpeaker speaker in speakers)
                {
                    response.Speakers.Add(new WebEventSpeakerInfo()
                    {
                        Id = speaker.Id,
                        Name = speaker.Name,
                        Title = speaker.Title,
                        Company = speaker.Company,
                        Profile = "",
                        ProfileFilename = getAssetsUrl(e.Id, speaker.Profile)
                    });
                }
*/                
                list.Add(response);
            }

            // Update 2022_23_11
            // Event yang sedang berjalan tidak dimasukkan
            /*
            Func<WebEvent, bool> WherePredicate2 = e => {
                bool cat = categoryId == 0 ? true : e.CategoryId == categoryId;
                bool topic = topicId == 0 ? true : e.TopicId == topicId;
                bool location = locationId == 0 ? true : e.LocationId == locationId;
                bool publish = publishOnly == 0 ? true : e.Publish;
                bool all = publishOnly == -1 || (e.FromDate < DateTime.Today && e.ToDate > DateTime.Now);

                return all && !e.IsDeleted && cat && topic && location && publish;
            };
            query2 = from a in _context.WebEvents
                     where WherePredicate2(a)
                     select a;
            List<WebEvent> events2 = await query2.ToListAsync<WebEvent>();

            foreach (WebEvent e in events2)
            {
                WebEventIntroResponse response = new WebEventIntroResponse();
                response.webEvent = e;

                WebEventImage image = _context.WebEventImages.Where(a => a.EventId == e.Id && a.ThumbnailId == a.EventId && !a.IsDeleted).FirstOrDefault();
                if (image != null && image.Id > 0)
                {
                    response.ThumbnailURL = getAssetsUrl(e.Id, image.Filename, "");
                }

                var qn = from se in _context.WebEventSpeakerEvents
                         join s in _context.WebEventSpeakerRecords on se.SpeakerId equals s.Id
                         where se.EventId == e.Id && !s.IsDeleted
                         select new WebEventSpeakerInfo()
                         {
                             Id = s.Id,
                             Name = s.Name,
                             Title = s.Title,
                             Company = s.Company,
                             Profile = string.IsNullOrEmpty(s.Profile) ? "" : getAssetsUrl(e.Id, "", s.Profile),
                             ProfileFilename = s.ProfileFilename
                         };
                response.Speakers = await qn.ToListAsync();

                list.Add(response);
            }
            */
            return list;
        }

        /**
         * @api {get} /event/intro/{categoryId}/{publishOnly} GET list intro category
         * @apiVersion 1.0.0
         * @apiName GetEventIntroCategory
         * @apiGroup Event
         * @apiPermission Basic authentication
         * 
         * @apiParam {Number} categoryId      0 untuk semua kategori, 1: Mega Seminar, 2: Public workshop, 3: Webinar, 4: Blended learning
         * @apiParam {Number} publishOnly     0 untuk semua, 1 untuk event yang sudah dipublish saja
         * 
         * @apiSuccessExample Success-Response:
         * [
         *   {
         *     "webEvent": {
         *       "id": 1,
         *       "title": "Mega Seminar Kepemimpinan",
         *       "intro": "Kepemimpinan dan program pengembangan SDM telah mengalami perubahan besar dalam era digital saat ini. Ikuti diskusinya dalam webinar ini.",
         *       "slug": "mega-seminar-kepemimpinan",
         *       "metaTitle": "Mega seminar",
         *       "metaDescription": "Deskripsi dari mega seminar",
         *       "keyword": "seminar, kepemimpinan, ",
         *       "categoryId": 1,
         *       "locationId": 1,
         *       "topicId": 1,
         *       "fromDate": "2020-04-08T00:00:00.000Z",
         *       "toDate": "2020-04-08T00:00:00.000Z",
         *       "startTime": "08:00",
         *       "endTime": "17:00",
         *       "audience": "",
         *       "address": "Hotel Mandarin",
         *       "publish": true,
         *       "addInfo": "",
         *       "RegistrationURL": "",
         *       "createdDate": "2020-04-04T06:29:17.821Z",
         *       "createdBy": 3,
         *       "lastUpdated": "2020-04-04T06:29:17.821Z",
         *       "lastUpdatedBy": 3,
         *       "isDeleted": false,
         *       "deletedBy": 0,
         *       "deletedDate": "1970-00-00T00:00:00.000Z"
         *     },
         *     "errors": [
         *       {
         *         "code": "ok",
         *         "description": ""
         *       }
         *     ],
         *     "thumbnailURL": "URL"
         *   }
         * ]
         */
        [AllowAnonymous]
        [HttpGet("intro/{categoryId}/{publishOnly}")]
        public async Task<ActionResult<List<WebEventIntroResponse>>> GetEventIntroCategory(int categoryId, int publishOnly)
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            authHeader = authHeader.Trim();
            if (authHeader.Equals(""))
            {
                return Unauthorized();
            }

            string encodedCredentials = authHeader.Substring(6);
            var credentialBytes = Convert.FromBase64String(encodedCredentials);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
            var username = credentials[0];
            var password = credentials[1];
            if (username != "onegmlapi" || password != "O1n6e0G4M7L")
            {
                return Unauthorized();
            }

            int topicId = 0;
            int locationId = 0;

            List<WebEventIntroResponse> list = new List<WebEventIntroResponse>();

            IQueryable<WebEvent> query;
            IQueryable<WebEvent> query2;

            Func<WebEvent, bool> WherePredicate = e => {
                bool cat = categoryId == 0 ? true : e.CategoryId == categoryId;
                bool topic = topicId == 0 ? true : e.TopicId == topicId;
                bool location = locationId == 0 ? true : e.LocationId == locationId;
                bool publish = publishOnly == 0 ? true : e.Publish;

                return e.FromDate >= DateTime.Today && !e.IsDeleted && cat && topic && location && publish;
            };
            query = from a in _context.WebEvents
                    where WherePredicate(a)
                    select a;

            List<WebEvent> events = await query.ToListAsync<WebEvent>();

            foreach (WebEvent e in events)
            {
                WebEventIntroResponse response = new WebEventIntroResponse();
                response.webEvent = e;
                WebEventImage image = _context.WebEventImages.Where(a => a.EventId == e.Id && a.ThumbnailId == a.EventId && !a.IsDeleted).FirstOrDefault();
                if (image != null && image.Id > 0 && !e.Slug.Equals(""))
                {
                    response.ThumbnailURL = getAssetsUrl(e.Id, image.Filename, "");
                    list.Add(response);
                }
            }

            // Update 2022_11_23
            // Event yang sedang berjalan tidak dimasukkan
            /*
            Func<WebEvent, bool> WherePredicate2 = e => {
                bool cat = categoryId == 0 ? true : e.CategoryId == categoryId;
                bool topic = topicId == 0 ? true : e.TopicId == topicId;
                bool location = locationId == 0 ? true : e.LocationId == locationId;
                bool publish = publishOnly == 0 ? true : e.Publish;

                return e.FromDate < DateTime.Today && e.ToDate > DateTime.Now && !e.IsDeleted && cat && topic && location && publish;
            };
            query2 = from a in _context.WebEvents
                     where WherePredicate2(a)
                     select a;
            List<WebEvent> events2 = await query2.ToListAsync<WebEvent>();

            foreach (WebEvent e in events2)
            {
                WebEventIntroResponse response = new WebEventIntroResponse();
                response.webEvent = e;

                WebEventImage image = _context.WebEventImages.Where(a => a.EventId == e.Id && a.ThumbnailId == a.EventId && !a.IsDeleted).FirstOrDefault();
                if (image != null && image.Id > 0 && !e.Slug.Equals(""))
                {
                    response.ThumbnailURL = getAssetsUrl(e.Id, image.Filename, "");
                    list.Add(response);
                }
            }
            */

            return list;
        }


        

        /**
         * @api {get} /event/list GET list event
         * @apiVersion 1.0.0
         * @apiName GetEventList
         * @apiGroup Event
         * @apiPermission Basic authentication
         * 
         * @apiSuccessExample Success-Response:
         * [
         *     {
         *         "id": 9,
         *         "text": "Corporate University"
         *     },
         *     {
         *         "id": 4,
         *         "text": "Five Practice of Execution Winners"
         *     },
         * ]
         */
        [AllowAnonymous]
        [HttpGet("list")]
        public async Task<ActionResult<List<GenericInfo>>> GetEventList()
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            authHeader = authHeader.Trim();
            if (authHeader.Equals(""))
            {
                return Unauthorized();
            }

            string encodedCredentials = authHeader.Substring(6);
            var credentialBytes = Convert.FromBase64String(encodedCredentials);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
            var username = credentials[0];
            var password = credentials[1];
            if (username != "onegmlapi" || password != "O1n6e0G4M7L")
            {
                return Unauthorized();
            }

            var query = from ev in _context.WebEvents
                        where !ev.IsDeleted
                        orderby ev.Title
                        select new GenericInfo()
                        {
                            Id = ev.Id,
                            Text = ev.Title
                        };

            return await query.ToListAsync<GenericInfo>();
        }

        /**
         * @api {post} /event POST event
         * @apiVersion 1.0.0
         * @apiName PostWebEvent
         * @apiGroup Event
         * @apiPermission ApiUser
         * @apiDescription Menambahkan event. DescriptionId: 1. Mega seminar, 2. Public workshop, 3. Webinar, 4. Blended learning. Perhatikan descriptionPhotos, thumbnails, brochures, frameworkImages, testimonyPhotos bukannya string, seperti contoh di bawah ini, tetapi files yang di-upload. 
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *       "id": 0,
         *       "userId": 3,
         *       "title": "Judul Event",
         *       "Intro": "Webinar ini mendiskusikan tentang..."
         *       "slug": "judul-event",
         *       "metaTitle": "Judul event",
         *       "metaDescription": "Deskripsi dari event",
         *       "keyword": "webinar, menarik",
         *       "fromDate": "2020-04-03T09.00",
         *       "toDate": "2020-05-03T17.00",
         *       "startTime": "15:00",
         *       "endTime": "16.30",
         *       "address": "Hotel Mandarin",
         *       "categoryId": 1,
         *       "locationId": 1,
         *       "topicId": 1,
         *       "registrationURL": "",
         *       "publish": 0,
         *       "audience": "",
         *       "description": "Deskripsi event ini dari CKEditor",
         *       "thumbnailFilename": "NamaFileThumbnail.jpg"
         *       "thumbnails": "base64 untuk thumbnail",
         *       "brochuleFilename": "NamaFileBrosur.jpg",
         *       "brochures": "base64 untuk brosur",
         *       "flyerFilename": "NamaFileFlyer.jpg",
         *       "flyer": "base64 untuk fluer",         
         *       "cdhxCategory": "hr-cademy",         
         *       "videoURL": "https://www.yourube.com/abcdefgh",         
         *       "agenda": [
         *           {
         *               "id": 0,
         *               "date": "2020-04-03",
         *               "startTime": "09:00",
         *               "endTime": "17:00",
         *               "description": "Agenda 1...."
         *           },
         *           {
         *               "id": 0,
         *               "date": "2020-05-03",
         *               "startTime": "09:00",
         *               "endTime": "17:00",
         *               "description": "Agenda 2...."
         *           }
         *       ],
         *       "testimonies": [
         *           {
         *               "id": 0,
         *               "name": "Tinus Garnida",
         *               "title": "HR Head",
         *               "company": "GML Performance Consulting",
         *               "testimony": "Workshop ini sangat bermanfaat.",
         *               "photoFilename": "FilePhoto.jpg",
         *               "testimonyPhotos": "base64 untuk foto"
         *           }
         *       ],
         *       "speakers": [
         *           {
         *               "id": 0,
         *               "name": "Bayu Setiaji",
         *               "title": "Tribe Chief",
         *               "company": "GML Performance Consulting".
         *               "profile": "base64 untuk foto speaker",
         *               "profileFilename": "Fotopembicara.jpg",
         *           }
         *       ],
         *       "investments": [
         *           {
         *               "id": 0,
         *               "title": "",
         *               "type": "Normal",
         *               "nominal": 6000000,
         *               "ppn": 1,
         *               "ppnpercent": 10,
         *               "paymenturl": "https://app.sandbox.midtrans.com/payment-links/1588732923215"
         *           },
         *           {
         *               "id": 0,
         *               "title": "",
         *               "type": "Early Bird",
         *               "nominal": 5000000,
         *               "ppn": 1,
         *               "ppnpercent": 10,
         *               "paymenturl": "https://app.sandbox.midtrans.com/payment-links/1588676502361"
         *           }
         *       ],
         *       "takeaways": [
         *           "Akses selamanya", "Sertifikat dari badan resmi"
         *       ],
         *       "emailNotification": true,
         *       "emailSubject": "Konfirmasi Registrasi",
         *       "email": "Terima kasih atas ... "
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   {
         *       "event": {
         *           "id": 1,
         *           "title": "Judul Event",
         *           "intro": "Webinar ini mendiskusikan tentang...",
         *           "slug": "judul-event",
         *           "metaTitle": "Judul event",
         *           "metaDescription": "Deskripsi dari event",
         *           "keyword": "webinar, menarik",
         *           "categoryId": 1,
         *           "locationId": 1,
         *           "topicId": 1,
         *           "registrationURL": "",
         *           "fromDate": "2020-04-08T00:00:00.000Z",
         *           "toDate": "2020-04-08T00:00:00.0003Z",
         *           "startTime": "08:00",
         *           "endTime": "17:00",
         *           "audience": "HR Director",
         *           "address": "Hotel Mandarin",
         *           "publish": true,
         *           "addInfo": "",
         *           "createdDate": "2020-04-06T05:52:18.303Z",
         *           "createdBy": 3,
         *           "lastUpdated": "2020-04-06T05:52:18.303Z",
         *           "lastUpdatedBy": 3,
         *           "isDeleted": false,
         *           "deletedBy": 0,
         *           "deletedDate": "1970-00-00T00:00:00.000Z"
         *       },
         *       "category": {
         *           "id": 1,
         *           "text": "Mega Seminar"
         *       },
         *       "description": "Deskripsi event ini dari CKEditor",
         *       "thumbnails": {
         *           "id": 0,
         *           "url": "https://....",
         *           "name": "ThumbnailMegaSeminar.pdf"
         *       },
         *       "brochures": {
         *           "id": 0,
         *           "url": "https://....",
         *           "name": "BrosurMegaSeminar.pdf"
         *       },
         *       "agenda": [
         *           {
         *               "id": 0,
         *               "date": "2020-04-08T00:00:00.000Z",
         *               "startTime": "08:00",
         *               "endTime": "17:00",
         *               "description": "Agenda seminar"
         *           }
         *       ],
         *       "testimonies": [
         *           {
         *               "id": 1,
         *               "name": "Erwin",
         *               "title": "Bagus",
         *               "company": "HaloDoc",
         *               "testimony": "Seminar ini bagus sekali",
         *               "photo": {
         *                   "id": 3,
         *                   "Name": "PhotoErwin.jpg"
         *                   "Url": "https://onegml.com/assets..."
         *               }
         *           }
         *       ],
         *       "speakers": [
         *           {
         *               "id": 1,
         *               "name": "Bayu Setiaji",
         *               "title": "Tribe Chief",
         *               "company": "GML",
         *           }
         *       ],         
         *       "investments": [
         *           {
         *               "id": 1,
         *               "title": "string",
         *               "type": "string",
         *               "nominal": 0,
         *               "ppn": true,
         *               "ppnpercent": 0,
         *               "paymenturl": "https://app.sandbox.midtrans.com/payment-links/1588676502361"
         *           }
         *       ],
         *       "emailNotification": true,
         *       "emailSubject": "Konfirmasi Registrasi",
         *       "email": "Terima kasih atas ... "
         *       "errors": [
         *           {
         *               "code": "",
         *               "description": ""
         *           }
         *       ]
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost]
        public async Task<ActionResult<WebEventResponse>> PostWebEvent(WebEventInfo request)
        {
            /*
            // seharusnya bisa seperti ini menggunakan mapper
            var config = new MapperConfiguration(cfg => {
                cfg.CreateMap<WebEventInfo, WebEvent>();
            });
            IMapper iMapper = config.CreateMapper();
            var e2 = iMapper.Map<WebEventInfo, WebEvent>(request);

            // kalau lebih kompleks, bisa juga config nya seperti ini
            var config = new MapperConfiguration(cfg => {
                cfg.CreateMap<AuthorDTO, AuthorModel>()
                   .ForMember(destination => destination.Address,
              map => map.MapFrom(
                  source => new Address
                  {
                      City = source .City,
                      State = source .State,
                      Country = source.Country
                  }));
            */

            if (request.Slug != null && !request.Slug.Equals(""))
            {
                WebEvent e = _context.WebEvents.Where(a => !a.IsDeleted && a.Slug.Equals(request.Slug.Trim().ToLower())).FirstOrDefault();
                if (e != null)
                {
                    return BadRequest(new { error = "Slug already exists." });
                }
            }

            WebEventResponse response = new WebEventResponse();

            DateTime now = DateTime.Now;

            WebEvent webEvent = new WebEvent()
            {
                Title = request.Title,
                Intro = request.Intro,
                Slug = request.Slug != null ? request.Slug.Trim().ToLower() : "",
                MetaTitle = request.MetaTitle != null ? request.MetaTitle.Trim() : "",
                MetaDescription = request.MetaDescription != null ? request.MetaDescription.Trim() : "",
                Keyword = request.Keyword != null ? request.Keyword.Trim() : "",
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Address = request.Address,
                CategoryId = request.CategoryId,
                LocationId = request.LocationId,
                TopicId = request.TopicId,
                Publish = request.Publish,
                Audience = request.Audience,
                AddInfo = "",
                RegistrationURL = request.RegistrationURL,
                VideoURL = request.VideoURL,
                Email = "",
                CdhxCategory = request.CdhxCategory,
                EmailSubject = "",
                TribeId = request.TribeId,
                CreatedDate = now,
                CreatedBy = request.UserId,
                LastUpdated = now,
                LastUpdatedBy = request.UserId,
                IsDeleted = false,
                DeletedBy = 0,
                LinkZoom = request.LinkZoom
            };

            _context.WebEvents.Add(webEvent);
            await _context.SaveChangesAsync();

            response.Event = webEvent;

            Error error2 = await AddEventDescription(webEvent.Id, request.Description, request.UserId, now);
            if (!error2.Code.Equals("ok"))
            {
                response.Errors.Add(error2);
            }

            Error err = await AddEventThumbnails(webEvent.Id, request.Thumbnails, request.UserId, now, request.ThumbnailFilename);
            if (!err.Code.Equals("ok"))
            {
                response.Errors.Add(err);
            }

            Error error1 = await AddEventBrochures(webEvent.Id, request.Brochures, request.UserId, now, request.BrochuleFilename);
            if (!error1.Code.Equals("ok"))
            {
                response.Errors.Add(error1);
            }

            Error errorf = await AddEventFlyers(webEvent.Id, request.Flyer, request.UserId, now, request.FlyerFilename);
            if (!errorf.Code.Equals("ok"))
            {
                response.Errors.Add(errorf);
            }

            Error error3 = await AddEventAgenda(webEvent.Id, request.Agenda, request.UserId, now);
            if (!error3.Code.Equals("ok"))
            {
                response.Errors.Add(error3);
            }

            Error error4 = await AddEventSpeakers(webEvent.Id, request.Speakers, request.UserId, now);
            if (!error4.Code.Equals("ok"))
            {
                response.Errors.Add(error4);
            }

            Error error5 = await AddEventTestimonies(webEvent.Id, request.Testimonies, request.UserId, now);
            if (!error5.Code.Equals("ok"))
            {
                response.Errors.Add(error5);
            }

            Error error6 = await AddEventInvestments(webEvent.Id, request.Investments, request.UserId, now);
            if (!error6.Code.Equals("ok"))
            {
                response.Errors.Add(error6);
            }

            Error error7 = await AddEventTakeaways(webEvent.Id, request.Takeaways, request.UserId, now);
            if (!error7.Code.Equals("ok"))
            {
                response.Errors.Add(error7);
            }

            Error error8 = await AddEventNotification(webEvent.Id, request.EmailNotification, request.EmailSubject, request.Email, now, request.UserId);
            if (!error8.Code.Equals("ok"))
            {
                response.Errors.Add(error8);
            }

            response.Category = GetEventCategory(response.Event.CategoryId);
            response.Location = GetEventLocation(response.Event.LocationId);
            response.Topic = GetEventTopic(response.Event.TopicId);
            response.Description = GetEventDescription(response.Event.Id);
            response.Thumbnail = GetEventThumbnails(response.Event.Id);
            response.Brochure = GetWebEventBrochures(response.Event.Id);
            response.Flyer = GetWebEventFlyers(response.Event.Id);
            response.Agenda = GetWebEventAgendas(response.Event.Id);
            response.Testimonies = GetWebEventTestimonies(response.Event.Id);
            response.Investments = GetWebEventInvestments(response.Event.Id);
            response.Takeaways = GetWebEventTakeaways(response.Event.Id);
            response.Speakers = GetWebEventSpeakers(response.Event.Id);
            response.EmailNotification = request.EmailNotification;
            response.EmailSubject = response.EmailSubject;
            response.Email = request.Email;
            response.Errors.Add(new Error("ok", ""));

            //{
            //  "contentId": 2,
            //  "title": "CDHX Try Inser 2",
            //  "description": "Testing",
            //  "slug": "cdhx-trying-insert",
            //  "categoryID": 1,
            //  "metaTitle": "string",
            //  "metaDescription": "string",
            //  "metaKeyword": "string",
            //  "dateStart": "29/04/2023 11:50:00",
            //  "dateEnd": "29/04/2023 13:50:00",
            //  "price": 20000000,
            //  "isOfflineEvent": 1,
            //  "addressOfflineEvent": "GML",
            //  "directZoom": "",
            //  "isDeleted": false,
            //  "imageURL": "https://qubisastorage.blob.core.windows.net/files/webinars/1585/images/img480/1585-webinar.jpg"
            //}

            if (request.Publish == true)
            {

                string url = "https://qubisaapi.azurewebsites.net/";

                HttpClient httpClient = new HttpClient
                {
                    BaseAddress = new Uri(url)
                };

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "cXViaXNhYXBpOmt5SF9xenc5alQzanJodUo0NDk/OE5kSjdrJUJFWVMjRS1iUmdZZWQhR2NlXmpDYzVOUVJuTFZVVEh4X14tQiUqeG1MeTVGN05yWlZwbmtOdzZ6eW1wU0w2a0BOX0d3V0VUR2doY3FMNSolQlI/TEJKRkZyJUx3Yng2VWZDanRK");

                int priceFinal = 0;
                bool isOffline = false;
                foreach (var t in response.Investments)
                {
                    if (response.Event.CategoryId == 3)
                    {
                        // Online
                        if (t.Title == "Investasi Online")
                        {
                            priceFinal = t.Nominal;
                        }
                    }
                    else if (response.Event.CategoryId == 5)
                    {
                        // Offline
                        if (t.Title == "Investasi Offline")
                        {
                            priceFinal = t.Nominal;
                            isOffline = true;
                        }
                    }
                }

                if(response.Event.CategoryId == 5)
                {
                    isOffline = true;
                }


                CdhxQubisa payload = new CdhxQubisa()
                {
                    contentId = response.Event.Id,
                    title = response.Event.Title,
                    description = request.Description,
                    slug = response.Event.Slug,
                    categoryURL = response.Event.CdhxCategory,
                    metaTitle = response.Event.MetaTitle,
                    metaDescription = response.Event.MetaDescription,
                    metaKeyword = response.Event.Keyword, 
                    dateStart = DateTime.Parse(response.Event.FromDate.ToString()).ToString("dd/MM/yyyy") + " " + response.Event.StartTime + ":00",
                    dateEnd = DateTime.Parse(response.Event.ToDate.ToString()).ToString("dd/MM/yyyy") + " " + response.Event.EndTime + ":00",
                    price = priceFinal,
                    isOfflineEvent = isOffline,
                    addressOfflineEvent = response.Event.Address,
                    directZoom = response.Event.LinkZoom,
                    isDeleted = false,
                    isPublished = request.Publish,
                    imageURL = response.Flyer.Url,
                    youtubeVideoURL = response.Event.VideoURL,
                    BrochureURL = GetWebEventBrochures(response.Event.Id).Url
                };

                response.PayloadQubisa = payload;
                HttpResponseMessage httpResponseMessage = await httpClient.PostAsJsonAsync("instructor/v1/Webinar", payload).ConfigureAwait(false);
                Console.WriteLine(payload);
                Console.WriteLine(httpResponseMessage);
            }
            return response;
        }

        /**
         * @api {get} /event/slug/{slug} GET event by slug
         * @apiVersion 1.0.0
         * @apiName GetWebEventBySlug
         * @apiGroup Event
         * @apiPermission Basic Authentication
         * 
         * @apiParam {string} slug      slug dari event yang bersangkutan
         *   
         * @apiSuccessExample Success-Response:
         *   {
         *       "event": {
         *          "id": 1,
         *          "userId": 3,
         *          "title": "Judul Event",
         *          "Intro": "Webinar ini mendiskusikan tentang..."
         *          "fromDate": "2020-04-03T09.00",
         *          "toDate": "2020-05-03T17.00",
         *          "startTime": "15:00",
         *          "endTime": "16.30",
         *          "address": "Hotel Mandarin",
         *          "categoryId": 1,
         *          "publish": 0,
         *          "audience": "",
         *          "description": "Deskripsi event ini dari CKEditor",
         *          "thumbnailFilename": "NamaFileThumbnail.jpg"
         *          "thumbnails": "base64 untuk thumbnail",
         *          "brochuleFilename": "NamaFileBrosur.jpg",
         *          "brochures": "base64 untuk brosur",
         *          "flyerFilename": "NamaFileFlyer.jpg",
         *          "flyer": "base64 untuk fluer",         
         *          "agenda": [
         *           {
         *               "id": 3,
         *               "date": "2020-04-03",
         *               "startTime": "09:00",
         *               "endTime": "17:00",
         *               "description": "Agenda 1...."
         *           },
         *           {
         *               "id": 2,
         *               "date": "2020-05-03",
         *               "startTime": "09:00",
         *               "endTime": "17:00",
         *               "description": "Agenda 2...."
         *           }
         *       ],
         *       "testimonies": [
         *           {
         *               "id": 2,
         *               "name": "Tinus Garnida",
         *               "title": "HR Head",
         *               "company": "GML Performance Consulting",
         *               "testimony": "Workshop ini sangat bermanfaat.",
         *               "photoFilename": "FilePhoto.jpg",
         *               "testimonyPhotos": "base64 untuk foto"
         *           }
         *       ],
         *       "speakers": [
         *           {
         *               "id": 1,
         *               "name": "Bayu Setiaji",
         *               "title": "Tribe Chief",
         *               "company": "GML Performance Consulting"
         *               "profile": "base64 untuk foto speaker",
         *               "profileFilename": "Fotopembicara.jpg",
         *           }
         *       ],
         *       "investments": [
         *           {
         *               "id": 2,
         *               "title": "",
         *               "type": "Normal",
         *               "nominal": 6000000,
         *               "ppn": 1,
         *               "ppnpercent": 10,
         *               "paymenturl": "https://app.sandbox.midtrans.com/payment-links/1588676502361"
         *           },
         *           {
         *               "id": 3,
         *               "title": "",
         *               "type": "Early Bird",
         *               "nominal": 5000000,
         *               "ppn": 1,
         *               "ppnpercent": 10,
         *               "paymenturl": "https://app.sandbox.midtrans.com/payment-links/1588732923215"
         *           }
         *       ]
         *   }
         * 
         */
        [AllowAnonymous]
        [HttpGet("slug/{slug}")]
        public async Task<ActionResult<WebEventResponse>> GetWebEventBySlug(string slug)
        {
            WebEvent e = _context.WebEvents.Where(a => a.Slug.Equals(slug.Trim().ToLower()) && !a.IsDeleted).FirstOrDefault();
            if (e == null) return NotFound();

            return await GetWebEvent(e.Id);
        }


        /**
        * @api {get} /event/{id} GET event by id 
        * @apiVersion 1.0.0
        * @apiName GetWebEvent
        * @apiGroup Event
        * @apiPermission Basic Authentication
        * 
        * @apiParam {Number} id Id dari event yang bersangkutan
        *   
        * @apiSuccessExample Success-Response:
        *   {
        *       "event": {
        *          "id": 1,
        *          "userId": 3,
        *          "title": "Judul Event",
        *          "Intro": "Webinar ini mendiskusikan tentang..."
        *          "fromDate": "2020-04-03T09.00",
        *          "toDate": "2020-05-03T17.00",
        *          "startTime": "15:00",
        *          "endTime": "16.30",
        *          "address": "Hotel Mandarin",
        *          "categoryId": 1,
        *          "publish": 0,
        *          "audience": "",
        *          "description": "Deskripsi event ini dari CKEditor",
        *          "thumbnailFilename": "NamaFileThumbnail.jpg"
        *          "thumbnails": "base64 untuk thumbnail",
        *          "brochuleFilename": "NamaFileBrosur.jpg",
        *          "brochures": "base64 untuk brosur",
        *          "tribeId": 0,         
         *         "videoURL": "https://www.yourube.com/abcdefgh",         
        *          "agenda": [
        *           {
        *               "id": 3,
        *               "date": "2020-04-03",
        *               "startTime": "09:00",
        *               "endTime": "17:00",
        *               "description": "Agenda 1...."
        *           },
        *           {
        *               "id": 2,
        *               "date": "2020-05-03",
        *               "startTime": "09:00",
        *               "endTime": "17:00",
        *               "description": "Agenda 2...."
        *           }
        *       ],
        *       "testimonies": [
        *           {
        *               "id": 2,
        *               "name": "Tinus Garnida",
        *               "title": "HR Head",
        *               "company": "GML Performance Consulting",
        *               "testimony": "Workshop ini sangat bermanfaat.",
        *               "photoFilename": "FilePhoto.jpg",
        *               "testimonyPhotos": "base64 untuk foto"
        *           }
        *       ],
        *       "speakers": [
        *           {
        *               "id": 1,
        *               "name": "Bayu Setiaji",
        *               "title": "Tribe Chief",
        *               "company": "GML Performance Consulting"
        *           }
        *       ],
        *       "investments": [
        *           {
        *               "id": 2,
        *               "title": "",
        *               "type": "Normal",
        *               "nominal": 6000000,
        *               "ppn": 1,
        *               "ppnpercent": 10,
        *               "paymenturl": "https://app.sandbox.midtrans.com/payment-links/1588676502361"
        *           },
        *           {
        *               "id": 3,
        *               "title": "",
        *               "type": "Early Bird",
        *               "nominal": 5000000,
        *               "ppn": 1,
        *               "ppnpercent": 10,
        *               "paymenturl": "https://app.sandbox.midtrans.com/payment-links/1588732923215"
        *           }
        *       ]
        *   }
        * 
        */
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<WebEventResponse>> GetWebEvent(int id)
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            authHeader = authHeader.Trim();
            if (authHeader.Equals(""))
            {
                return Unauthorized();
            }

            string encodedCredentials = authHeader.Substring(6);
            var credentialBytes = Convert.FromBase64String(encodedCredentials);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
            var username = credentials[0];
            var password = credentials[1];
            if (username != "onegmlapi" || password != "O1n6e0G4M7L")
            {
                return Unauthorized();
            }

            WebEventResponse response = new WebEventResponse();

            response.Event = GetWebEventById(id);

            if (response.Event == null || response.Event.Id == 0)
            {
                return NotFound();
            }
            response.Category = GetEventCategory(response.Event.CategoryId);
            response.Topic = GetEventTopic(response.Event.TopicId);
            response.Location = GetEventLocation(response.Event.LocationId);
            response.Description = GetEventDescription(response.Event.Id);
            response.Thumbnail = GetEventThumbnails(response.Event.Id);
            response.Brochure = GetWebEventBrochures(response.Event.Id);
            response.Flyer = GetWebEventFlyers(response.Event.Id);
            response.Agenda = GetWebEventAgendas(response.Event.Id);
            response.Testimonies = GetWebEventTestimonies(response.Event.Id);
            response.Investments = GetWebEventInvestments(response.Event.Id);
            response.Takeaways = GetWebEventTakeaways(response.Event.Id);
            response.Speakers = GetWebEventSpeakers(response.Event.Id);

            WebEventNotification notification = _context.WebEventNotifications.Where(a => a.EventId == response.Event.Id && !a.IsDeleted).FirstOrDefault();
            if(notification == null)
            {
                response.EmailNotification = false;
                response.EmailSubject = "";
                response.Email = "";
            }
            else
            {
                response.EmailNotification = notification.EmailNotification;
                response.EmailSubject = notification.EmailSubject;
                response.Email = notification.Email;
            }
            return response;
        }

        [AllowAnonymous]
        [HttpGet("visitor")]
        public async Task<ActionResult<WebEventVisitorResponse>> GetVisitor()
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            authHeader = authHeader.Trim();
            if (authHeader.Equals(""))
            {
                return Unauthorized();
            }

            string encodedCredentials = authHeader.Substring(6);
            var credentialBytes = Convert.FromBase64String(encodedCredentials);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
            var username = credentials[0];
            var password = credentials[1];
            if (username != "onegmlapi" || password != "O1n6e0G4M7L")
            {
                return Unauthorized();
            }

            WebEventVisitorResponse response = new WebEventVisitorResponse();

            response.Visitor = GetVisitorById(1).Visitor;

            return response;
        }

        [AllowAnonymous]
        [HttpPost("add_visitor")]
        public async Task<ActionResult<WebEventVisitorResponse>> PostEventVisitor()
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            authHeader = authHeader.Trim();
            if (authHeader.Equals(""))
            {
                return Unauthorized();
            }

            string encodedCredentials = authHeader.Substring(6);
            var credentialBytes = Convert.FromBase64String(encodedCredentials);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
            var username = credentials[0];
            var password = credentials[1];
            if (username != "onegmlapi" || password != "O1n6e0G4M7L")
            {
                return Unauthorized();
            }

            await UpdateVisitor();
            
            WebEventVisitorResponse response = new WebEventVisitorResponse();

            response.Visitor = GetVisitorById(1).Visitor;

            return response;
        }

        /**
         * @api {put} /event/{id} PUT event
         * @apiVersion 1.0.0
         * @apiName PutWebEvent
         * @apiGroup Event
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} id        Id dari event yang bersangkutan. Harus sama dengan id yang di body.
         * @apiParamExample {json} Request-Example:
         *   {
         *       "id": 0,
         *       "userId": 3,
         *       "title": "Judul Event",
         *       "Intro": "Webinar ini mendiskusikan tentang..."
         *       "fromDate": "2020-04-03T09.00",
         *       "toDate": "2020-05-03T17.00",
         *       "startTime": "15:00",
         *       "endTime": "16.30",
         *       "address": "Hotel Mandarin",
         *       "categoryId": 1,
         *       "locationId": 1,
         *       "topicId": 1,
         *       "registrationURL": "",
         *       "publish": 0,
         *       "audience": "",
         *       "description": "Deskripsi event ini dari CKEditor",
         *       "thumbnailFilename": "NamaFileThumbnail.jpg"
         *       "thumbnails": "base64 untuk thumbnail",
         *       "brochuleFilename": "NamaFileBrosur.jpg",
         *       "brochures": "base64 untuk brosur",
         *       "cdhxCategory": "hr-cademy",         
         *       "tribeId": 0,         
         *       "videoURL": "https://www.yourube.com/abcdefgh",         
         *       "agenda": [
         *           {
         *               "id": 0,
         *               "date": "2020-04-03",
         *               "startTime": "09:00",
         *               "endTime": "17:00",
         *               "description": "Agenda 1...."
         *           },
         *           {
         *               "id": 0,
         *               "date": "2020-05-03",
         *               "startTime": "09:00",
         *               "endTime": "17:00",
         *               "description": "Agenda 2...."
         *           }
         *       ],
         *       "testimonies": [
         *           {
         *               "id": 0,
         *               "name": "Tinus Garnida",
         *               "title": "HR Head",
         *               "company": "GML Performance Consulting",
         *               "testimony": "Workshop ini sangat bermanfaat.",
         *               "photoFilename": "FilePhoto.jpg",
         *               "testimonyPhotos": "base64 untuk foto"
         *           }
         *       ],
         *       "speakers": [
         *           {
         *               "id": 0,
         *               "name": "Bayu Setiaji",
         *               "title": "Tribe Chief",
         *               "company": "GML Performance Consulting"
         *           }
         *       ],
         *       "investments": [
         *           {
         *               "id": 0,
         *               "title": "",
         *               "type": "Normal",
         *               "nominal": 6000000,
         *               "ppn": 1,
         *               "ppnpercent": 10,
         *               "paymenturl": "https://app.sandbox.midtrans.com/payment-links/1588732923215"
         *           },
         *           {
         *               "id": 0,
         *               "title": "",
         *               "type": "Early Bird",
         *               "nominal": 5000000,
         *               "ppn": 1,
         *               "ppnpercent": 10,
         *               "paymenturl": "https://app.sandbox.midtrans.com/payment-links/1588676502361"
         *           }
         *       ],
         *       "takeaways": [
         *           "Akses selamanya", "Sertifikat dari badan resmi"
         *       ],
         *       "emailNotification": true,
         *       "emailSubject": "Konfirmasi Registrasi",
         *       "email": "Terima kasih atas ... "
         *   }
         *   
         * @apiSuccessExample Success-Response:
         * [
         *   {
         *     "code": "string",
         *     "description": "string"
         *   }
         * ]
         * 
         * @apiError NotAuthorized Token salah.
         * @apiError BadRequest Event Id yang ada di URL berbeda dengan yang ada di body.
         */
     
        [HttpPut("{id}")]
        [Authorize(Policy = "ApiUser")]
        public async Task<ActionResult<List<Error>>> PutWebEvent(int id, WebEventInfo request)
        {
            List<Error> response = new List<Error>();

            if (id != request.Id)
            {
                return BadRequest();
            }

            if (!EventExists(id))
            {
                return NotFound();
            }

            request.Description = request.Description == null ? "" : request.Description;

            if (request.Slug != null && !request.Slug.Equals(""))
            {
                WebEvent ev = _context.WebEvents.Where(a => a.Id != id && !a.IsDeleted && a.Slug.Equals(request.Slug.Trim().ToLower())).FirstOrDefault();
                if (ev != null)
                {
                    return BadRequest(new { error = "Slug already exists." });
                }
            }

            DateTime now = DateTime.Now;

            WebEvent e = GetWebEventById(id);
            e.Title = request.Title;
            e.Intro = request.Intro;
            e.Slug = request.Slug != null ? request.Slug.Trim().ToLower() : "";
            e.MetaTitle = request.MetaTitle != null ? request.MetaTitle.Trim() : "";
            e.MetaDescription = request.MetaDescription != null ? request.MetaDescription.Trim() : "";
            e.Keyword = request.Keyword != null ? request.Keyword.Trim() : "";
            e.MetaTitle = request.MetaTitle;
            e.MetaDescription = request.MetaDescription;
            e.Keyword = request.Keyword;
            e.FromDate = request.FromDate;
            e.ToDate = request.ToDate;
            e.StartTime = request.StartTime;
            e.EndTime = request.EndTime;
            e.Address = request.Address;
            e.CategoryId = request.CategoryId;
            e.TopicId = request.TopicId;
            e.LocationId = request.LocationId;
            e.Publish = request.Publish;
            e.Audience = request.Audience;
            e.RegistrationURL = request.RegistrationURL;
            e.VideoURL = request.VideoURL;
            e.CdhxCategory = request.CdhxCategory;
            e.TribeId = request.TribeId;
            e.LastUpdatedBy = request.UserId;
            e.LastUpdated = now;
            e.LinkZoom = request.LinkZoom;

            try
            {
                _context.Entry(e).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch
            {
                response.Add(new Error("event", "Error updating database for event"));
                return response;
            }

            Error error = await UpdateSpeakers(e.Id, request.Speakers, request.UserId, now);
            if (!error.Code.Equals("ok"))
            {
                response.Add(error); ;
            }

            Error error1 = await UpdateEventDescription(e.Id, request.Description, request.UserId, now);
            if (!error1.Code.Equals("ok"))
            {
                response.Add(error1); ;
            }

            Error error2 = await UpdateEventThumbnails(e.Id, request.Thumbnails, request.UserId, now, request.ThumbnailFilename);
            if (!error2.Code.Equals("ok"))
            {
                response.Add(error2); ;
            }

            Error error3 = await UpdateEventBrochures(e.Id, request.Brochures, request.UserId, now, request.BrochuleFilename);
            if (!error3.Code.Equals("ok"))
            {
                response.Add(error3); ;
            }

            Error errorf = await UpdateEventFlyers(e.Id, request.Flyer, request.UserId, now, request.FlyerFilename);
            if (!errorf.Code.Equals("ok"))
            {
                response.Add(errorf); ;
            }

            Error error4 = await UpdateEventAgenda(e.Id, request.Agenda, request.UserId, now);
            if (!error4.Code.Equals("ok"))
            {
                response.Add(error4); ;
            }

            Error error5 = await UpdateEventTestimonies(e.Id, request.Testimonies, request.UserId, now);
            if (!error5.Code.Equals("ok"))
            {
                response.Add(error5); ;
            }

            Error error6 = await UpdateEventInvestments(e.Id, request.Investments, request.UserId, now);
            if (!error6.Code.Equals("ok"))
            {
                response.Add(error6); ;
            }

            Error error7 = await UpdateEventTakeaways(e.Id, request.Takeaways, request.UserId, now);
            if (!error7.Code.Equals("ok"))
            {
                response.Add(error7); ;
            }

            Error error8 = await UpdateEventNotification(e.Id, request.EmailNotification, request.EmailSubject, request.Email, now, request.UserId);
            if (!error8.Code.Equals("ok"))
            {
                response.Add(error8); ;
            }

            //if (request.Publish == true)
            //{

            string url = "https://qubisaapi.azurewebsites.net/";

            HttpClient httpClient = new HttpClient
            {
                BaseAddress = new Uri(url)
            };

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "cXViaXNhYXBpOmt5SF9xenc5alQzanJodUo0NDk/OE5kSjdrJUJFWVMjRS1iUmdZZWQhR2NlXmpDYzVOUVJuTFZVVEh4X14tQiUqeG1MeTVGN05yWlZwbmtOdzZ6eW1wU0w2a0BOX0d3V0VUR2doY3FMNSolQlI/TEJKRkZyJUx3Yng2VWZDanRK");

            int priceFinal = 0;
            bool isOffline = false;
            foreach (var t in request.Investments)
            {
                if (request.CategoryId == 3)
                {
                    // Online
                    if (t.Title == "Investasi Online")
                    {
                        priceFinal = t.Nominal;
                    }
                }
                else if (request.CategoryId == 5)
                {
                    // Offline
                    if (t.Title == "Investasi Offline")
                    {
                        priceFinal = t.Nominal;
                        isOffline = true;
                    }
                }
            }

            if (request.CategoryId == 5)
            {
                isOffline = true;
            }

            CdhxQubisa payload = new CdhxQubisa()
            {
                contentId = request.Id,
                title = request.Title,
                description = request.Description,
                slug = request.Slug,
                categoryURL = request.CdhxCategory,
                metaTitle = request.MetaTitle,
                metaDescription = request.MetaDescription,
                metaKeyword = request.Keyword,
                dateStart = DateTime.Parse(request.FromDate.ToString()).ToString("dd/MM/yyyy") + " " + request.StartTime + ":00",
                dateEnd = DateTime.Parse(request.ToDate.ToString()).ToString("dd/MM/yyyy") + " " + request.EndTime + ":00",
                price = priceFinal,
                isOfflineEvent = isOffline,
                addressOfflineEvent = request.Address,
                directZoom = request.LinkZoom,
                isDeleted = false,
                isPublished = request.Publish,
                imageURL = GetWebEventFlyers(request.Id).Url,
                youtubeVideoURL = request.VideoURL,
                BrochureURL = GetWebEventBrochures(request.Id).Url
            };


            HttpResponseMessage httpResponseMessage = await httpClient.PostAsJsonAsync("instructor/v1/Webinar", payload).ConfigureAwait(false);
            Console.WriteLine($"Response Status: {httpResponseMessage.StatusCode}");

            // Log response content
            string responseContent = await httpResponseMessage.Content.ReadAsStringAsync();
            Console.WriteLine($"Response Content: {responseContent}");
            Console.WriteLine(JsonConvert.SerializeObject(payload, Formatting.Indented));
            //}

            return response;
        }

        /**
         * @api {get} /Event/publish/{id}/{publish}/{userId} GET Publish event  
         * @apiVersion 1.0.0
         * @apiName PublishWebEvent
         * @apiGroup Event
         * @apiPermission ApiUser
         * @apiDescription Mem-publish event dengan id tertentu. 
         * 
         * @apiParam {Number} id            Id dari event yang ingin di-publish
         * @apiParam {Number} publish       1 untuk publish, 0 untuk tidak mem-publish         
         * @apiParam {Number} userId        userId dari user yang login
         * @apiSuccessExample Success-Response:        
         * {
         *   "id": 4,
         *   "title": "Five Practice of Execution Winners",
         *   "intro": "Saat ini isu-isu yang sering muncul adalah bagaimana dapat mengeksekusi strategi yang telah direncanakan secara efektif. Masalah yang dialami oleh sebagian besar manajer adalah terjebak dengan rutinitas atau tidak memfokuskan kepada hal yang penting untuk dicapai bagi unit yang Anda pimpin.",
         *   "categoryId": 4,
         *   "fromDate": "2020-04-21T00:00:00",
         *   "toDate": "2020-06-18T00:00:00",
         *   "startTime": null,
         *   "endTime": null,
         *   "audience": "",
         *   "address": "",
         *   "publish": true,
         *   "addInfo": "",
         *   "createdDate": "2020-04-04T15:14:36.3566958",
         *   "createdBy": 3,
         *   "lastUpdated": "2020-04-09T14:21:05.6496678+07:00",
         *   "lastUpdatedBy": 3,
         *   "isDeleted": false,
         *   "deletedBy": 0,
         *   "deletedDate": "0001-01-01T00:00:00"
         * }
         * 
         * @apiError NotFound    id salah
         * 
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("publish/{id}/{publish}/{userId}")]
        public async Task<ActionResult<WebEvent>> PublishWebEvent(int id, int publish, int userId)
        {
            var webEvent = await _context.WebEvents.FindAsync(id);
            if (webEvent == null)
            {
                return NotFound();
            }
            DateTime now = DateTime.Now;
            webEvent.LastUpdated = now;
            webEvent.LastUpdatedBy = userId;
            webEvent.Publish = publish == 1;

            _context.Entry(webEvent).State = EntityState.Modified;
            await _context.SaveChangesAsync();


            string url = "https://qubisaapi.azurewebsites.net/";

            HttpClient httpClient = new HttpClient
            {
                BaseAddress = new Uri(url)
            };

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "cXViaXNhYXBpOmt5SF9xenc5alQzanJodUo0NDk/OE5kSjdrJUJFWVMjRS1iUmdZZWQhR2NlXmpDYzVOUVJuTFZVVEh4X14tQiUqeG1MeTVGN05yWlZwbmtOdzZ6eW1wU0w2a0BOX0d3V0VUR2doY3FMNSolQlI/TEJKRkZyJUx3Yng2VWZDanRK");

            CdhxQubisaDelete payload = new CdhxQubisaDelete()
            {
                contentId = id,
                isDeleted = webEvent.Publish
            };

            HttpResponseMessage httpResponseMessage = await httpClient.PostAsJsonAsync("instructor/v1/Webinar", payload).ConfigureAwait(false);
            Console.WriteLine(payload);


            return webEvent;
        }

        /**
         * @api {post} /event/image         POST image untuk event
         * @apiVersion 1.0.0
         * @apiName PostEventImage
         * @apiGroup Event
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} id              0 untuk post
         * @apiParam {String} caption         Caption atau deskripsi untuk image ini
         * @apiParam {Number} eventId         Id dari event untuk image ini
         * @apiParam {File} image             File image yang ingin diupload
         * @apiParam {Number} publish         0 untuk draft, 1 untuk publish
         * @apiParam {Number} userId          Id dari user yang login
         * 
         * @apiSuccessExample Success-Response:
         * {
         *     "id": 33,
         *     "caption": "Ini caption untuk image ini",
         *     "imageURL": "http://localhost/assets/events/4/ow0owstm.sqq.jpg",
         *     "eventId": 4,
         *     "Publish": 0,
         *     "errors": []
         * }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost("image")]
        public async Task<ActionResult<WebEventImageResponse>> PostEventImage([FromForm] WebEventImageRequest request)
        {
            WebEventImageResponse response = new WebEventImageResponse();

            if (request.EventId == 0)
            {
                response.Errors.Add(new Error("event", "Event id cannot be null or 0."));
            }
            else if (!EventExists(request.EventId))
            {
                response.Errors.Add(new Error("event", "Invalid event id."));
            }
            else
            {
                foreach (IFormFile formFile in request.Image)
                {
                    if (formFile.Length > 0)
                    {
                        var fileExt = System.IO.Path.GetExtension(formFile.FileName).Substring(1).ToLower();
                        if (!_fileService.checkFileExtension(fileExt, new[] { "jpg", "jpeg", "png" }))
                        {
                            response.Errors.Add(new Error("extension", "Please upload PNG or JPG file only."));
                            return response;
                        }
                        else
                        {
                            DateTime now = DateTime.Now;

                            string randomName = Path.GetRandomFileName() + "." + fileExt;
                            string fileDir = Path.Combine(_options.AssetsRootDirectory, @"events", request.EventId.ToString());
                            if (_fileService.CheckAndCreateDirectory(fileDir))
                            {
                                var fileName = Path.Combine(fileDir, randomName);

                                Stream stream = formFile.OpenReadStream();
                                /*
                                using (var imagedata = System.Drawing.Image.FromStream(stream))
                                {
                                    _fileService.ResizeImage(imagedata, imagedata.Width, imagedata.Height, fileName, fileExt);
                                }
                                */
                                _fileService.SaveFromStream(stream, fileName);
                                stream.Dispose();

                                int docId = request.Publish == 0 ? IMAGE_DRAFT : IMAGE_PUBLISH;

                                Error e = await SaveImageToDb(new string[] { formFile.FileName, randomName, fileExt }, request.EventId, 0, 0, docId, 0, 0, now, request.UserId);
                                if (!e.Code.Equals("ok"))
                                {
                                    response.Errors.Add(e);
                                }
                                else
                                {
                                    try
                                    {
                                        int imageId = Int32.Parse(e.Description);
                                        string str = request.Caption.Trim();
                                        if (!str.Equals(""))
                                        {
                                            WebEventImageCaption caption = new WebEventImageCaption()
                                            {
                                                EventImageId = imageId,
                                                Caption = str,
                                                CreatedDate = now,
                                                CreatedBy = request.UserId,
                                                LastUpdated = now,
                                                LastUpdatedBy = request.UserId,
                                                IsDeleted = false,
                                                DeletedBy = 0
                                            };
                                            _context.WebEventImageCaptions.Add(caption);
                                            await _context.SaveChangesAsync();
                                        }

                                        response.Id = imageId;
                                        response.EventId = request.EventId;
                                        response.Publish = request.Publish;
                                        response.Caption = str;
                                        response.ImageURL = getAssetsUrl(request.EventId, randomName, "");
                                    }
                                    catch
                                    {
                                        response.Errors.Add(new Error("caption", "Error in updating db for caption"));
                                    }



                                }
                            }
                            else
                            {
                                response.Errors.Add(new Error("directory", "Error in creating directory."));
                            }

                        }
                    }
                }

            }

            return response;
        }

        /**
         * @api {get} /event/image/{categoryId}/{publish}/{page}/{perPage}/{search} GET list images 
         * @apiVersion 1.0.0
         * @apiName GetEventImages
         * @apiGroup Event
         * @apiPermission Basic authentication
         * 
         * @apiParam {Number} categoryId      0 untuk semua kategori, 1: Mega Seminar, 2: Public workshop, 3: Webinar, 4: Blended learning
         * @apiParam {Number} publish         0 untuk draft, 1 untuk publish
         * @apiParam {Number} page            Halaman yang ditampilkan
         * @apiParam {Number} perPage         Jumlah data per halaman
         * @apiParam {String} search          Kata yang mau dicari di judul event. * untuk semua
         * 
         * @apiSuccessExample Success-Response:
         * {
         *     "images": [
         *         {
         *             "id": 33,
         *             "caption": "Ini caption untuk image ini",
         *             "imageURL": "http://localhost/assets/events/4/ow0owstm.sqq.jpg",
         *             "eventId": 4,
         *             "ImageName": "5practices.5d4112f5.jpg",
         *             "Event": "Five Practice of Execution Winners",
         *             "Publish": 0,
         *             "errors": []
         *         }
         *     ],
         *     "info": {
         *         "page": 1,
         *         "perPage": 10,
         *         "total": 1
         *     }
         * }
         */
        [AllowAnonymous]
        [HttpGet("image/{categoryId}/{publish}/{page}/{perPage}/{search}")]
        public async Task<ActionResult<WebEventImageGetResponse>> GetEventImages(int categoryId, int publish, int page, int perPage, string search)
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            authHeader = authHeader.Trim();
            if (authHeader.Equals(""))
            {
                return Unauthorized();
            }

            string encodedCredentials = authHeader.Substring(6);
            var credentialBytes = Convert.FromBase64String(encodedCredentials);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
            var username = credentials[0];
            var password = credentials[1];
            if (username != "onegmlapi" || password != "O1n6e0G4M7L")
            {
                return Unauthorized();
            }

            WebEventImageGetResponse response = new WebEventImageGetResponse();

            IQueryable<WebEventImageDetailResponse> query;
            int docId = publish == 0 ? IMAGE_DRAFT : IMAGE_PUBLISH;

            if (categoryId == 0)
            {
                if (search.Trim().Equals("*"))
                {
                    query = from image in _context.WebEventImages
                            join caption in _context.WebEventImageCaptions
                            on image.Id equals caption.EventImageId
                            join eve in _context.WebEvents
                            on image.EventId equals eve.Id
                            where !image.IsDeleted && image.DocumentationId == docId && !caption.IsDeleted
                            orderby image.Id descending
                            select new WebEventImageDetailResponse()
                            {
                                Id = image.Id,
                                Caption = caption.Caption,
                                ImageURL = getAssetsUrl(image.EventId, image.Filename, ""),
                                EventId = image.EventId,
                                ImageName = image.Name,
                                Event = eve.Title,
                                Publish = image.DocumentationId == IMAGE_DRAFT ? 0 : 1
                            };

                }
                else
                {
                    query = from image in _context.WebEventImages
                            join caption in _context.WebEventImageCaptions
                            on image.Id equals caption.EventImageId
                            join eve in _context.WebEvents
                            on image.EventId equals eve.Id
                            where !image.IsDeleted && image.DocumentationId == docId && !caption.IsDeleted && eve.Title.Contains(search.Trim())
                            orderby image.Id descending
                            select new WebEventImageDetailResponse()
                            {
                                Id = image.Id,
                                Caption = caption.Caption,
                                ImageURL = getAssetsUrl(image.EventId, image.Filename, ""),
                                EventId = image.EventId,
                                ImageName = image.Name,
                                Event = eve.Title,
                                Publish = image.DocumentationId == IMAGE_DRAFT ? 0 : 1
                            };

                }

            }
            else
            {
                if (search.Trim().Equals("*"))
                {
                    query = from image in _context.WebEventImages
                            join caption in _context.WebEventImageCaptions
                            on image.Id equals caption.EventImageId
                            join eve in _context.WebEvents
                            on image.EventId equals eve.Id
                            where !image.IsDeleted && image.DocumentationId == docId && !caption.IsDeleted && eve.CategoryId == categoryId
                            orderby image.Id descending
                            select new WebEventImageDetailResponse()
                            {
                                Id = image.Id,
                                Caption = caption.Caption,
                                ImageURL = getAssetsUrl(image.EventId, image.Filename, ""),
                                EventId = image.EventId,
                                ImageName = image.Name,
                                Event = eve.Title
                            };

                }
                else
                {
                    query = from image in _context.WebEventImages
                            join caption in _context.WebEventImageCaptions
                            on image.Id equals caption.EventImageId
                            join eve in _context.WebEvents
                            on image.EventId equals eve.Id
                            where !image.IsDeleted && image.DocumentationId == docId && !caption.IsDeleted && eve.CategoryId == categoryId && eve.Title.Contains(search.Trim())
                            orderby image.Id descending
                            select new WebEventImageDetailResponse()
                            {
                                Id = image.Id,
                                Caption = caption.Caption,
                                ImageURL = getAssetsUrl(image.EventId, image.Filename, ""),
                                EventId = image.EventId,
                                ImageName = image.Name,
                                Event = eve.Title
                            };

                }
            }

            int total = query.Count();
            response.images = await query.Skip(perPage * (page - 1)).Take(perPage).ToListAsync<WebEventImageDetailResponse>();
            response.info = new PaginationInfo(page, perPage, total);

            return response;
        }

        /**
         * @api {get} /Event/banner/cdhx/{publish}/{page}/{perPage} GET banner CDHX
         * @apiVersion 1.0.0
         * @apiName GetBannerCDHX
         * @apiGroup Event
         * @apiPermission Basic authentication
         * 
         * @apiParam {Number} publish       1 untuk publish, 0 untuk unpublished, 2 untuk semuanya      
         * @apiParam {Number} page          Halaman yang ditampilkan.
         * @apiParam {Number} perPage       Jumlah data per halaman.
         * 
         * @apiSuccessExample Success-Response:        
         * {
         *     "banners": [
         *         {
         *             "bannerId": 2,
         *             "filename": "Background Webinar 2.jpg",
         *             "url": "http://localhost/assets/web/3mgmh3jz.n0q.jpg",
         *             "mobileFilename": "Background Webinar Mobile 2.jpg",
         *             "mobileUrl": "http://localhost/assets/web/abcd1234.n0q.jpg",
         *             "publish": false,
         *             "link": "https://www.gmlperformance.com"
         *         }
         *     ],
         *     "info": {
         *         "page": 1,
         *         "perPage": 10,
         *         "total": 1
         *     }
         * }
         * 
         * @apiError NotFound    bannerId salah
         * 
         */
        [AllowAnonymous]
        [HttpGet("banner/{category}/{publish}/{page}/{perPage}")]
        public async Task<ActionResult<GetBannerResponse>> GetBannerCDHX(string category, int publish, int page, int perPage)
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            authHeader = authHeader.Trim();
            if (authHeader.Equals(""))
            {
                return Unauthorized();
            }

            string encodedCredentials = authHeader.Substring(6);
            var credentialBytes = Convert.FromBase64String(encodedCredentials);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
            var username = credentials[0];
            var password = credentials[1];
            if (username != "onegmlapi" || password != "O1n6e0G4M7L")
            {
                return Unauthorized();
            }

            return await GetBannerByCategory(publish, page, perPage, category);
        }

        /**
         * @api {get} /Event/banner/{publish}/{page}/{perPage} GET banner  
         * @apiVersion 1.0.0
         * @apiName GetBanner
         * @apiGroup Event
         * @apiPermission Basic authentication
         * 
         * @apiParam {Number} publish       1 untuk publish, 0 untuk unpublished, 2 untuk semuanya      
         * @apiParam {Number} page          Halaman yang ditampilkan.
         * @apiParam {Number} perPage       Jumlah data per halaman.
         * 
         * @apiSuccessExample Success-Response:        
         * {
         *     "banners": [
         *         {
         *             "bannerId": 2,
         *             "filename": "Background Webinar 2.jpg",
         *             "url": "http://localhost/assets/web/3mgmh3jz.n0q.jpg",
         *             "mobileFilename": "Background Webinar Mobile 2.jpg",
         *             "mobileUrl": "http://localhost/assets/web/abcd1234.n0q.jpg",
         *             "publish": false,
         *             "link": "https://www.gmlperformance.com"
         *         }
         *     ],
         *     "info": {
         *         "page": 1,
         *         "perPage": 10,
         *         "total": 1
         *     }
         * }
         * 
         * @apiError NotFound    bannerId salah
         * 
         */
        [AllowAnonymous]
        [HttpGet("banner/{publish}/{page}/{perPage}")]
        public async Task<ActionResult<GetBannerResponse>> GetBanner(int publish, int page, int perPage)
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            authHeader = authHeader.Trim();
            if (authHeader.Equals(""))
            {
                return Unauthorized();
            }

            string encodedCredentials = authHeader.Substring(6);
            var credentialBytes = Convert.FromBase64String(encodedCredentials);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
            var username = credentials[0];
            var password = credentials[1];
            if (username != "onegmlapi" || password != "O1n6e0G4M7L")
            {
                return Unauthorized();
            }

            return await GetBannerByCategory(publish, page, perPage, "");
        }

        /**
         * @api {post} /event/couter           POST counter
         * @apiVersion 1.0.0
         * @apiName PostWebCounter
         * @apiGroup Event
         * @apiPermission ApiUser
         * 
         * @apiParamExample {json} Request-Example:
         * {
         *   "counter": "9384",
         *   "userId": 1
         * }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost("counter")]
        public async Task<ActionResult> PostWebCounter(PostWebCounter request)
        {
            DateTime now = DateTime.Now;
            await UpdateWebCaption("counter", request.Counter, request.UserId, now);

            return NoContent();
        }

        /**
         * @api {post} /event/caption           POST caption
         * @apiVersion 1.0.0
         * @apiName PostWebCaption
         * @apiGroup Event
         * @apiPermission ApiUser
         * 
         * @apiParamExample {json} Request-Example:
         * {
         *   "welcome": "We enhance...",
         *   "welcomeNote": "We help...",
         *   "userId": 1
         * }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost("caption")]
        public async Task<ActionResult> PostWebCaption(PostWebCaption request)
        {
            DateTime now = DateTime.Now;
            await UpdateWebCaption("welcome", request.Welcome, request.UserId, now);
            await UpdateWebCaption("welcomenote", request.WelcomeNote, request.UserId, now);

            return NoContent();
        }

        /**
         * @api {gett} /event/counter           GET counter
         * @apiVersion 1.0.0
         * @apiName GetWebCounter
         * @apiGroup Event
         * @apiPermission AllowAnonymous
         */
        [AllowAnonymous]
        [HttpGet("counter")]
        public async Task<ActionResult<string>> GetWebCounter()
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            authHeader = authHeader.Trim();
            if (authHeader.Equals(""))
            {
                return Unauthorized();
            }

            string encodedCredentials = authHeader.Substring(6);
            var credentialBytes = Convert.FromBase64String(encodedCredentials);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
            var username = credentials[0];
            var password = credentials[1];
            if (username != "onegmlapi" || password != "O1n6e0G4M7L")
            {
                return Unauthorized();
            }

            WebCaption caption = new WebCaption();

            var query = from c in _context.WebTexts
                        where !c.IsDeleted && c.Publish
                        select new
                        {
                            c.Shortname,
                            c.Caption
                        };

            var objs = await query.ToListAsync();
            foreach (var obj in objs)
            {
                if (obj.Shortname.Equals("counter"))
                {
                    return obj.Caption;
                }
            }
            return "9384";
        }

        /**
         * @api {gett} /event/caption           GET caption
         * @apiVersion 1.0.0
         * @apiName GetWebCaption
         * @apiGroup Event
         * @apiPermission AllowAnonymous
         * 
         * @apiSuccessExample Success-Response:
         * {
         *     "welcome": "We enhance your execution capability",
         *     "welcomeNote": "We help you transform your organization’s execution capability through Strategy, Process, Structure, People, and Culture"
         * }
         */
        [AllowAnonymous]
        [HttpGet("caption")]
        public async Task<ActionResult<WebCaption>> GetWebCaption()
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            authHeader = authHeader.Trim();
            if (authHeader.Equals(""))
            {
                return Unauthorized();
            }

            string encodedCredentials = authHeader.Substring(6);
            var credentialBytes = Convert.FromBase64String(encodedCredentials);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
            var username = credentials[0];
            var password = credentials[1];
            if (username != "onegmlapi" || password != "O1n6e0G4M7L")
            {
                return Unauthorized();
            }

            WebCaption caption = new WebCaption();

            var query = from c in _context.WebTexts
                        where !c.IsDeleted && c.Publish
                        select new
                        {
                            c.Shortname,
                            c.Caption
                        };

            var objs = await query.ToListAsync();
            foreach (var obj in objs)
            {
                if (obj.Shortname.Equals("welcome"))
                {
                    caption.Welcome = obj.Caption;
                }
                else if (obj.Shortname.Equals("welcomenote"))
                {
                    caption.WelcomeNote = obj.Caption;
                }
            }
            return caption;
        }

        /**
         * @api {post} /event/banner           POST banner
         * @apiVersion 1.0.0
         * @apiName PostWebBanner
         * @apiGroup Event
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} BannerId        0 untuk banner baru. 
         * @apiParam {File} image             File image yang ingin diupload
         * @apiParam {File} mobileImage       File image untuk mobile
         * @apiParam {Number} publish         0 untuk draft, 1 untuk publish
         * @apiParam {String} link            Link URL ketika banner diklik
         * @apiParam {Number} userId          Id dari user yang login
         * @apiParam {String} category        "" untuk web site GML atau "cdhx" untuk web site CDHX
         * @apiParam {String} title           Judul banner, untuk web site versi baru
         * @apiParam {String} description     Deskripsi banner, untuk web site versi baru
         * 
         * @apiSuccessExample Success-Response:
         * {
         *     "bannerId": 1,
         *     "filename": "Background Webinar 2.jpg",
         *     "url": "http://localhost/assets/web/mayl1ux5.oeo.jpg",
         *     "link": "https://www.gmlperformance.com",
         *     "title": "Judul",
         *     "description": "Deskripsi"
         * }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost("banner")]
        public async Task<ActionResult<WebBannerResponse>> PostWebBanner([FromForm] WebBannerRequest request)
        {
            WebBannerResponse response = new WebBannerResponse();

            foreach (IFormFile formFile in request.Image)
            {
                if (formFile.Length > 0)
                {
                    var fileExt = System.IO.Path.GetExtension(formFile.FileName).Substring(1).ToLower();
                    if (!_fileService.checkFileExtension(fileExt, new[] { "jpg", "jpeg", "png" }))
                    {
                        return BadRequest(new { error = "Please upload PNG or JPG file only." });
                    }
                    else
                    {
                        DateTime now = DateTime.Now;

                        string randomName = Path.GetRandomFileName() + "." + fileExt;
                        string fileDir = Path.Combine(_options.AssetsRootDirectory, @"web");
                        if (_fileService.CheckAndCreateDirectory(fileDir))
                        {
                            var fileName = Path.Combine(fileDir, randomName);

                            Stream stream = formFile.OpenReadStream();
                            /*
                            using (var imagedata = System.Drawing.Image.FromStream(stream))
                            {
                                _fileService.ResizeImage(imagedata, imagedata.Width, imagedata.Height, fileName, fileExt);
                            }
                            */
                            _fileService.SaveFromStream(stream, fileName);
                            stream.Dispose();


                            int maxId = 0;
                            if (_context.WebImages.Where(a => a.BannerId > 0).Count() > 0)
                            {
                                maxId = _context.WebImages.Where(a => a.BannerId > 0).Max(a => a.BannerId);
                            }

                            WebImage image = new WebImage()
                            {
                                Name = formFile.FileName,
                                Filename = randomName,
                                FileType = fileExt,
                                MobileName = "",
                                MobileFilename = "",
                                MobileFileType = "",
                                BannerId = maxId + 1,
                                Publish = request.Publish == 1,
                                Link = request.Link,
                                Category = request.Category.Trim().ToLower(),
                                Title = string.IsNullOrEmpty(request.Title) ? "" : request.Title,
                                Description = string.IsNullOrEmpty(request.Description) ? "" : request.Description,
                                CreatedDate = now,
                                CreatedBy = request.UserId,
                                LastUpdated = now,
                                LastUpdatedBy = request.UserId
                            };

                            _context.WebImages.Add(image);
                            await _context.SaveChangesAsync();

                            response.BannerId = maxId + 1;
                            response.Filename = formFile.FileName;
                            response.URL = _options.AssetsBaseURL + "web/" + randomName;
                            response.MobileFilename = "";
                            response.MobileURL = "";
                            response.Link = request.Link;
                            response.Publish = image.Publish;
                            response.Title = image.Title;
                            response.Description = image.Description;
                        }
                        else
                        {
                            return BadRequest(new { error = "Error in creating directory" });
                        }

                    }
                }
            }
            if(response.BannerId != 0 && request.MobileImage != null)
            {
                foreach (IFormFile mobileFile in request.MobileImage)
                {
                    if (mobileFile.Length > 0)
                    {
                        var fileExt = System.IO.Path.GetExtension(mobileFile.FileName).Substring(1).ToLower();
                        if (!_fileService.checkFileExtension(fileExt, new[] { "jpg", "jpeg", "png" }))
                        {
                            return BadRequest(new { error = "Please upload PNG or JPG file only." });
                        }
                        else
                        {
                            string randomName = Path.GetRandomFileName() + "." + fileExt;
                            string fileDir = Path.Combine(_options.AssetsRootDirectory, @"web");
                            if (_fileService.CheckAndCreateDirectory(fileDir))
                            {
                                WebImage image = _context.WebImages.Where(a => a.BannerId == response.BannerId && !a.IsDeleted).FirstOrDefault();
                                if(image != null)
                                {
                                    image.MobileName = mobileFile.FileName;
                                    image.MobileFilename = randomName;
                                    image.MobileFileType = fileExt;
                                    _context.Entry(image).State = EntityState.Modified;
                                    await _context.SaveChangesAsync();
                                }

                                var fileName = Path.Combine(fileDir, randomName);

                                Stream stream = mobileFile.OpenReadStream();
                                /*
                                using (var imagedata = System.Drawing.Image.FromStream(stream))
                                {
                                    _fileService.ResizeImage(imagedata, imagedata.Width, imagedata.Height, fileName, fileExt);
                                }
                                */
                                _fileService.SaveFromStream(stream, fileName);
                                stream.Dispose();

                                response.MobileFilename = mobileFile.FileName;
                                response.MobileURL = _options.AssetsBaseURL + "web/" + randomName;
                            }
                            else
                            {
                                return BadRequest(new { error = "Error in creating directory" });
                            }

                        }
                    }
                }
            }

            return response;
        }

        /**
         * @api {put} /event/banner           PUT banner
         * @apiVersion 1.0.0
         * @apiName PutWebBanner
         * @apiGroup Event
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} BannerId        0 untuk banner baru. 
         * @apiParam {File} image             File image yang ingin diupload
         * @apiParam {File} mobileImage       File image untuk mobile
         * @apiParam {Number} publish         0 untuk draft, 1 untuk publish
         * @apiParam {String} link            Link URL ketika banner diklik
         * @apiParam {Number} userId          Id dari user yang login
         * @apiParam {String} category        "" untuk web site GML atau "cdhx" untuk web site CDHX
         * @apiParam {String} title           Judul banner, untuk web site versi baru
         * @apiParam {String} description     Deskripsi banner, untuk web site versi baru
         * 
         * @apiSuccessExample Success-Response:
         *   NoContent
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPut("banner")]
        public async Task<ActionResult> PutWebBanner([FromForm] WebBannerRequest request)
        {
            DateTime now = DateTime.Now;

            WebImage image = _context.WebImages.Where(a => a.BannerId == request.BannerId).FirstOrDefault();
            if (image == null || image.Id == 0)
            {
                return NotFound();
            }

            image.Title = string.IsNullOrEmpty(request.Title) ? "" : request.Title;
            image.Description = string.IsNullOrEmpty(request.Description) ? "" : request.Description;
            image.Publish = request.Publish == 1;
            image.Link = request.Link;
            image.Category = request.Category.Trim().ToLower();
            image.LastUpdated = now;
            image.LastUpdatedBy = request.UserId;

            if (request.Image != null)
            {
                foreach (IFormFile formFile in request.Image)
                {
                    if (formFile.Length > 0)
                    {
                        var fileExt = System.IO.Path.GetExtension(formFile.FileName).Substring(1).ToLower();
                        if (!_fileService.checkFileExtension(fileExt, new[] { "jpg", "jpeg", "png" }))
                        {
                            return BadRequest(new { error = "Please upload PNG or JPG file only." });
                        }
                        else
                        {
                            string randomName = Path.GetRandomFileName() + "." + fileExt;
                            string fileDir = Path.Combine(_options.AssetsRootDirectory, @"web");
                            if (_fileService.CheckAndCreateDirectory(fileDir))
                            {
                                var fileName = Path.Combine(fileDir, randomName);

                                Stream stream = formFile.OpenReadStream();
                                /*
                                using (var imagedata = System.Drawing.Image.FromStream(stream))
                                {
                                    _fileService.ResizeImage(imagedata, imagedata.Width, imagedata.Height, fileName, fileExt);
                                }*/
                                _fileService.SaveFromStream(stream, fileName);
                                stream.Dispose();

                                image.Name = formFile.FileName;
                                image.Filename = randomName;
                                image.FileType = fileExt;
                            }
                            else
                            {
                                return BadRequest(new { error = "Error in creating directory" });
                            }

                        }
                    }
                }
            }

            if(request.MobileImage != null)
            {
                foreach (IFormFile mobileFile in request.MobileImage)
                {
                    if (mobileFile.Length > 0)
                    {
                        var fileExt = System.IO.Path.GetExtension(mobileFile.FileName).Substring(1).ToLower();
                        if (!_fileService.checkFileExtension(fileExt, new[] { "jpg", "jpeg", "png" }))
                        {
                            return BadRequest(new { error = "Please upload PNG or JPG file only." });
                        }
                        else
                        {
                            string randomName = Path.GetRandomFileName() + "." + fileExt;
                            string fileDir = Path.Combine(_options.AssetsRootDirectory, @"web");
                            if (_fileService.CheckAndCreateDirectory(fileDir))
                            {
                                image.MobileName = mobileFile.FileName;
                                image.MobileFilename = randomName;
                                image.MobileFileType = fileExt;
                                _context.Entry(image).State = EntityState.Modified;
                                await _context.SaveChangesAsync();

                                var fileName = Path.Combine(fileDir, randomName);

                                Stream stream = mobileFile.OpenReadStream();
                                /*
                                using (var imagedata = System.Drawing.Image.FromStream(stream))
                                {
                                    _fileService.ResizeImage(imagedata, imagedata.Width, imagedata.Height, fileName, fileExt);
                                }
                                */
                                _fileService.SaveFromStream(stream, fileName);
                                stream.Dispose();
                            }
                            else
                            {
                                return BadRequest(new { error = "Error in creating directory" });
                            }

                        }
                    }
                }

            }

            try
            {
                _context.Entry(image).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch
            {
                return BadRequest(new { error = "Error updating database." });
            }
            return NoContent();
        }


        /**
         * @api {get} /Event/banner/publish/{bannerId}/{publish}/{userId} Mem-publish banner  
         * @apiVersion 1.0.0
         * @apiName PublishBanner
         * @apiGroup Event
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} bannerId      Id dari banner yang ingin di-publish
         * @apiParam {Number} publish       1 untuk publish, 0 untuk tidak mem-publish         
         * @apiParam {Number} userId        userId dari user yang login
         * 
         * @apiSuccessExample Success-Response:        
         * {
         *     "bannerId": 1,
         *     "filename": "Background Webinar 2.jpg",
         *     "url": "http://localhost/assets/web/mayl1ux5.oeo.jpg",
         *     "link": "https://www.gmlperformance.com",
         *     "title": "judul",
         *     "description": "deskripsi"
         * }
         * 
         * @apiError NotFound    bannerId salah
         * 
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("banner/publish/{bannerId}/{publish}/{userId}")]
        public async Task<ActionResult<WebBannerResponse>> PublishBanner(int bannerId, int publish, int userId)
        {
            WebImage image = _context.WebImages.Where(a => a.BannerId == bannerId).FirstOrDefault();
            if (image == null)
            {
                return NotFound(new { error = "Banner not found. Please check bannerId." });
            }

            DateTime now = DateTime.Now;
            image.Publish = publish == 1;
            image.LastUpdated = now;
            image.LastUpdatedBy = userId;

            _context.Entry(image).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            WebBannerResponse response = new WebBannerResponse();
            response.BannerId = bannerId;
            response.Filename = image.Name;
            response.URL = _options.AssetsBaseURL + "web/" + image.Filename;
            response.Publish = image.Publish;
            response.Link = image.Link;
            response.Title = image.Title;
            response.Description = image.Description;

            return response;
        }

        /**
         * @api {get} /Event/banner/up/{bannerId}/{move}/{userId} GET move banner  
         * @apiVersion 1.0.0
         * @apiName MoveBanner
         * @apiGroup Event
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} bannerId      Id dari banner yang ingin di-publish
         * @apiParam {Number} move          1 untuk naik, -1 untuk turun         
         * @apiParam {Number} userId        userId dari user yang login
         * 
         * @apiSuccessExample Success-Response:        
         *   NoContent
         * 
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("banner/up/{bannerId}/{move}/{userId}")]
        public async Task<ActionResult> MoveBanner(int bannerId, int move, int userId)
        {
            DateTime now = DateTime.Now;


            if (move >= 1)
            {
                int curImageid = bannerId;
                int nextImageId = 0;
                for (int i = 0; i < move; i++)
                {
                    WebImage image = _context.WebImages.Where(a => a.BannerId == curImageid).FirstOrDefault();
                    if (image != null)
                    {
                        if (_context.WebImages.Where(a => a.BannerId < curImageid && a.BannerId > 0).Count() > 0)
                        {
                            nextImageId = _context.WebImages.Where(a => a.BannerId < curImageid && a.BannerId > 0).Max(a => a.BannerId);
                            WebImage nextImage = _context.WebImages.Where(a => a.BannerId == nextImageId).FirstOrDefault();
                            if (nextImage != null)
                            {
                                await SwitchBannerId(image, nextImage, now, userId);
                            }
                        }
                    }
                    curImageid = nextImageId;
                }
            }
            else if (move <= -1)
            {
                int curImageid = bannerId;
                int nextImageId = 0;
                for (int i = 0; i > move; i--)
                {
                    WebImage image = _context.WebImages.Where(a => a.BannerId == curImageid).FirstOrDefault();
                    if (image != null)
                    {
                        if (_context.WebImages.Where(a => a.BannerId > curImageid).Count() > 0)
                        {
                            nextImageId = _context.WebImages.Where(a => a.BannerId > curImageid).Min(a => a.BannerId);
                            WebImage nextImage = _context.WebImages.Where(a => a.BannerId == nextImageId).FirstOrDefault();
                            if (nextImage != null)
                            {
                                await SwitchBannerId(image, nextImage, now, userId);
                            }
                        }
                    }
                    curImageid = nextImageId;
                }
            }

            return NoContent();
        }

        /**
         * @api {delete} /event/banner/{bannerId}/{userId} DELETE banner
         * @apiVersion 1.0.0
         * @apiName DeleteBanner
         * @apiGroup Event
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} bannerId   Id dari image yang ingin dihapus
         * @apiParam {Number} userId    Id dari user yang login
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "id": 1,
         *       "name": "Background Webinar 2.jpg",
         *       "filename": "mayl1ux5.oeo.jpg",
         *       "fileType": "jpg",
         *       "bannerId": 1,
         *       "publish": true,
         *       "createdDate": "2020-08-31T16:07:46.1861448",
         *       "createdBy": 1,
         *       "lastUpdated": "2020-08-31T16:29:22.1948748",
         *       "lastUpdatedBy": 1,
         *       "isDeleted": true,
         *       "deletedBy": 1,
         *       "deletedDate": "2020-09-01T10:12:04.1197704+07:00"
         *   }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpDelete("banner/{bannerId}/{userId}")]
        public async Task<ActionResult<WebImage>> DeleteBanner(int bannerId, int userId)
        {
            WebImage image = _context.WebImages.Where(a => a.BannerId == bannerId && !a.IsDeleted).FirstOrDefault();
            if (image == null || image.Id == 0)
            {
                return NotFound();
            }

            DateTime now = DateTime.Now;
            image.DeletedBy = userId;
            image.DeletedDate = now;
            image.IsDeleted = true;
            _context.Entry(image).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return image;
        }

        /**
         * @api {put} /event/image/{id}      PUT image untuk event
         * @apiVersion 1.0.0
         * @apiName PutEventImage
         * @apiGroup Event
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} id              id dari image yang ingin diupdate. Harus sama dengan yang ada di URL 
         * @apiParam {String} caption         Caption atau deskripsi untuk image ini
         * @apiParam {Number} eventId         Id dari event untuk image ini
         * @apiParam {File} image             File image yang ingin diupload
         * @apiParam {Number} publish         0 untuk draft, 1 untuk publish
         * @apiParam {Number} userId          Id dari user yang login
         * 
         * @apiSuccessExample Success-Response:
         * {
         *     "id": 34,
         *     "caption": "Ini caption untuk image ini",
         *     "imageURL": "http://localhost/assets/events/4/ow0owstm.sqq.jpg",
         *     "eventId": 4,
         *     "errors": []
         * }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPut("image/{id}")]
        public async Task<ActionResult<WebEventImageResponse>> PutEventImage(int id, [FromForm] WebEventImageRequest request)
        {
            DateTime now = DateTime.Now;

            WebEventImageResponse response = new WebEventImageResponse();

            if (request.EventId == 0)
            {
                response.Errors.Add(new Error("event", "Event id cannot be null or 0."));
            }
            else if (!EventExists(request.EventId))
            {
                response.Errors.Add(new Error("event", "Invalid event id."));
            }
            else if (id != request.Id)
            {
                return BadRequest();
            }
            else
            {
                WebEventImage curImage = _context.WebEventImages.Find(id);
                if (curImage == null || curImage.Id == 0)
                {
                    return NotFound();
                }

                curImage.DocumentationId = request.Publish == 0 ? IMAGE_DRAFT : IMAGE_PUBLISH;
                curImage.EventId = request.EventId;
                curImage.LastUpdated = now;
                curImage.LastUpdatedBy = request.UserId;
                _context.Entry(curImage).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                if (request.Image == null)
                {
                    string str = request.Caption.Trim();
                    WebEventImageCaption curCaption = _context.WebEventImageCaptions.Where(a => a.EventImageId == curImage.Id).FirstOrDefault();
                    if (curCaption != null && curCaption.Id != 0)
                    {
                        curCaption.Caption = str;
                        curCaption.LastUpdated = now;
                        curCaption.LastUpdatedBy = request.UserId;

                        _context.Entry(curCaption).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                    }
                    else if (!str.Equals(""))
                    {
                        WebEventImageCaption caption = new WebEventImageCaption()
                        {
                            EventImageId = id,
                            Caption = str,
                            CreatedDate = now,
                            CreatedBy = request.UserId,
                            LastUpdated = now,
                            LastUpdatedBy = request.UserId,
                            IsDeleted = false,
                            DeletedBy = 0
                        };
                        _context.WebEventImageCaptions.Add(caption);
                        await _context.SaveChangesAsync();

                    }
                    response.Id = id;
                    response.EventId = request.EventId;
                    response.Caption = str;
                    response.ImageURL = getAssetsUrl(request.EventId, curImage.Filename, "");
                    response.Publish = request.Publish;
                }
                else
                {
                    foreach (IFormFile formFile in request.Image)
                    {
                        if (formFile.Length > 0)
                        {
                            var fileExt = System.IO.Path.GetExtension(formFile.FileName).Substring(1).ToLower();
                            if (!_fileService.checkFileExtension(fileExt, new[] { "jpg", "jpeg", "png" }))
                            {
                                response.Errors.Add(new Error("extension", "Please upload PNG or JPG file only."));
                                return response;
                            }
                            else
                            {

                                string randomName = Path.GetRandomFileName() + "." + fileExt;
                                string fileDir = Path.Combine(_options.AssetsRootDirectory, @"events", request.EventId.ToString());
                                if (_fileService.CheckAndCreateDirectory(fileDir))
                                {
                                    var fileName = Path.Combine(fileDir, randomName);

                                    Stream stream = formFile.OpenReadStream();
                                    using (var imagedata = System.Drawing.Image.FromStream(stream))
                                    {
                                        _fileService.ResizeImage(imagedata, imagedata.Width, imagedata.Height, fileName, fileExt);
                                    }
                                    stream.Dispose();

                                    int docId = request.Publish == 0 ? IMAGE_DRAFT : IMAGE_PUBLISH;

                                    curImage.Name = formFile.FileName;
                                    curImage.Filename = randomName;
                                    curImage.FileType = fileExt;

                                    _context.Entry(curImage).State = EntityState.Modified;
                                    await _context.SaveChangesAsync();

                                    string str = request.Caption.Trim();

                                    WebEventImageCaption curCaption = _context.WebEventImageCaptions.Where(a => a.EventImageId == curImage.Id).FirstOrDefault();
                                    if (curCaption != null && curCaption.Id != 0)
                                    {
                                        curCaption.Caption = str;
                                        curCaption.LastUpdated = now;
                                        curCaption.LastUpdatedBy = request.UserId;

                                        _context.Entry(curCaption).State = EntityState.Modified;
                                        await _context.SaveChangesAsync();
                                    }
                                    else if (!str.Equals(""))
                                    {
                                        WebEventImageCaption caption = new WebEventImageCaption()
                                        {
                                            EventImageId = id,
                                            Caption = str,
                                            CreatedDate = now,
                                            CreatedBy = request.UserId,
                                            LastUpdated = now,
                                            LastUpdatedBy = request.UserId,
                                            IsDeleted = false,
                                            DeletedBy = 0
                                        };
                                        _context.WebEventImageCaptions.Add(caption);
                                        await _context.SaveChangesAsync();

                                    }
                                    response.Id = id;
                                    response.EventId = request.EventId;
                                    response.Caption = str;
                                    response.ImageURL = getAssetsUrl(request.EventId, randomName, "");
                                    response.Publish = request.Publish;
                                }
                                else
                                {
                                    response.Errors.Add(new Error("directory", "Error in creating directory."));
                                }

                            }
                        }
                    }

                }

            }

            return response;
        }

        /**
         * @api {delete} /event/{eventId}/{userId} DELETE event
         * @apiVersion 1.0.0
         * @apiName DeleteEvent
         * @apiGroup Event
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} eventId   Id dari event yang ingin dihapus
         * @apiParam {Number} userId    Id dari user yang login
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "id": 34,
         *       "title": "Corporate University",
         *       "intro": "Corpu membahas perkembangan terakhir dalam dunia corporate university khususnya di Indonesia. ",
         *       "categoryId": 2,
         *       "fromDate": "0001-01-01T00:00:00",
         *       "toDate": "2020-07-06T00:00:00",
         *       "startTime": "null",
         *       "endTime": "null",
         *       "audience": "HR, Chief Learning Officer",
         *       "address": "Hotel Mandarin",
         *       "publish": false,
         *       "addInfo": "",
         *       "createdDate": "2020-04-09T15:32:29.2405462",
         *       "createdBy": 3,
         *       "lastUpdated": "2020-04-09T15:32:29.2405462",
         *       "lastUpdatedBy": 3,
         *       "isDeleted": true,
         *       "deletedBy": 3,
         *       "deletedDate": "2020-04-13T19:26:23.0481129+07:00"
         *   }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpDelete("{eventId}/{userId}")]
        public async Task<ActionResult<WebEvent>> DeleteEvent(int eventId, int userId)
        {
            var eve = await _context.WebEvents.FindAsync(eventId);
            if (eve == null)
            {
                return NotFound();
            }

            DateTime now = DateTime.Now;

            eve.IsDeleted = true;
            eve.DeletedBy = userId;
            eve.DeletedDate = now;
            _context.Entry(eve).State = EntityState.Modified;

            // cdhx qubisa delete

            string url = "https://qubisaapi.azurewebsites.net/";

            HttpClient httpClient = new HttpClient
            {
                BaseAddress = new Uri(url)
            };
            
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "cXViaXNhYXBpOmt5SF9xenc5alQzanJodUo0NDk/OE5kSjdrJUJFWVMjRS1iUmdZZWQhR2NlXmpDYzVOUVJuTFZVVEh4X14tQiUqeG1MeTVGN05yWlZwbmtOdzZ6eW1wU0w2a0BOX0d3V0VUR2doY3FMNSolQlI/TEJKRkZyJUx3Yng2VWZDanRK");

            CdhxQubisaDelete payload = new CdhxQubisaDelete()
            {
                contentId = eventId,
                isDeleted = true,
            };
            
            HttpResponseMessage httpResponseMessage = await httpClient.PostAsJsonAsync("instructor/v1/Webinar", payload).ConfigureAwait(false);
            Console.WriteLine(payload);

            await _context.SaveChangesAsync();

            return eve;
        }

        /**
         * @api {delete} /event/image/{imageId}/{userId} DELETE image
         * @apiVersion 1.0.0
         * @apiName DeleteEventImage
         * @apiGroup Event
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} imageId   Id dari image yang ingin dihapus
         * @apiParam {Number} userId    Id dari user yang login
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "id": 31,
         *       "name": "5practices.5d4112f5.jpg",
         *       "filename": "imn3dier.2fw.jpg",
         *       "fileType": "jpg",
         *       "eventId": 4,
         *       "descriptionId": 0,
         *       "frameworkId": 0,
         *       "documentationId": 1,
         *       "testimonyId": 0,
         *       "thumbnailId": 0,
         *       "createdDate": "2020-04-13T17:58:23.8691942",
         *       "createdBy": 3,
         *       "lastUpdated": "2020-04-13T17:58:23.8691942",
         *       "lastUpdatedBy": 3,
         *       "isDeleted": true,
         *       "deletedBy": 3,
         *       "deletedDate": "2020-04-13T19:34:07.2286479+07:00"
         *   }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpDelete("image/{imageId}/{userId}")]
        public async Task<ActionResult<WebEventImage>> DeleteEventImage(int imageId, int userId)
        {
            var eve = await _context.WebEventImages.FindAsync(imageId);
            if (eve == null)
            {
                return NotFound();
            }

            DateTime now = DateTime.Now;

            eve.IsDeleted = true;
            eve.DeletedBy = userId;
            eve.DeletedDate = now;
            _context.Entry(eve).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return eve;
        }


        /**
         * @api {get} /event/imageslug/{slug}/{publish} GET list event images pakai slug
         * @apiVersion 1.0.0
         * @apiName GetEventImagesBySlug
         * @apiGroup Event
         * @apiPermission Basic authentication
         * 
         * @apiParam {string} slug            slug dari event yang bersangkutan 
         * @apiParam {Number} publish         0 untuk draft, 1 untuk publish 
         * 
         * @apiSuccessExample Success-Response:
         * [
         *     {
         *         "id": 32,
         *         "caption": "Ini caption untuk image ini",
         *         "imageURL": "http://localhost/assets/events/4/qzqxext0.y4p.jpg",
         *         "eventId": 4,
         *         "Publish": 0,
         *         "errors": []
         *     }
         * ]
         */
        [AllowAnonymous]
        [HttpGet("imageslug/{slug}/{publish}")]
        public async Task<ActionResult<List<WebEventImageResponse>>> GetEventImagesBySlug(string slug, int publish)
        {
            WebEvent e = _context.WebEvents.Where(a => !a.IsDeleted && a.Slug.Equals(slug.Trim())).FirstOrDefault();
            if (e == null)
            {
                return NotFound();
            }

            return await GetEventImagesById(e.Id, publish);
        }


        /**
         * @api {get} /event/eventimages/{eventId}/{publish} GET list images dari event
         * @apiVersion 1.0.0
         * @apiName GetEventImagesById
         * @apiGroup Event
         * @apiPermission Basic authentication
         * 
         * @apiParam {Number} eventId         id dari event yang bersangkutan 
         * @apiParam {Number} publish         0 untuk draft, 1 untuk publish 
         * 
         * @apiSuccessExample Success-Response:
         * [
         *     {
         *         "id": 32,
         *         "caption": "Ini caption untuk image ini",
         *         "imageURL": "http://localhost/assets/events/4/qzqxext0.y4p.jpg",
         *         "eventId": 4,
         *         "Publish": 0,
         *         "errors": []
         *     }
         * ]
         */
        [AllowAnonymous]
        [HttpGet("eventimages/{eventId}/{publish}")]
        public async Task<ActionResult<List<WebEventImageResponse>>> GetEventImagesById(int eventId, int publish)
        {
            int docId = publish == 0 ? IMAGE_DRAFT : IMAGE_PUBLISH;

            if (!EventExists(eventId))
            {
                return NotFound();
            }

            IQueryable<WebEventImageResponse> query = from image in _context.WebEventImages
                                                      join caption in _context.WebEventImageCaptions
                                                      on image.Id equals caption.EventImageId
                                                      where !image.IsDeleted && image.DocumentationId == docId && image.EventId == eventId
                                                      select new WebEventImageResponse()
                                                      {
                                                          Id = image.Id,
                                                          Caption = caption.Caption,
                                                          ImageURL = getAssetsUrl(image.EventId, image.Filename, ""),
                                                          EventId = image.EventId,
                                                          Publish = image.DocumentationId == IMAGE_DRAFT ? 0 : 1
                                                      };

            return await query.ToListAsync();
        }

        /**
         * @api {get} /event/public/{fromMonth}/{toMonth}/{branchfilter} GET public workshops
         * @apiVersion 1.0.0
         * @apiName GetPublicWorkshops
         * @apiGroup Event
         * @apiPermission Basic Authentication
         * 
         * @apiParam {String} fromMonth             Filter untuk bulan, dalam format YYYYMM, misal 202005 untuk bulan Mei 2020. Gunakan 0 untuk tidak menggunakan filter bulan.
         * @apiParam {String} toMonth               Filter untuk bulan, dalam format YYYYMM, misal 202007 untuk bulan Juli 2020. Gunakan 0 untuk tidak menggunakan filter bulan.
         * @apiParam {String} branchfilter          0 untuk tidak menggunakan filter, atau comma-separated values dari branchId, misal 1,3.
         * 
         * @apiSuccessExample Success-Response:
         *   [
         *       {
         *           "branch": {
         *               "id": 1,
         *               "text": "Jakarta"
         *           },
         *           "items": [
         *               {
         *                   "workshopId": 14,
         *                   "eventId": 18,
         *                   "title": "Certified Branch Manager Development Program",
         *                   "categoryId": 1,
         *                   "mgrUp": true,
         *                   "mgr": true,
         *                   "spv": false,
         *                   "tl": false,
         *                   "staff": false,
         *                   "dates": [
         *                       [
         *                           {
         *                               "id": 7,
         *                               "text": "21-24"
         *                           }
         *                       ]
         *                   ]
         *               },
         *               {
         *                   "workshopId": 44,
         *                   "eventId": 54,
         *                   "title": "Managing Task and Team For Supervisors",
         *                   "categoryId": 1,
         *                   "mgrUp": false,
         *                   "mgr": false,
         *                   "spv": true,
         *                   "tl": true,
         *                   "staff": false,
         *                   "dates": [
         *                       [
         *                           {
         *                               "id": 7,
         *                               "text": "6-7"
         *                           }
         *                       ]
         *                   ]
         *               }
         *           ]
         *       },
         *       {
         *           "branch": {
         *               "id": 2,
         *               "text": "Medan"
         *           },
         *           "items": []
         *       },
         *       {
         *           "branch": {
         *               "id": 3,
         *               "text": "Surabaya"
         *           },
         *           "items": []
         *       },
         *       {
         *           "branch": {
         *               "id": 4,
         *               "text": "Makassar"
         *           },
         *           "items": []
         *       }
         *   ]
         * 
         */
        [AllowAnonymous]
        [HttpGet("public/{fromMonth}/{toMonth}/{branchfilter}")]
        public async Task<ActionResult<List<PublicWorkshopResponse>>> GetPublicWorkshops(string fromMonth, string toMonth, string branchfilter)
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            authHeader = authHeader.Trim();
            if (authHeader.Equals(""))
            {
                return Unauthorized();
            }

            string encodedCredentials = authHeader.Substring(6);
            var credentialBytes = Convert.FromBase64String(encodedCredentials);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
            var username = credentials[0];
            var password = credentials[1];
            if (username != "onegmlapi" || password != "O1n6e0G4M7L")
            {
                return Unauthorized();
            }

            int fromYear = 0;
            int toYear = 0;
            try
            {
                fromYear = Convert.ToInt32(fromMonth.Substring(0, 4));
                toYear = Convert.ToInt32(toMonth.Substring(0, 4));
            }
            catch
            {
                return BadRequest(new { error = "Error converting from month and to month" });
            }

            if (fromYear != toYear)
            {
                return BadRequest(new { error = "From month and to month must in in the same year" });
            }

            List<PublicWorkshopResponse> response = new List<PublicWorkshopResponse>();
            if (branchfilter.Equals("0"))
            {
                IQueryable<GenericInfo> query;

                if (fromYear == 2020)
                {
                    query = from branch in _context.CrmBranches
                            where !branch.IsDeleted
                            select new GenericInfo()
                            {
                                Id = branch.Id,
                                Text = branch.Branch
                            };
                }
                else
                {
                    query = from branch in _context.WebEventLocations
                            where !branch.IsDeleted
                            select new GenericInfo()
                            {
                                Id = branch.Id,
                                Text = branch.Location
                            };
                }

                List<GenericInfo> branches = await query.ToListAsync();
                foreach (GenericInfo b in branches)
                {
                    PublicWorkshopResponse r = new PublicWorkshopResponse();
                    r.Branch = new GenericInfo();
                    r.Branch.Id = b.Id;
                    r.Branch.Text = b.Text;
                    r.Items = await GetPublicWorkshopsByBranch(fromMonth, toMonth, b.Id);

                    response.Add(r);
                }
            }
            else
            {
                foreach (string s in branchfilter.Split(","))
                {
                    try
                    {
                        int n = Int32.Parse(s);
                        if (fromYear == 2020)
                        {
                            CrmBranch branch = _context.CrmBranches.Find(n);
                            if (branch != null && branch.Id > 0)
                            {
                                PublicWorkshopResponse r = new PublicWorkshopResponse();
                                r.Branch = new GenericInfo();
                                r.Branch.Id = branch.Id;
                                r.Branch.Text = branch.Branch;
                                r.Items = await GetPublicWorkshopsByBranch(fromMonth, toMonth, branch.Id);

                                response.Add(r);
                            }
                        }
                        else
                        {
                            WebEventLocation location = _context.WebEventLocations.Find(n);
                            if (location != null && location.Id > 0)
                            {
                                PublicWorkshopResponse r = new PublicWorkshopResponse();
                                r.Branch = new GenericInfo();
                                r.Branch.Id = location.Id;
                                r.Branch.Text = location.Location;
                                r.Items = await GetPublicWorkshopsByBranch(fromMonth, toMonth, location.Id);

                                response.Add(r);
                            }

                        }
                    }
                    catch
                    {
                        return BadRequest(new { error = "Branch filter error" });
                    };
                }

            }


            return response;

        }

        /**
         * @api {get} /event/list/alumni/{eventId}/{page}/{perPage}/{search} GET list alumni event
         * @apiVersion 1.0.0
         * @apiName GetEventAlumniList
         * @apiGroup Event
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} eventId               Id dari event yang diinginkan.
         * @apiParam {Number} page                  Halaman yang ditampilkan.
         * @apiParam {Number} perPage               Jumlah data per halaman.
         * @apiParam {String} search                * untuk tidak menggunakan search, atau kata yang dicari.
         * 
         * @apiSuccessExample Success-Response:
         * {
         *     "items": [
         *         {
         *             "id": 38404,
         *             "name": "Ina Mardianna",
         *             "company": "Gunting, PT",
         *             "department": "Pemasaran",
         *             "jobTitle": "Manager",
         *             "email": "ina@gunting.com",
         *             "phone": "0819"
         *         },
         *         {
         *             "id": 38405,
         *             "name": "Ita Martianna",
         *             "company": "Gunting, PT",
         *             "department": "Pemasaran",
         *             "jobTitle": "Staff",
         *             "email": "ita@gunting.com",
         *             "phone": "0819"
         *         }
         *     ],
         *     "info": {
         *         "page": 1,
         *         "perPage": 2,
         *         "total": 15
         *     }
         * }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("list/alumni/{eventId}/{page}/{perPage}/{search}")]
        public async Task<ActionResult<EventAlumniResponse>> GetEventAlumniList(int eventId, int page, int perPage, string search)
        {
            Func<CrmContact, bool> AlumniPredicate = u => {
                return search.Trim().Equals("*") || u.Name.ToLower().Contains(search.Trim().ToLower());
            };

            int roleId = GetRole("participant");

            var query = from alumni in _context.WebEventParticipants
                        join contact in _context.CrmContacts on alumni.ContactId equals contact.Id
                        join client in _context.CrmClients on contact.CrmClientId equals client.Id
                        where !contact.IsDeleted && alumni.EventId == eventId && alumni.RoleId == roleId && AlumniPredicate(contact)
                        select new EventAlumniItem()
                        {
                            Id = contact.Id,
                            Name = contact.Name,
                            Company = client.Company,
                            Department = contact.Department,
                            JobTitle = contact.Position,
                            Email = contact.Email1,
                            Phone = contact.Phone1
                        };

            int total = query.Count();

            EventAlumniResponse response = new EventAlumniResponse();
            response.Info = new PaginationInfo(page, perPage, total);

            response.Items = await query.Skip(perPage * (page - 1)).Take(perPage).ToListAsync<EventAlumniItem>();

            return response;
        }

        /**
         * @api {get} /event/alumni/{id} GET detail alumni 
         * @apiVersion 1.0.0
         * @apiName GetEventAlumni
         * @apiGroup Event
         * @apiPermission ApiUser
         * 
         * @apiSuccessExample Success-Response:
         * {
         *     "id": 37994,
         *     "name": "Dewi Kupri",
         *     "company": {
         *         "id": 14335,
         *         "text": "GUNTING, PT"
         *     },
         *     "department": "HR",
         *     "jobTitle": "Learning System Development Officer",
         *     "email": "kupri@gmail.com",
         *     "phone": "0888 8989 8989",
         *     "events": [
         *         {
         *             "id": 255,
         *             "text": "GML Hackathon 2021 part 4"
         *         }
         *     ]
         * }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("alumni/{id}")]
        public async Task<ActionResult<DetailAlumniResponse>> GetEventAlumni(int id)
        {
            if (!_context.CrmContacts.Where(a => a.Id == id && !a.IsDeleted).Any()) return NotFound();

            // id is Contact.Id
            DetailAlumniResponse response = new DetailAlumniResponse();

            int roleId = GetRole("participant");

            var query = from contact in _context.CrmContacts 
                        join client in _context.CrmClients on contact.CrmClientId equals client.Id
                        where !contact.IsDeleted && contact.Id == id  
                        select new 
                        {
                            Id = contact.Id,
                            Name = contact.Name,
                            Company = client.Company,
                            CompanyId = client.Id,
                            Department = contact.Department,
                            JobTitle = contact.Position,
                            Email = contact.Email1,
                            Phone = contact.Phone1
                        };
            var obj = query.FirstOrDefault();

            response.Id = obj.Id;
            response.Name = obj.Name;
            response.Company = new GenericInfo()
            {
                Id = obj.CompanyId,
                Text = obj.Company
            };
            response.Department = obj.Department;
            response.JobTitle = obj.JobTitle;
            response.Email = obj.Email;
            response.Phone = obj.Phone;

            var q = from alumni in _context.WebEventParticipants
                    join e in _context.WebEvents on alumni.EventId equals e.Id
                    where alumni.ContactId == id
                    select new GenericInfo()
                    {
                        Id = e.Id,
                        Text = e.Title
                    };
            response.Events = await q.ToListAsync();

            return response;
        }

        /**
         * @api {post} /event/expert POST expert
         * @apiVersion 1.0.0
         * @apiName PostExpert
         * @apiGroup Event
         * @apiPermission ApiUser
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "id": 0,
         *     "eventId": 256,
         *     "name": "Adi Ahli",
         *     "email": "adiahli@gmail.com"
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   {
         *       "id": 1,
         *       "eventId": 256,
         *       "name": "Adi Ahli",
         *       "email": "adiahli@gmail.com"
         *   }
         * 
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost("expert")]
        public async Task<ActionResult<WebEventExpert>> PostExpert(EventExpertItem expert)
        {
            WebEvent we = _context.WebEvents.Where(a => a.Id == expert.EventId && !a.IsDeleted).FirstOrDefault();
            if (we == null) return NotFound();

            RegisterUserRequest ur = new RegisterUserRequest()
            {
                FirstName = expert.Name,
                LastName = "",
                Email = expert.Email,
                UserName = expert.Email,
                Password = _clientService.GetPassword(expert.Name)
            };

            HttpResponseMessage ac = await _clientService.GetQuBisaAccess(_options.QuBisaAPIBaseURL, "/admin/access", _options.QuBisaBasicUsername, _options.QuBisaBasicPassword, _options.QuBisaAPIUsername, _options.QuBisaAPIPassword);

            QuBisaAccessResponse access = await ac.Content.ReadAsAsync<QuBisaAccessResponse>().ConfigureAwait(false);
            if (access != null)
            {
                RegisterForumRequest payload = new RegisterForumRequest()
                {
                    UserId = access.Id,
                    ChannelId = _options.ChannelId,
                    ForumName = we.Title,
                    Description = "",
                    Users = new List<RegisterUserRequest>(),
                    Experts = new List<RegisterUserRequest>(new[] { ur })
                };
                HttpResponseMessage message = await _clientService.RegisterUserToQuBisa(_options.QuBisaAPIBaseURL, "/forum/register", access.AccessToken.Token, payload);
            }

            WebEventExpert expert1 = await AddExpert(expert.EventId, expert.Name.Trim(), expert.Email.Trim().ToLower());

            string title = @"Selamat Bergabung Instruktur CDHX";
            string email = @"<p>Halo <strong>Bapak/Ibu [%NAMA%]</strong></p><p>Bapak/Ibu telah didaftarkan sebagai instruktur dalam <strong>[%COURSENAME%]</strong> yang berlangsung pada tanggal [%COURSEDATE%].</p><p>Sebagai tindak lanjut dari program tersebut, kami mengundang Bapak/Ibu untuk ikut serta dalam forum diskusi online, di mana Bapak/Ibu dapat berinteraksi dengan para instruktur dan rekan-rekan peserta lain.</p><p>Forum ini adalah hasil kerja sama dengan QuBisa, sebuah platform pembelajaran online terbaik di Indonesia. Berikut adalah link, username, dan password untuk ikut serta dalam forum tersebut.</p><p><strong>Account Details</strong></p>" + "<figure class=\"table\">" + @"<table><tbody><tr><td>Link</td><td>[%LINK%]</td></tr><tr><td>Email</td><td>[%EMAIL%]</td></tr><tr><td>Password</td><td>[%PASSWORD%]</td></tr></tbody></table></figure><p>Password di atas dapat diganti setelah login di Forum CDHX ini. Harap dicatat bahwa jika sebelumnya sudah terdaftar sebagai pengguna QuBisa, maka Bapak/Ibu dapat mengakses forum ini dengan menggunakan username dan password yang digunakan saat ini.</p><p><strong>Contact</strong></p><p>Untuk informasi dan pertanyaan, silahkan menghubungi gml@gmlperformance.co.id atau lewat pesan singkat Whatsapp ke nomor 0821-2325-3700.</p><p>Atas nama GML Performance Consulting kami ucapkan terima kasih.</p>";
            email = RemovePlaceholders(we, ur, email);

            SendEmail(ur.FirstName, ur.Email, title, email);

            return expert1;
        }

        
        /**
         * @api {put} /event/expert/{id} PUT expert
         * @apiVersion 1.0.0
         * @apiName PutExpert
         * @apiGroup Event
         * @apiPermission ApiUser
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "id": 1,
         *     "eventId": 256,
         *     "name": "Adi Ahli",
         *     "email": "adiahli@gmail.com"
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   {
         *       "id": 1,
         *       "eventId": 256,
         *       "name": "Adi Ahli",
         *       "email": "adiahli@gmail.com"
         *   }
         * 
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPut("expert/{id}")]
        public async Task<ActionResult<WebEventExpert>> PutExpert(int id, EventExpertItem expert)
        {
            if (id != expert.Id) return BadRequest(new { error = "Id tidak sesuai." });

            WebEventExpert curExpert = _context.WebEventExperts.Where(a => a.Id == id).FirstOrDefault();
            if (curExpert != null)
            {
                curExpert.Name = expert.Name;
                curExpert.Email = expert.Email;
                _context.Entry(curExpert).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return curExpert;
            }

            return NotFound();
        }

        /**
         * @api {delete} /event/expert/{id} DELETE expert
         * @apiVersion 1.0.0
         * @apiName DeleteExpert
         * @apiGroup Event
         * @apiPermission ApiUser
         */
        [Authorize(Policy = "ApiUser")]
        [HttpDelete("expert/{id}")]
        public async Task<ActionResult> DeleteExpert(int id)
        {
            WebEventExpert curExpert = _context.WebEventExperts.Where(a => a.Id == id).FirstOrDefault();
            if (curExpert != null)
            {
                _context.WebEventExperts.Remove(curExpert);
                await _context.SaveChangesAsync();

                return NoContent();
            }

            return NotFound();
        }

        /**
         * @api {get} /event/list/expert/{eventId}/{page}/{perPage}/{search} GET list expert event
         * @apiVersion 1.0.0
         * @apiName GetEventExpertList
         * @apiGroup Event
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} eventId               Id dari event yang diinginkan.
         * @apiParam {Number} page                  Halaman yang ditampilkan.
         * @apiParam {Number} perPage               Jumlah data per halaman.
         * @apiParam {String} search                * untuk tidak menggunakan search, atau kata yang dicari.
         * 
         * @apiSuccessExample Success-Response:
         * {
         *     "items": [
         *         {
         *             "id": 1,
         *             "eventId": 256,
         *             "name": "Adi Ahli",
         *             "email": "adiahli@gmail.com"
         *         }
         *     ],
         *     "info": {
         *         "page": 1,
         *         "perPage": 10,
         *         "total": 1
         *     }
         * }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("list/expert/{eventId}/{page}/{perPage}/{search}")]
        public async Task<ActionResult<EventExpertResponse>> GetEventExpertList(int eventId, int page, int perPage, string search)
        {
            Func<WebEventExpert, bool> ExpertPredicate = u => {
                return search.Trim().Equals("*") || u.Name.ToLower().Contains(search.Trim().ToLower());
            };

            var query = from expert in _context.WebEventExperts
                        where expert.EventId == eventId && ExpertPredicate(expert)
                        select new EventExpertItem()
                        {
                            Id = expert.Id,
                            EventId = expert.EventId,
                            Name = expert.Name,
                            Email = expert.Email
                        };

            int total = query.Count();

            EventExpertResponse response = new EventExpertResponse();
            response.Info = new PaginationInfo(page, perPage, total);

            response.Items = await query.Skip(perPage * (page - 1)).Take(perPage).ToListAsync<EventExpertItem>();

            return response;
        }

        /**
         * @api {get} /event/email/{eventId}/{userId} GET email
         * @apiVersion 1.0.0
         * @apiName GetAlumniEmail
         * @apiGroup Event
         * @apiPermission ApiUser
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("email/{eventId}/{userId}")]
        public ActionResult<EmailTemplate> GetAlumniEmail(int eventId, int userId)
        {
            WebEvent ev = _context.WebEvents.Where(a => a.Id == eventId && !a.IsDeleted).FirstOrDefault();
            if (ev == null) return NotFound();

            EmailTemplate response = new EmailTemplate();

            if(string.IsNullOrEmpty(ev.Email))
            {
                return new EmailTemplate()
                {
                    Id = eventId,
                    Subject = "Selamat Datang di Forum CDHX",
                    Text = @"<p>Halo <strong>Sdr/Sdri. [%NAMA%]</strong></p><p>Terima kasih atas partisipasi Anda dalam <strong>[%COURSENAME%]</strong> pada tanggal [%COURSEDATE%].</p><p>Sebagai tindak lanjut dari program tersebut, kami mengundang Anda untuk ikut serta dalam forum diskusi online, di mana Anda dapat berinteraksi dengan para instruktur dan rekan-rekan peserta lainnya, bekerja sama dengan QuBisa.</p><p>Berikut adalah link, username, dan password Anda untuk ikut serta dalam forum tersebut.</p><p><strong>Account Details</strong></p>"
                          + "<figure class=\"table\">" 
                          + @"<table><tbody><tr><td>Link</td><td>[%LINK%]</td></tr><tr><td>Email</td><td>[%EMAIL%]</td></tr><tr><td>Sandi</td><td>[%PASSWORD%]</td></tr></tbody></table></figure><p>Anda dapat memanfaatkan forum ini untuk berdiskusi dengan instruktur dan peserta lain tentang topik-topik yang berkaitan dengan program yang sudah Anda ikuti di atas mulai tanggal [%DARI%] sampai tanggal [%SAMPAI%].</p><p>Password di atas bisa Anda ganti setelah Anda login di Forum CDHX ini. Harap dicatat bahwa jika Anda sebelumnya sudah terdaftar sebagai pengguna QuBisa, maka Anda dapat mengakses forum ini dengan menggunakan username dan password yang Anda gunakan saat ini.</p><p><strong>Contact</strong></p><p>Untuk informasi dan pertanyaan, silahkan menghubungi gml@gmlperformance.co.id atau lewat pesan singkat Whatsapp ke nomor 0821-2325-3700.</p><p>Atas nama GML Performance Consulting kami ucapkan terima kasih.</p>"
                };
            }

            response.Id = eventId;
            response.Text = ev.Email;
            response.Subject = ev.EmailSubject;

            return response;
        }
        /**
         * @api {post} /event/email POST email
         * @apiVersion 1.0.0
         * @apiName PostAlumniEmail
         * @apiGroup Event
         * @apiPermission ApiUser
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "alumni": [
         *       {
         *         "eventId": 256,
         *         "roleId": 8,
         *         "contactId": 38419    
         *       }
         *     ],
         *     "email": "Selamat datang... "
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   NoContent
         * 
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost("email")]
        public async Task<ActionResult> PostAlumniEmail(EventEmail email)
        {
            if (string.IsNullOrWhiteSpace(email.EmailSubject)) email.EmailSubject = "Selamat Datang di Forum CDHX";
            if (string.IsNullOrWhiteSpace(email.Email)) return BadRequest(new { error = "Email cannot be blank." });

            bool updated = false;
            foreach (WebEventParticipant p in email.Alumni)
            {
                CrmContact contact = _context.CrmContacts.Find(p.ContactId);
                WebEvent we = _context.WebEvents.Find(p.EventId);

                if (we == null || contact == null) continue;

                if (!updated)
                {
                    we.Email = string.IsNullOrWhiteSpace(email.Email) ? "" : email.Email;
                    we.EmailSubject = string.IsNullOrWhiteSpace(email.EmailSubject) ? "" : email.EmailSubject;
                    _context.Entry(we).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                    updated = true;
                }

                SendEmail(contact.Name, contact.Email1, email.EmailSubject, RemovePlaceholders(we, contact, email.Email));
            }
/*
            foreach(NotificationRequest request in requests)
            {
                EmailMessage message = new EmailMessage();
                List<EmailAddress> senders = new List<EmailAddress>();
                senders.Add(new EmailAddress()
                {
                    Name = "GML Performance Consulting",
                    Address = "gml@gmlperformance.co.id"
                });
                message.FromAddresses = senders;
                message.ToAddresses = request.Recipients;
                message.Subject = request.EmailSubject;
                message.Content = request.Email;

                _emailService.Send(message);
            } */
            // No need to send to QuBisa
            /*            QuBisaAccessResponse access = await _clientService.GetQuBisaAccess(_options.QuBisaAPIBaseURL, "admin/access", _options.QuBisaBasicUsername, _options.QuBisaBasicPassword, _options.QuBisaAPIUsername, _options.QuBisaAPIPassword);

                        if (access != null)
                        {
                            List<NotificationRequest> requests = new List<NotificationRequest>();

                            foreach (WebEventParticipant p in email.Alumni)
                            {
                                WebEvent we = _context.WebEvents.Find(p.EventId);
                                NotificationRequest r = requests.Where(a => a.ForumName.Equals(we.Title)).FirstOrDefault();
                                if(r == null)
                                {

                                    r = new NotificationRequest()
                                    {
                                        Email = email.Email,
                                        EmailSubject = email.EmailSubject,
                                        ForumName = we.Title,
                                        EventName = "newparticipant",
                                        Recipients = new List<EmailAddress>()
                                    };

                                    requests.Add(r);
                                }

                                if(r != null)
                                {
                                    CrmContact contact = _context.CrmContacts.Find(p.ContactId);
                                    if(contact != null)
                                    {
                                        r.Recipients.Add(new EmailAddress()
                                        {
                                            Name = contact.Name,
                                            Address = contact.Email1
                                        });
                                    }
                                }
                            }
            HttpResponseMessage message = await _clientService.NotifyUsers(_options.QuBisaAPIBaseURL, "forum/notify", access.AccessToken.Token, requests);
            }
            */


            return NoContent();
        }

        [Authorize(Policy = "ApiUser")]
        [HttpGet("fnupdate")]
        public async Task<ActionResult<WebEvent>> UpdateFilename()
        {
            List<WebEventFlyer> images = await _context.WebEventFlyers.Where(a => !a.IsDeleted).ToListAsync();
            foreach(WebEventFlyer img in images)
            {
                String originalFilename = img.Filename;

                int idx = img.Name.LastIndexOf(".");
                String fn = img.Name.Substring(0, idx);
                String newFilename = GetFilename(fn, img.FileType, img.EventId);
                if(!newFilename.Equals(originalFilename))
                {
                    String fileDir = GetEventDirectory(img.EventId);
                    String fullpath = Path.Combine(fileDir, originalFilename);
                    if(System.IO.File.Exists(fullpath))
                    {
                        String newFullPath = Path.Combine(fileDir, newFilename);
                        System.IO.File.Move(fullpath, newFullPath);
                    }
                    img.Filename = newFilename;
                    _context.Entry(img).State = EntityState.Modified;
                }
            }
            await _context.SaveChangesAsync();

            return NoContent();
        }


        private string RemovePlaceholders(WebEvent we, RegisterUserRequest user, string text)
        {
            return RemovePlaceholders(we, user.FirstName, user.Email, text);
        }
        private string RemovePlaceholders(WebEvent we, CrmContact contact, string text)
        {
            return RemovePlaceholders(we, contact.Name, contact.Email1, text);
        }
        private string RemovePlaceholders(WebEvent we, string name, string email, string text)
        {
            /*
            * [%COURSENAME%]
            * [%COURSEDATE%]
            * [%LINK%]
            * [%NAMA%]
            * [%EMAIL%]
            * [%PASSWORD%]
            * [%USERNAME%]
            */

            StringBuilder builder = new StringBuilder(text);
            builder.Replace("[%COURSENAME%]", we.Title)
                .Replace("[%COURSEDATE%]", we.FromDate.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("id")))
                .Replace("[%LINK%]", _options.QuBisaForumLoginURL)
                .Replace("[%NAMA%]", name)
                .Replace("[%EMAIL%]", email)
                .Replace("[%PASSWORD%]", _clientService.GetPassword(name))
                .Replace("[%USERNAME%]", email)
                .Replace("[%DARI%]", DateTime.Now.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("id")))
                .Replace("[%SAMPAI%]", DateTime.Now.AddDays(15).ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("id")));

            return builder.ToString();
        }

        /**
         * @api {post} /event/alumni POST alumni
         * @apiVersion 1.0.0
         * @apiName PostAlumni
         * @apiGroup Event
         * @apiPermission ApiUser
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "id": 0,
         *     "clientId": 14400,
         *     "eventId": 446,
         *     "name": "Daniel",
         *     "email": "daniel@gmail.com",
         *     "phone": "0819",
         *     "department": "KMD",
         *     "jobTitle": "Head",
         *     "userId": 35
         *   }
         *   
         * @apiSuccessExample Success-Response:
         * [
         *     {
         *         "eventId": 256,
         *         "roleId": 8,
         *         "contactId": 38419
         *     }
         * ]
         * 
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost("alumni")]
        public async Task<ActionResult<List<WebEventParticipant>>> PostAlumni(EventAlumniInfo alumni)
        {
            WebEvent ev = _context.WebEvents.Where(a => a.Id == alumni.EventId).FirstOrDefault();
            if (ev == null) return NotFound();

            bool b = _context.CrmClients.Where(a => a.Id == alumni.ClientId && !a.IsDeleted).Any();
            if (!b) return NotFound();

            int roleId = GetRole("participant");
            if (roleId == 0) return NotFound();

            List<WebEventParticipant> response = new List<WebEventParticipant>();
            try
            {
                int contactId = await _clientService.GetOrCreateContact(alumni, DateTime.Now, "Forum");
                if (contactId == 0) return BadRequest(new { error = "Error saving contact to database." });

                WebEventParticipant ep = _context.WebEventParticipants.Where(a => a.EventId == alumni.EventId && a.RoleId == roleId && a.ContactId == contactId).FirstOrDefault();

                if (ep != null)
                {
                    response.Add(ep);
                }
                else
                {
                    WebEventParticipant participant = new WebEventParticipant()
                    {
                        EventId = alumni.EventId,
                        RoleId = roleId,
                        ContactId = contactId
                    };

                    _context.WebEventParticipants.Add(participant);
                    await _context.SaveChangesAsync();

                    response.Add(participant);
                }

                HttpResponseMessage ac = await _clientService.GetQuBisaAccess(_options.QuBisaAPIBaseURL, "/admin/access", _options.QuBisaBasicUsername, _options.QuBisaBasicPassword, _options.QuBisaAPIUsername, _options.QuBisaAPIPassword);

                QuBisaAccessResponse access = await ac.Content.ReadAsAsync<QuBisaAccessResponse>().ConfigureAwait(false);
                if(access != null)
                {
                    List<RegisterUserRequest> users = new List<RegisterUserRequest>();
                    users.Add(new RegisterUserRequest()
                    {
                        FirstName = alumni.Name,
                        LastName = "",
                        Email = alumni.Email,
                        UserName = alumni.Email,
                        Password = _clientService.GetPassword(alumni.Name)
                    });
                    RegisterForumRequest payload = new RegisterForumRequest()
                    {
                        UserId = access.Id,         // 45917, // 
                        ChannelId = _options.ChannelId,
                        ForumName = ev.Title,
                        Description = "",
                        Users = users, 
                        Experts = new List<RegisterUserRequest>()
                    };
                    HttpResponseMessage message = await _clientService.RegisterUserToQuBisa(_options.QuBisaAPIBaseURL, "/forum/register", access.AccessToken.Token, payload);
                }

                return response;
            }
            catch
            {
                return BadRequest(new { error = "Error saving alumni." });
            }

        }

        /**
         * @api {put} /event/alumni/{id} PUT alumni
         * @apiVersion 1.0.0
         * @apiName PutAlumni
         * @apiGroup Event
         * @apiPermission ApiUser
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "id": 0,
         *     "clientId": 14400,
         *     "eventId": 446,
         *     "name": "Daniel",
         *     "email": "daniel@gmail.com",
         *     "phone": "0819",
         *     "department": "KMD",
         *     "jobTitle": "Head",
         *     "userId": 35
         *   }
         *   
         * @apiSuccessExample Success-Response:
         * [
         *     {
         *         "eventId": 256,
         *         "roleId": 8,
         *         "contactId": 38419
         *     }
         * ]
         * 
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPut("alumni/{id}")]
        public async Task<ActionResult<List<WebEventParticipant>>> PutAlumni(int id, EventAlumniInfo alumni)
        {
            // id is contactId
            if (id != alumni.Id) return BadRequest(new { error = "Id tidak sesuai." });
            bool ab = _context.WebEvents.Where(a => a.Id == alumni.EventId).Any();
            if (!ab) return NotFound();

            bool b = _context.CrmClients.Where(a => a.Id == alumni.ClientId && !a.IsDeleted).Any();
            if (!b) return NotFound();

            int roleId = GetRole("participant");
            if (roleId == 0) return NotFound();

            try
            {
                int contactId = await _clientService.GetOrCreateContact(alumni, DateTime.Now, "Forum");
                if (contactId == 0) return BadRequest(new { error = "Error saving contact to database." });

                WebEventParticipant ep = _context.WebEventParticipants.Where(a => a.EventId == alumni.EventId && a.RoleId == roleId && a.ContactId == contactId).FirstOrDefault();

                if (ep != null) return new List<WebEventParticipant>(new[] { ep });

                WebEventParticipant participant = new WebEventParticipant()
                {
                    EventId = alumni.EventId,
                    RoleId = roleId,
                    ContactId = contactId
                };

                _context.WebEventParticipants.Add(participant);
                await _context.SaveChangesAsync();

                /*
                 // Call APi with token
                string AccessToken = lblToken.Text;

HttpClient tRequest = new HttpClient();
tRequest.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

Task<HttpResponseMessage> getTask = tRequest.PostAsJsonAsync(new Uri(strURL).ToString(), TestMaster);

HttpResponseMessage urlContents = await getTask;

Console.WriteLine("urlContents.ToString");
lblEDDR.Text = urlContents.ToString();
                 */

                /*
                string url = _options.QuBisaAPIBaseURL;

                HttpClient httpClient = new HttpClient
                {
                    BaseAddress = new Uri(url)
                };

                httpClient.DefaultRequestHeaders.Add($"Authorization", $"Basic {Base64Encode($"{_options.QuBisaBasicUsername}:{_options.QuBisaBasicPassword}")}");

                UsernamePassword payload = new UsernamePassword(_options.QuBisaAPIUsername, _options.QuBisaAPIPassword);

                HttpResponseMessage httpResponseMessage = await httpClient.PostAsJsonAsync("admin/access", payload).ConfigureAwait(false);
                QuBisaAccessResponse response = await httpResponseMessage.Content.ReadAsAsync<QuBisaAccessResponse>().ConfigureAwait(false);
                */

                return new List<WebEventParticipant>(new[] { participant });
            }
            catch
            {
                return BadRequest(new { error = "Error saving alumni." });
            }

        }

        /**
         * @api {delete} /event/alumni/{id}/{eventId} DELETE alumni
         * @apiVersion 1.0.0
         * @apiName DeleteAlumni
         * @apiGroup Event
         * @apiPermission ApiUser
         */
        [Authorize(Policy = "ApiUser")]
        [HttpDelete("alumni/{id}/{eventId}")]
        public async Task<ActionResult> DeleteAlumni(int id, int eventId)
        {
            WebEventParticipant participant = _context.WebEventParticipants.Where(a => a.ContactId == id && a.EventId == eventId).FirstOrDefault();
            if (participant != null)
            {
                _context.WebEventParticipants.Remove(participant);
                await _context.SaveChangesAsync();

                return NoContent();
            }

            return NotFound();
        }

        /**
         * @api {post} /event/upload POST upload alumni
         * @apiVersion 1.0.0
         * @apiName UploadParticipants
         * @apiGroup Event
         * @apiPermission ApiUser
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "eventId": 255,
         *     "Filename": "upload.xlsx",
         *     "FileBase64": "UEsDBBQABg...",
         *     "userId": 1,
         *     "experts": [
         *       {
         *         "name": "Rudy Hartono",
         *         "address": "rh@gmail.com"
         *       }
         *     ]
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   [
         *       {
         *           "eventId": 255,
         *           "roleId": 8,
         *           "contactId": 37994
         *       },
         *       {
         *           "eventId": 255,
         *           "roleId": 8,
         *           "contactId": 37995
         *       }
         *   ]
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost("upload")]
        public async Task<ActionResult<List<WebEventParticipant>>> UploadParticipants(EventAlumniUpload upload)
        {
            WebEvent ev = _context.WebEvents.Where(a => a.Id == upload.EventId).FirstOrDefault();
            if (ev == null) return NotFound();

            List<RegisterUserRequest> users = new List<RegisterUserRequest>();

            List<RegisterUserRequest> experts = new List<RegisterUserRequest>();
            foreach (EmailAddress expert in upload.Experts)
            {
                await AddExpert(upload.EventId, expert.Name, expert.Address);

                experts.Add(new RegisterUserRequest()
                {
                    FirstName = expert.Name,
                    LastName = "",
                    Email = expert.Address,
                    UserName = expert.Address,
                    Password = _clientService.GetPassword(expert.Name)
                });
            }

            List<WebEventParticipant> participants = new List<WebEventParticipant>();

            int roleId = GetRole("participant");
            if (roleId == 0) return NotFound();

            var fileName = Path.GetTempFileName();
            int lineNumber = 0;

            var fileExt = System.IO.Path.GetExtension(upload.Filename).Substring(1).ToLower();
            if (!_fileService.checkFileExtension(fileExt, new[] { "csv", "xls", "xlsx" }))
            {
                return BadRequest(new { error = "Please upload csv, xls, or xlsx file only." });
            }

            if(string.IsNullOrEmpty(upload.FileBase64))
            {
                return BadRequest(new { error = "File cannot be empty." });
            }

            DateTime now = DateTime.Now;

            _fileService.SaveByteArrayAsFile(fileName, upload.FileBase64);
            
            if (fileExt.Equals("csv"))
            {
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
                            int industryId = _clientService.GetOrCreateIndustry(contact, now, upload.UserId);
                            int clientId = _clientService.UpdateOrCreateClient(contact, now, upload.UserId, industryId, "Forum");
                            int contactId = _clientService.UpdateOrCreateContact(contact, now, upload.UserId, clientId, "Forum");

                            if (contactId == 0) return BadRequest(new { error = "Error saving contact to database." });

                            WebEventParticipant ep = _context.WebEventParticipants.Where(a => a.EventId == upload.EventId && a.RoleId == roleId && a.ContactId == contactId).FirstOrDefault();

                            if (ep != null) participants.Add(ep);
                            else
                            {
                                WebEventParticipant participant = new WebEventParticipant()
                                {
                                    EventId = upload.EventId,
                                    RoleId = roleId,
                                    ContactId = contactId
                                };

                                _context.WebEventParticipants.Add(participant);
                                participants.Add(participant);
                            }

                            users.Add(new RegisterUserRequest()
                            {
                                FirstName = contact.Name,
                                LastName = "",
                                Email = contact.Email1,
                                UserName = contact.Email1,
                                Password = _clientService.GetPassword(contact.Name)
                            });

                        }
                    }
                    catch
                    {
                        return BadRequest(new { error = string.Join(" ", new[] { "Error in line", lineNumber.ToString() }) });
                    }
                }
            }
            else
            {
             
                // Excel file
                try
                {
                    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                    IExcelDataReader excelReader;

                    using (var stream = System.IO.File.Open(fileName, FileMode.Open, FileAccess.Read))
                    {
                        if (fileExt.Equals("xls"))
                        {
                            excelReader = ExcelReaderFactory.CreateBinaryReader(stream);
                        }
                        else
                        {
                            // xlsx
                            excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                        }

                        var conf = new ExcelDataSetConfiguration()
                        {
                            ConfigureDataTable = a => new ExcelDataTableConfiguration
                            {
                                UseHeaderRow = true
                            }
                        };

                        DataSet dataSet = excelReader.AsDataSet(conf);
                        DataRowCollection row = dataSet.Tables[0].Rows;

                        foreach (DataRow item in row)
                        {
                            lineNumber++;
                            List<object> rowDataList = item.ItemArray.ToList(); //list of each rows

                            CsvContact contact = new CsvContact()
                            {
                                Valid = rowDataList[0].ToString(),
                                Company = rowDataList[1].ToString(),
                                Name = rowDataList[2].ToString(),
                                Salutation = rowDataList[3].ToString(),
                                Title = rowDataList[4].ToString(),
                                Department = rowDataList[5].ToString(),
                                Address1 = rowDataList[6].ToString(),
                                Address2 = rowDataList[7].ToString(),
                                Address3 = rowDataList[8].ToString(),
                                Hp1 = rowDataList[9].ToString(),
                                Hp2 = rowDataList[10].ToString(),
                                Hp3 = rowDataList[11].ToString(),
                                Phone = rowDataList[12].ToString(),
                                Fax = rowDataList[13].ToString(),
                                Email1 = rowDataList[14].ToString(),
                                Email2 = rowDataList[15].ToString(),
                                Email3 = rowDataList[16].ToString(),
                                Email4 = rowDataList[17].ToString(),
                                Website = rowDataList[18].ToString(),
                                Industry = rowDataList[19].ToString(),
                                Remarks = rowDataList[20].ToString()
                            };

                            int industryId = _clientService.GetOrCreateIndustry(contact, now, upload.UserId);
                            int clientId = _clientService.UpdateOrCreateClient(contact, now, upload.UserId, industryId, "Forum");
                            int contactId = _clientService.UpdateOrCreateContact(contact, now, upload.UserId, clientId, "Forum");

                            if (contactId == 0) return BadRequest(new { error = "Error saving contact to database." });

                            WebEventParticipant ep = _context.WebEventParticipants.Where(a => a.EventId == upload.EventId && a.RoleId == roleId && a.ContactId == contactId).FirstOrDefault();

                            if (ep != null) participants.Add(ep);
                            else
                            {
                                WebEventParticipant participant = new WebEventParticipant()
                                {
                                    EventId = upload.EventId,
                                    RoleId = roleId,
                                    ContactId = contactId
                                };

                                _context.WebEventParticipants.Add(participant);
                                participants.Add(participant);
                            }

                            users.Add(new RegisterUserRequest()
                            {
                                FirstName = contact.Name,
                                LastName = "",
                                Email = contact.Email1,
                                UserName = contact.Email1,
                                Password = _clientService.GetPassword(contact.Name)
                            });

                        }
                    }

                }
                catch
                {
                    return BadRequest(new { error = string.Join(" ", new[] { "Error in line", lineNumber.ToString() }) });
                }
                
            }
            
            System.IO.File.Delete(fileName);
            await _context.SaveChangesAsync();

            if(users.Count() > 0 || experts.Count() > 0)
            {
                HttpResponseMessage ac = await _clientService.GetQuBisaAccess(_options.QuBisaAPIBaseURL, "/admin/access", _options.QuBisaBasicUsername, _options.QuBisaBasicPassword, _options.QuBisaAPIUsername, _options.QuBisaAPIPassword);

                QuBisaAccessResponse access = await ac.Content.ReadAsAsync<QuBisaAccessResponse>().ConfigureAwait(false);
                if (access != null)
                {
                    RegisterForumRequest payload = new RegisterForumRequest()
                    {
                        UserId = access.Id,
                        ChannelId = _options.ChannelId,
                        ForumName = ev.Title,
                        Description = "",
                        Users = users,
                        Experts = experts
                    };
                    HttpResponseMessage message = await _clientService.RegisterUserToQuBisa(_options.QuBisaAPIBaseURL, "/forum/register", access.AccessToken.Token, payload);
                }
            }


            return participants;
        }

        /**
         * @api {get} /event/contact/{page}/{perPage}/{search} GET list contact
         * @apiVersion 1.0.0
         * @apiName GetEventContact
         * @apiGroup Event
         * @apiPermission Token authentication
         * 
         * @apiParam {Number} page            Halaman yang ditampilkan. 
         * @apiParam {Number} perPage         Jumlah data per halaman.  
         * @apiParam {String} search          Kata yang mau dicari di judul. * untuk semua
         * 
         * @apiSuccessExample Success-Response:
         * {
         *     "contacts": [
         *         {
         *             "id": 4072,
         *             "name": "Soekarno",
         *             "phoneNumber": "0812345678",
         *             "email": "soekarno@gmail.com",
         *             "companyName": "PT Merdeka",
         *             "jobTitle": "Analyst",
         *             "department": "Otoritas Jasa Keuangan Institute",
         *             "message": "Selamat pagi",
         *             "action": "contact-us",
         *             "voucher": "",
         *             "referenceFrom": "",
         *             "lastUpdated": "2022-09-06T09:25:30.0762559",
         *             "lastUpdatedBy": 0,
         *             "isDeleted": false,
         *             "deletedBy": 0,
         *             "deletedDate": "1970-01-01T00:00:00"
         *         },
         *         {
         *             "id": 4047,
         *             "name": "Soeharto",
         *             "phoneNumber": "0813-4567890",
         *             "email": "PT BCD",
         *             "companyName": "PT Merah Putih",
         *             "jobTitle": "Human Capital",
         *             "department": "Human Capital",
         *             "message": null,
         *             "action": "eventregister-Hustle Culture and Work Life Balance",
         *             "voucher": "",
         *             "referenceFrom": null,
         *             "lastUpdated": "2022-08-22T17:03:38.9242406",
         *             "lastUpdatedBy": 0,
         *             "isDeleted": false,
         *             "deletedBy": 0,
         *             "deletedDate": "1970-01-01T00:00:00"
         *         }
         *     ],
         *     "info": {
         *         "page": 1,
         *         "perPage": 2,
         *         "total": 10
         *     }
         * }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("contact/{page}/{perPage}/{search}")]
        public async Task<ActionResult<WebContact>> GetEventContact(int page, int perPage, string search)
        {
            Func<WebContactRegister, bool> WherePredicate = r => {
                return search.Trim().Equals("*") || r.Name.Contains(search) || r.CompanyName.Contains(search) || r.Action.Contains(search);
            };

            var query = from reg in _context.WebContactRegisters
                        where !reg.IsDeleted && WherePredicate(reg)
                        orderby reg.LastUpdated descending
                        select reg;

            int total = query.Count();
            List<WebContactRegister> list = await query.Skip(perPage * (page - 1)).Take(perPage).ToListAsync();

            return new WebContact()
            {
                Contacts = list == null ? new List<WebContactRegister>() : list,
                Info = new PaginationInfo(page, perPage, total)
            };
        }


        /**
         * @api {delete} /event/contact/{contactId}/{userId} DELETE contact
         * @apiVersion 1.0.0
         * @apiName DeleteContact
         * @apiGroup Event
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} contactId   Id dari contact yang ingin dihapus
         * @apiParam {Number} userId      Id dari user yang login
         * 
         */

        [Authorize(Policy = "ApiUser")]
        [HttpDelete("contact/{contactId}/{userId}")]
        public async Task<ActionResult<WebContactRegister>> DeleteContact(int contactId, int userId)
        {
            DateTime now = DateTime.Now;

            WebContactRegister record = _context.WebContactRegisters.Where(a => a.Id == contactId && !a.IsDeleted).FirstOrDefault();
            if (record == null) return NotFound();

            record.IsDeleted = true;
            record.DeletedDate = now;
            record.DeletedBy = userId;
            _context.Entry(record).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return record;
        }


        private async Task<WebEventExpert> AddExpert(int eventId, string name, string email)
        {
            WebEventExpert curExpert = _context.WebEventExperts.Where(a => a.EventId == eventId && a.Email.ToLower().Equals(email)).FirstOrDefault();
            if (curExpert != null)
            {
                if (!curExpert.Name.Equals(name))
                {
                    curExpert.Name = name;
                    _context.Entry(curExpert).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }

                return curExpert;
            }

            DateTime now = DateTime.Now;

            WebEventExpert newExpert = new WebEventExpert()
            {
                EventId = eventId,
                Name = name,
                Email = email
            };

            _context.WebEventExperts.Add(newExpert);
            await _context.SaveChangesAsync();

            return newExpert;

        }
        private int GetRole(string str)
        {
            return _context.KmProjectTeamRoles.Where(a => a.Shortname.Equals(str)).Select(a => a.Id).FirstOrDefault();
        }


        private async Task<Error> UpdateEventDescription(int eventId, string desc, int userId, DateTime now)
        {
            if (desc.Length > 0)
            {
                try
                {
                    WebEventDescription description = _context.WebEventDescriptions.Where<WebEventDescription>(a => a.EventId == eventId && !a.IsDeleted).FirstOrDefault();
                    if (description == null || description.Id == 0)
                    {
                        return await AddEventDescription(eventId, desc, userId, now);
                    }
                    else
                    {
                        description.Description = desc;
                        description.LastUpdated = now;
                        description.LastUpdatedBy = userId;

                        _context.Entry(description).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                    }
                }
                catch
                {
                    return new Error("description", "Error updating database for event description.");
                }
            }
            return new Error("ok", "");

        }
        private async Task<Error> AddEventDescription(int eventId, string desc, int userId, DateTime now)
        {
            if (desc.Length > 0)
            {
                try
                {
                    WebEventDescription description = new WebEventDescription()
                    {
                        EventId = eventId,
                        Description = desc,
                        CreatedDate = now,
                        CreatedBy = userId,
                        LastUpdated = now,
                        LastUpdatedBy = userId,
                        IsDeleted = false,
                        DeletedBy = 0
                    };
                    _context.WebEventDescriptions.Add(description);
                    await _context.SaveChangesAsync();

                }
                catch
                {
                    return new Error("db", "Error writing to database for event descrtiption.");
                }
            }
            return new Error("ok", "");
        }
        private async Task<Error> UpdateEventThumbnails(int eventId, string thumbnails, int userId, DateTime now, string filename)
        {
            if (thumbnails == null)
            {
                return new Error("ok", "");
            }

            if (string.IsNullOrEmpty(thumbnails)) return new Error("ok", "");

            try
            {
                List<WebEventImage> images = _context.WebEventImages.Where<WebEventImage>(a => a.EventId == eventId && a.ThumbnailId == eventId && !a.IsDeleted).ToList();

                foreach (WebEventImage image in images)
                {
                    image.IsDeleted = true;
                    _context.Entry(image).State = EntityState.Modified;
                }
                await _context.SaveChangesAsync();

            }
            catch
            {
                return new Error("thumbnail", "Error updating database for event thumbnail.");
            }

            return await AddEventThumbnails(eventId, thumbnails, userId, now, filename);

        }
        private async Task<Error> AddEventThumbnails(int eventId, string thumbnails, int userId, DateTime now, string filename)
        {
            if (thumbnails == null)
            {
                return new Error("ok", "");
            }

            if (string.IsNullOrEmpty(thumbnails)) return new Error("ok", "");

            if (thumbnails.Length > 0)
            {
                var error = SaveImage(thumbnails, eventId, filename, false);
                if (error.Code.Equals("ok"))
                {
                    string[] names = error.Description.Split(separator);
                    if (names.Length >= 3)
                    {
                        // Untuk thumbnail, ThumbnaikId == EventId
                        Error err = await SaveImageToDb(names, eventId, 0, 0, 0, 0, eventId, now, userId);
                        if (!err.Code.Equals("ok"))
                        {
                            return err;
                        }
                    }
                }
                else
                {
                    return error;
                }
            }

            return new Error("ok", "");
        }
        private async Task<Error> UpdateEventFlyers(int eventId, string flyer, int userId, DateTime now, string filename)
        {
            if (flyer == null)
            {
                return new Error("ok", "");
            }

            if (string.IsNullOrEmpty(flyer)) return new Error("ok", "");

            try
            {
                List<WebEventFlyer> brochures1 = _context.WebEventFlyers.Where<WebEventFlyer>(a => a.EventId == eventId && !a.IsDeleted).ToList();

                foreach (WebEventFlyer bro in brochures1)
                {
                    bro.IsDeleted = true;
                    _context.Entry(bro).State = EntityState.Modified;
                }
                await _context.SaveChangesAsync();

            }
            catch
            {
                return new Error("flyer", "Error updating database for event flyer.");
            }

            return await AddEventFlyers(eventId, flyer, userId, now, filename);

        }
        private async Task<Error> UpdateEventBrochures(int eventId, string brochures, int userId, DateTime now, string filename)
        {
            if (brochures == null)
            {
                return new Error("ok", "");
            }

            if (string.IsNullOrEmpty(brochures)) return new Error("ok", "");

            try
            {
                List<WebEventBrochure> brochures1 = _context.WebEventBrochures.Where<WebEventBrochure>(a => a.EventId == eventId && !a.IsDeleted).ToList();

                foreach (WebEventBrochure bro in brochures1)
                {
                    bro.IsDeleted = true;
                    _context.Entry(bro).State = EntityState.Modified;
                }
                await _context.SaveChangesAsync();

            }
            catch
            {
                return new Error("brochure", "Error updating database for event brochure.");
            }

            return await AddEventBrochures(eventId, brochures, userId, now, filename);

        }
        private async Task<Error> AddEventFlyers(int eventId, string flyer, int userId, DateTime now, string filename)
        {
            if (flyer == null)
            {
                return new Error("ok", "");
            }

            if (flyer.Length > 0)
            {
                var error = SaveImage(flyer, eventId, filename, false);
                if (error.Code.Equals("ok"))
                {
                    string[] names = error.Description.Split(separator);
                    if (names.Length >= 3)
                    {
                        WebEventFlyer webEventsFlyer = new WebEventFlyer()
                        {
                            EventId = eventId,
                            Name = names[0],
                            Filename = names[1],
                            FileType = names[2],
                            CreatedDate = now,
                            CreatedBy = userId,
                            LastUpdated = now,
                            LastUpdatedBy = userId,
                            IsDeleted = false,
                            DeletedBy = 0
                        };
                        _context.WebEventFlyers.Add(webEventsFlyer);
                        await _context.SaveChangesAsync();
                    }

                }
                else
                {
                    return error;
                }
            }

            return new Error("ok", "");
        }

        private async Task<Error> AddEventBrochures(int eventId, string brochures, int userId, DateTime now, string filename)
        {
            if (brochures == null)
            {
                return new Error("ok", "");
            }

            if (string.IsNullOrEmpty(brochures)) return new Error("ok", "");

            if (brochures.Length > 0)
            {
                var error = SaveImage(brochures, eventId, filename);
                if (error.Code.Equals("ok"))
                {
                    string[] names = error.Description.Split(separator);
                    if (names.Length >= 3)
                    {
                        WebEventBrochure webEventsBrochure = new WebEventBrochure()
                        {
                            EventId = eventId,
                            Name = names[0],
                            Filename = names[1],
                            FileType = names[2],
                            CreatedDate = now,
                            CreatedBy = userId,
                            LastUpdated = now,
                            LastUpdatedBy = userId,
                            IsDeleted = false,
                            DeletedBy = 0
                        };
                        _context.WebEventBrochures.Add(webEventsBrochure);
                        await _context.SaveChangesAsync();
                    }

                }
                else
                {
                    return error;
                }
            }

            return new Error("ok", "");
        }
        private async Task<Error> UpdateEventAgenda(int eventId, List<WebEventAgendaInfo> agenda, int userId, DateTime now)
        {
            try
            {
                List<WebEventAgenda> agendas = _context.WebEventAgendas.Where<WebEventAgenda>(a => a.EventId == eventId && !a.IsDeleted).ToList();

                foreach (WebEventAgenda ag in agendas)
                {
                    ag.IsDeleted = true;
                    _context.Entry(ag).State = EntityState.Modified;
                }
                await _context.SaveChangesAsync();

            }
            catch
            {
                return new Error("thumbnail", "Error updating database for event thumbnail.");
            }

            return await AddEventAgenda(eventId, agenda, userId, now);
        }
        private async Task<Error> AddEventAgenda(int eventId, List<WebEventAgendaInfo> agenda, int userId, DateTime now)
        {
            if (agenda == null)
            {
                return new Error("ok", "");
            }
            foreach (WebEventAgendaInfo a in agenda)
            {
                try
                {
                    WebEventAgenda webEventAgenda = new WebEventAgenda()
                    {
                        EventId = eventId,
                        Agenda = a.Description,
                        Date = a.Date,
                        StartTime = a.StartTime,
                        EndTime = a.EndTime,
                        CreatedDate = now,
                        CreatedBy = userId,
                        LastUpdated = now,
                        LastUpdatedBy = userId,
                        IsDeleted = false,
                        DeletedBy = 0
                    };
                    _context.WebEventAgendas.Add(webEventAgenda);
                    await _context.SaveChangesAsync();
                }
                catch
                {
                    return new Error("agenda", "Error updating database for event agenda.");
                }
            }

            return new Error("ok", "");

        }
        private async Task<Error> UpdateSpeakers(int eventId, List<WebEventSpeakerInfo> newSpeakers, int userId, DateTime now)
        {
            try
            {
                var qn = from se in _context.WebEventSpeakerEvents
                         join s in _context.WebEventSpeakerRecords on se.SpeakerId equals s.Id
                         where se.EventId == eventId && !s.IsDeleted
                         select new
                         {
                             se,
                             s
                         };
                var curSpeakers = await qn.ToListAsync();

                //List<WebEventSpeaker> curSpeakers = _context.WebEventSpeakers.Where<WebEventSpeaker>(a => a.EventId == eventId && !a.IsDeleted).ToList();

                foreach (var curSpeaker in curSpeakers)
                {
                    WebEventSpeakerInfo innew = newSpeakers.Where(a => a.Name.Equals(curSpeaker.s.Name)).FirstOrDefault();
                    if (innew != null && innew.Company.Equals(curSpeaker.s.Company) && innew.Title.Equals(curSpeaker.s.Title)) continue;

                    _context.WebEventSpeakerEvents.Remove(curSpeaker.se);
                }

                await _context.SaveChangesAsync();

                List<WebEventSpeakerInfo> newList = new List<WebEventSpeakerInfo>();

                List<WebEventSpeakerRecord> records = curSpeakers.Select(a => a.s).ToList();

                foreach(WebEventSpeakerInfo info in newSpeakers)
                {
                    WebEventSpeakerRecord exspeaker = records.Where(a => a.Name.Equals(info.Name.Trim()) && a.Company.Equals(info.Company.Trim()) && a.Title.Equals(info.Title.Trim()) && !a.IsDeleted).FirstOrDefault();
//                    WebEventSpeaker exspeaker = curSpeakers.Where(a => a.Name.Equals(info.Name) && !a.IsDeleted).FirstOrDefault();
                    if(exspeaker == null)
                    {
                        newList.Add(info);
                    }
                }

                return await AddEventSpeakers(eventId, newList, userId, now);

            }
            catch
            {
                return new Error("speaker", "Error updating database for event speakers.");
            }
        }
        private async Task<Error> AddEventSpeakers(int eventId, List<WebEventSpeakerInfo> speakers, int userId, DateTime now)
        {
            if (speakers == null)
            {
                return new Error("ok", "");
            }
            try
            {
                string randomFileName = "";
                string originalFileName = "";

                foreach (WebEventSpeakerInfo a in speakers)
                {
                    randomFileName = "";
                    originalFileName = "";

                    if (a.Profile != null && a.Profile.Length > 0)
                    {

                        var error = SaveImage(a.Profile, eventId, a.ProfileFilename);
                        if (error.Code.Equals("ok"))
                        {
                            string[] names = error.Description.Split(separator);
                            if (names.Length >= 3)
                            {
                                randomFileName = names[1];
                                originalFileName = names[0];
                            }
                        }
                        else
                        {
                            return error;
                        }
                    }

                    WebEventSpeakerRecord s = await AddSpeaker(a.Name, a.Company, a.Title, randomFileName, originalFileName, now, userId);
                    await AddEventSpeaker(eventId, s.Id);
                    /*
                                        WebEventSpeaker speaker = new WebEventSpeaker()
                                        {
                                            EventId = eventId,
                                            Name = a.Name,
                                            Title = a.Title,
                                            Company = a.Company,
                                            Profile = randomFileName,
                                            ProfileFilename = originalFileName,
                                            CreatedDate = now,
                                            CreatedBy = userId,
                                            LastUpdated = now,
                                            LastUpdatedBy = userId,
                                            IsDeleted = false,
                                            DeletedBy = 0
                                        };
                                        _context.WebEventSpeakers.Add(speaker);
                                        await _context.SaveChangesAsync();

                    */
                }

            }
            catch
            {
                return new Error("speaker", "Error updating database for event agenda.");
            }

            return new Error("ok", "");
        }
        private async Task<Error> UpdateEventTestimonies(int eventId, List<WebEventTestimonyInfo> testimonies, int userId, DateTime now)
        {
            if (testimonies == null)
            {
                return new Error("ok", "");
            }

            List<WebEventTestimony> curTestimonies = _context.WebEventTestimonies.Where(a => a.EventId == eventId && !a.IsDeleted).ToList();
            foreach (WebEventTestimony t in curTestimonies)
            {
                WebEventTestimonyInfo ti = testimonies.Find(a => a.Id == t.Id);
                if (ti == null)
                {
                    t.IsDeleted = true;
                    t.DeletedDate = now;
                    t.DeletedBy = userId;
                    _context.Entry(t).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }

            }

            foreach (WebEventTestimonyInfo testimonyInfo in testimonies)
            {
                if (testimonyInfo.Id == 0)
                {
                    if (testimonyInfo.Name != null && testimonyInfo.Testimony != null)
                    {
                        WebEventTestimony testimony = new WebEventTestimony()
                        {
                            EventId = eventId,
                            Testimony = testimonyInfo.Testimony,
                            Name = testimonyInfo.Name,
                            Company = testimonyInfo.Company,
                            Title = testimonyInfo.Title,
                            CreatedDate = now,
                            CreatedBy = userId,
                            LastUpdated = now,
                            LastUpdatedBy = userId,
                            IsDeleted = false,
                            DeletedBy = 0
                        };
                        _context.WebEventTestimonies.Add(testimony);
                        await _context.SaveChangesAsync();

                        if (testimonyInfo.TestimonyPhotos != null)
                        {
                            if (testimonyInfo.TestimonyPhotos.Length > 0)
                            {
                                var error = SaveImage(testimonyInfo.TestimonyPhotos, eventId, testimonyInfo.PhotoFilename);
                                if (error.Code.Equals("ok"))
                                {
                                    string[] names = error.Description.Split(separator);
                                    if (names.Length >= 3)
                                    {
                                        Error err = await SaveImageToDb(names, eventId, 0, 0, 0, testimony.Id, 0, now, userId);
                                        if (!err.Code.Equals("ok"))
                                        {
                                            return new Error("testimony_photo", "Error in updating database for testimony_photo " + testimonyInfo.PhotoFilename);
                                        }
                                    }
                                }
                                else
                                {
                                    return new Error("testimony_photo", "Error in saving testimony_photo " + testimonyInfo.PhotoFilename);
                                }
                            }
                        }

                    }

                }
                else
                {
                    WebEventTestimony testimony = _context.WebEventTestimonies.Find(testimonyInfo.Id);
                    if (testimony != null && testimony.Id > 0)
                    {
                        testimony.EventId = eventId;
                        testimony.Testimony = testimonyInfo.Testimony;
                        testimony.Name = testimonyInfo.Name;
                        testimony.Company = testimonyInfo.Company;
                        testimony.Title = testimonyInfo.Title;
                        testimony.LastUpdated = now;
                        testimony.LastUpdatedBy = userId;

                        _context.Entry(testimony).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                    }

                    if (testimonyInfo.TestimonyPhotos != null)
                    {
                        if (testimonyInfo.TestimonyPhotos.Length > 0)
                        {
                            var error = SaveImage(testimonyInfo.TestimonyPhotos, eventId, testimonyInfo.PhotoFilename);
                            if (error.Code.Equals("ok"))
                            {
                                string[] names = error.Description.Split(separator);
                                if (names.Length >= 3)
                                {
                                    Error err = await UpdateImageToDb(names, eventId, 0, 0, 0, testimony.Id, 0, now, userId);
                                    if (!err.Code.Equals("ok"))
                                    {
                                        return new Error("testimony_photo", "Error in updating database for testimony_photo " + testimonyInfo.PhotoFilename);
                                    }
                                }
                            }
                            else
                            {
                                return new Error("testimony_photo", "Error in saving testimony_photo " + testimonyInfo.PhotoFilename);
                            }
                        }
                    }


                }
            }

            return new Error("ok", "");
        }
        private async Task<Error> AddEventTestimonies(int eventId, List<WebEventTestimonyInfo> testimonies, int userId, DateTime now)
        {
            if (testimonies == null)
            {
                return new Error("ok", "");
            }
            try
            {
                foreach (WebEventTestimonyInfo testimonyInfo in testimonies)
                {
                    if (testimonyInfo.Name != null && testimonyInfo.Testimony != null)
                    {
                        WebEventTestimony testimony = new WebEventTestimony()
                        {
                            EventId = eventId,
                            Testimony = testimonyInfo.Testimony,
                            Name = testimonyInfo.Name,
                            Company = testimonyInfo.Company,
                            Title = testimonyInfo.Title,
                            CreatedDate = now,
                            CreatedBy = userId,
                            LastUpdated = now,
                            LastUpdatedBy = userId,
                            IsDeleted = false,
                            DeletedBy = 0
                        };
                        _context.WebEventTestimonies.Add(testimony);
                        await _context.SaveChangesAsync();

                        if (testimonyInfo.TestimonyPhotos != null)
                        {
                            if (testimonyInfo.TestimonyPhotos.Length > 0)
                            {
                                var error = SaveImage(testimonyInfo.TestimonyPhotos, eventId, testimonyInfo.PhotoFilename);
                                if (error.Code.Equals("ok"))
                                {
                                    string[] names = error.Description.Split(separator);
                                    if (names.Length >= 3)
                                    {
                                        Error err = await SaveImageToDb(names, eventId, 0, 0, 0, testimony.Id, 0, now, userId);
                                        if (!err.Code.Equals("ok"))
                                        {
                                            return new Error("testimony_photo", "Error in updating database for testimony_photo " + testimonyInfo.PhotoFilename);
                                        }
                                    }
                                }
                                else
                                {
                                    return new Error("testimony_photo", "Error in saving testimony_photo " + testimonyInfo.PhotoFilename);
                                }
                            }
                        }

                    }
                }

            }
            catch
            {
                return new Error("testimony", "Error in updating database for event testimony");
            }
            return new Error("ok", "");
        }
        private async Task<Error> UpdateEventTakeaways(int eventId, List<string> takeaways, int userId, DateTime now)
        {
            try
            {
                List<WebEventTakeaway> tks = _context.WebEventTakeaways.Where<WebEventTakeaway>(a => a.EventId == eventId && !a.IsDeleted).ToList();

                foreach (WebEventTakeaway tk in tks)
                {
                    tk.IsDeleted = true;
                    _context.Entry(tk).State = EntityState.Modified;
                }
                await _context.SaveChangesAsync();

            }
            catch
            {
                return new Error("thumbnail", "Error updating database for event thumbnail.");
            }

            return await AddEventTakeaways(eventId, takeaways, userId, now);
        }
        private async Task<Error> UpdateEventInvestments(int eventId, List<WebEventInvestmentInfo> investments, int userId, DateTime now)
        {
            try
            {
                List<WebEventInvestment> invests = _context.WebEventInvestments.Where<WebEventInvestment>(a => a.EventId == eventId && !a.IsDeleted).ToList();

                foreach (WebEventInvestment invest in invests)
                {
                    invest.IsDeleted = true;
                    _context.Entry(invest).State = EntityState.Modified;
                }
                await _context.SaveChangesAsync();

            }
            catch
            {
                return new Error("thumbnail", "Error updating database for event thumbnail.");
            }

            return await AddEventInvestments(eventId, investments, userId, now);
        }
        private async Task<Error> AddEventTakeaways(int eventId, List<string> takeaways, int userId, DateTime now)
        {
            if (takeaways == null || takeaways.Count() == 0)
            {
                return new Error("ok", "");
            }
            try
            {
                foreach (string takeaway in takeaways)
                {
                    WebEventTakeaway tk = new WebEventTakeaway()
                    {
                        EventId = eventId,
                        Takeaway = takeaway,
                        CreatedDate = now,
                        CreatedBy = userId,
                        LastUpdated = now,
                        LastUpdatedBy = userId,
                        IsDeleted = false,
                        DeletedBy = 0
                    };
                    _context.WebEventTakeaways.Add(tk);
                }

                await _context.SaveChangesAsync();

            }
            catch
            {
                return new Error("takeaway", "Error updating database for event takeaways.");
            }

            return new Error("ok", "");
        }
        private async Task<Error> UpdateEventNotification(int eventId, bool emailNotification, string subject, string email, DateTime now, int userId)
        {
            return await AddEventNotification(eventId, emailNotification, subject, email, now, userId);
        }
        private async Task<Error> AddEventNotification(int eventId, bool emailNotification, string subject, string email, DateTime now, int userId)
        {
            if (string.IsNullOrEmpty(email))
            {
                return new Error("ok", "");
            }
            try
            {
                WebEventNotification curNotification = _context.WebEventNotifications.Where(a => a.EventId == eventId && !a.IsDeleted).FirstOrDefault();
                if(curNotification != null)
                {
                    if(curNotification.EmailNotification != emailNotification || !curNotification.Email.Equals(email.Trim()) || !curNotification.EmailSubject.Equals(subject.Trim()))
                    {
                        curNotification.EmailNotification = emailNotification;
                        curNotification.EmailSubject = subject;
                        curNotification.Email = email;
                        curNotification.LastUpdated = now;
                        curNotification.LastUpdatedBy = userId;
                        _context.Entry(curNotification).State = EntityState.Modified;
                    }
                }
                else
                {
                    WebEventNotification notification = new WebEventNotification()
                    {
                        EventId = eventId,
                        EmailSubject = subject,
                        EmailNotification = emailNotification,
                        Email = email,
                        CreatedDate = now,
                        CreatedBy = userId,
                        LastUpdated = now,
                        LastUpdatedBy = userId,
                        IsDeleted = false,
                        DeletedBy = 0
                    };
                    _context.WebEventNotifications.Add(notification);
                }
                await _context.SaveChangesAsync();
            }
            catch
            {
                return new Error("takeaway", "Error updating database for event takeaways.");
            }

            return new Error("ok", "");
        }
        private async Task<Error> AddEventInvestments(int eventId, List<WebEventInvestmentInfo> investments, int userId, DateTime now)
        {
            if (investments == null)
            {
                return new Error("ok", "");
            }
            try
            {
                foreach (WebEventInvestmentInfo info in investments)
                {
                    //   info.ppnpercent = info.ppnpercent == null ? 0 : info.ppnpercent;
                    //   info.Nominal = info.Nominal == null ? 0 : info.Nominal;
                    info.paymenturl = info.paymenturl == null ? "" : info.paymenturl;
                    WebEventInvestment investment = new WebEventInvestment()
                    {
                        EventId = eventId,
                        Title = info.Title,
                        Description = info.Type,
                        Nominal = info.Nominal,
                        PPN = info.ppn,
                        PPNPercent = info.ppnpercent,
                        PaymentUrl = info.paymenturl,
                        CreatedDate = now,
                        CreatedBy = userId,
                        LastUpdated = now,
                        LastUpdatedBy = userId,
                        IsDeleted = false,
                        DeletedBy = 0
                    };
                    _context.WebEventInvestments.Add(investment);
                }

                await _context.SaveChangesAsync();

            }
            catch
            {
                return new Error("investment", "Error updating database for event investment.");
            }

            return new Error("ok", "");
        }
        private bool EventExists(int id)
        {
            return _context.WebEvents.Any(e => e.Id == id);
        }

        private bool RegistrationExists(int id)
        {
            return _context.WebEventRegistrations.Any(e => e.Id == id);
        }

        private WebEvent GetWebEventById(int id)
        {
            return _context.WebEvents.Where(a => a.Id == id && !a.IsDeleted).FirstOrDefault();
        }

        private WebEventVisitors GetVisitorById(int id)
        {
            return _context.WebEventVisitors.Where(a => a.Id == id).FirstOrDefault();
        }

        private GenericInfo GetEventTopic(int topicId)
        {
            var query = from loc in _context.WebTopicCategories
                        where loc.Id == topicId && !loc.IsDeleted
                        select new GenericInfo()
                        {
                            Id = loc.Id,
                            Text = loc.Category
                        };
            return query.FirstOrDefault();

        }
        private GenericInfo GetEventLocation(int locationId)
        {
            var query = from loc in _context.WebEventLocations
                        where loc.Id == locationId && !loc.IsDeleted
                        select new GenericInfo()
                        {
                            Id = loc.Id,
                            Text = loc.Location
                        };
            return query.FirstOrDefault();

        }
        private GenericInfo GetEventCategory(int categoryId)
        {
            var query = from category in _context.WebEventCategories
                        where category.Id == categoryId && !category.IsDeleted
                        select new GenericInfo()
                        {
                            Id = category.Id,
                            Text = category.Category
                        };
            return query.FirstOrDefault();

        }

        private string GetEventDescription(int eventId)
        {
            WebEventDescription description = _context.WebEventDescriptions.Where(a => a.EventId == eventId && !a.IsDeleted).FirstOrDefault();
            if(description != null && description.Id > 0)
            {
                return description.Description;
            }
            return null;
        }
        private WebEventBrochureInfo GetEventThumbnails(int eventId)
        {
            WebEventImage image = _context.WebEventImages.Where<WebEventImage>(a => a.EventId == eventId && a.ThumbnailId == eventId && !a.IsDeleted).FirstOrDefault();

            WebEventBrochureInfo response = new WebEventBrochureInfo();
            if(image != null && image.Id > 0)
            {
                response.Id = image.Id;
                response.Name = image.Name;
                response.Url = getAssetsUrl(image.EventId, image.Filename, "");
            }

            return response;
        }
        private WebEventBrochureInfo GetWebEventFlyers(int eventId)
        {
            WebEventFlyer flyer = _context.WebEventFlyers.Where<WebEventFlyer>(a => a.EventId == eventId && !a.IsDeleted).FirstOrDefault();
            WebEventBrochureInfo response = new WebEventBrochureInfo();

            if (flyer != null && flyer.Id > 0)
            {
                response.Id = flyer.Id;
                response.Name = flyer.Name;
                response.Url = getAssetsUrl(flyer.EventId, flyer.Filename, "");
            }

            return response;
        }
        private WebEventBrochureInfo GetWebEventBrochures(int eventId)
        {
            WebEventBrochure brochure = _context.WebEventBrochures.Where<WebEventBrochure>(a => a.EventId == eventId && !a.IsDeleted).FirstOrDefault();
            WebEventBrochureInfo response = new WebEventBrochureInfo();

            if (brochure != null && brochure.Id > 0)
            {
                response.Id = brochure.Id;
                response.Name = brochure.Name;
                response.Url = getAssetsUrl(brochure.EventId, brochure.Filename, "");
            }

            return response;
        }
        private List<WebEventAgendaInfo> GetWebEventAgendas(int eventId)
        {
            var query = from agenda in _context.WebEventAgendas
                        where agenda.EventId == eventId && !agenda.IsDeleted
                        select new WebEventAgendaInfo()
                        {
                            Id = agenda.Id,
                            Date = agenda.Date,
                            StartTime = agenda.StartTime,
                            EndTime = agenda.EndTime,
                            Description = agenda.Agenda
                        };

            return query.ToList<WebEventAgendaInfo>();
        }
        private List<WebEventSpeakerInfo> GetWebEventSpeakers(int eventId)
        {
            var qn = from se in _context.WebEventSpeakerEvents
                     join s in _context.WebEventSpeakerRecords on se.SpeakerId equals s.Id
                     where se.EventId == eventId && !s.IsDeleted
                     select new WebEventSpeakerInfo()
                     {
                         Id = s.Id,
                         Name = s.Name,
                         Company = s.Company,
                         Title = s.Title,
                         Profile = string.IsNullOrEmpty(s.Profile) ? "" : getAssetsUrl(eventId, "", s.Profile),
                         ProfileFilename = s.ProfileFilename
                     };
            return qn.ToList();
            /*            
                                 var query = from speaker in _context.WebEventSpeakers
                                    where speaker.EventId == eventId && !speaker.IsDeleted
                                    select new WebEventSpeakerInfo()
                                    {
                                        Id = speaker.Id,
                                        Name = speaker.Name,
                                        Company = speaker.Company,
                                        Title = speaker.Title,
                                        Profile = "",
                                        ProfileFilename = getAssetsUrl(eventId, speaker.Profile)
                                    };

                        return query.ToList<WebEventSpeakerInfo>();

            */
        }
        private List<WebEventTestimonyResponse> GetWebEventTestimonies(int eventId)
        {
            List<WebEventTestimony> testimonies = _context.WebEventTestimonies.Where(a => a.EventId == eventId && !a.IsDeleted).ToList();
            List<WebEventTestimonyResponse> response = new List<WebEventTestimonyResponse>();
            foreach(WebEventTestimony testimony in testimonies)
            {
                WebEventTestimonyResponse r = new WebEventTestimonyResponse()
                {
                    Id = testimony.Id,
                    Name = testimony.Name,
                    Title = testimony.Title,
                    Company = testimony.Company,
                    Testimony = testimony.Testimony,
                    Photo = GetTestimonyPhotos(eventId, testimony.Id)
                };
                response.Add(r);
            }
            return response;
        }
        private WebEventBrochureInfo GetTestimonyPhotos(int eventId, int testimonyId)
        {
            WebEventImage image = _context.WebEventImages.Where(a => a.EventId == eventId && a.TestimonyId == testimonyId && !a.IsDeleted).FirstOrDefault();
            WebEventBrochureInfo response = new WebEventBrochureInfo();
            if(image != null && image.Id > 0)
            {
                response.Id = image.Id;
                response.Name = image.Name;
                response.Url = getAssetsUrl(image.EventId, image.Filename, "");
            }

            return response;
        }
        private List<string> GetWebEventTakeaways(int eventId)
        {
            var query = from tk in _context.WebEventTakeaways
                        where tk.EventId == eventId && !tk.IsDeleted
                        select tk.Takeaway;

            return query.ToList();
        }
        private List<WebEventInvestmentInfo> GetWebEventInvestments(int eventId)
        {
            var query = from investment in _context.WebEventInvestments
                        where investment.EventId == eventId && !investment.IsDeleted
                        select new WebEventInvestmentInfo()
                        {
                            Id = investment.Id,
                            Title = investment.Title,
                            Type = investment.Description,
                            Nominal = investment.Nominal,
                            ppn = investment.PPN,
                            ppnpercent = investment.PPNPercent,
                            paymenturl = investment.PaymentUrl
                        };

            return query.ToList();
        }
        private async Task<Error> UpdateImageToDb(string[] names, int eventId, int descriptionId, int frameworkId, int documentationId, int testimonyId, int thumbnailId, DateTime now, int userId)
        {
            if (names.Length < 3) return new Error("error", "Filename error.");

            try
            {
                WebEventImage image = _context.WebEventImages.Where(a => a.EventId == eventId && a.DescriptionId == descriptionId && a.FrameworkId == frameworkId && a.DocumentationId == documentationId && a.TestimonyId == testimonyId && a.ThumbnailId == thumbnailId).FirstOrDefault();
                if(image != null && image.Id > 0)
                {
                    image.Name = names[0];
                    image.Filename = names[1];
                    image.FileType = names[2];
                    image.LastUpdated = now;
                    image.LastUpdatedBy = userId;

                    _context.Entry(image).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }

                return new Error("ok", image.Id.ToString());
            }
            catch
            {
                return new Error("error", "Error writing to database.");
            }
        }
        private async Task<Error> SaveImageToDb(string[] names, int eventId, int descriptionId, int frameworkId, int documentationId, int testimonyId, int thumbnailId, DateTime now, int userId)
        {
            if (names.Length < 3) return new Error("error", "Filename error.");

            try
            {
                WebEventImage image = new WebEventImage()
                {
                    Name = names[0],
                    Filename = names[1],
                    FileType = names[2],
                    EventId = eventId,
                    DescriptionId = descriptionId,
                    FrameworkId = frameworkId,
                    DocumentationId = documentationId,
                    TestimonyId = testimonyId,
                    ThumbnailId = thumbnailId,
                    CreatedDate = now,
                    CreatedBy = userId,
                    LastUpdated = now,
                    LastUpdatedBy = userId,
                    IsDeleted = false,
                    DeletedBy = 0
                };
                _context.WebEventImages.Add(image);
                await _context.SaveChangesAsync();

                return new Error("ok", image.Id.ToString());
            }
            catch
            {
                return new Error("error", "Error writing to database.");
            }
        }

        private async Task<Error> UpdateSignature(string name, int registrationId)
        {
            try
            {
                WebEventRegistration registration = _context.WebEventRegistrations.Where<WebEventRegistration>(a => a.Id == registrationId && !a.IsDeleted).FirstOrDefault();
                registration.Signature = name;

                _context.Entry(registration).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch
            {
                return new Error("description", "Error updating database for event description.");
            }
            return new Error("ok", "");
        }

        private Error SaveImage(string base64String, int eventId, string name, bool randomFilename = true, bool speaker = false)
        {
            int n = 0;
            string fileExt = "jpg";
            ImageFormat format = System.Drawing.Imaging.ImageFormat.Jpeg;

            if (base64String.StartsWith("data:image/jpeg;base64,"))
            {
                n = 23;
            }
            else if (base64String.StartsWith("data:image/png;base64,"))
            {
                n = 22;
                format = System.Drawing.Imaging.ImageFormat.Png;
                fileExt = "png";
            }
            else if (base64String.StartsWith("data:application/pdf;base64,"))
            {
                n = 28;
                fileExt = "pdf";
            }
            if (n != 0)
            {
                try
                {
                    base64String = base64String.Substring(n);

                    string randomName = Path.GetRandomFileName() + "." + fileExt;
                    if(!randomFilename)
                    {
                        int idx = name.LastIndexOf(".");
                        String fn = name.Substring(0, idx);

                        randomName = GetFilename(fn, fileExt, eventId);
                    }
                    string fileDir;
                    if(speaker)
                    {
                        fileDir = GetSpeakerDirectory();
                    }
                    else if (eventId == 0)
                    {
                        fileDir = GetWebDirectory();
                    }
                    else
                    {
                        fileDir = GetEventDirectory(eventId);
                    }
                    if (_fileService.CheckAndCreateDirectory(fileDir))
                    {
                        var fileName = Path.Combine(fileDir, randomName);
                        if(fileExt.Equals("pdf"))
                        {
                            _fileService.SaveByteAsFile(fileName, base64String);
                        }
                        else
                        {
                            _fileService.SaveByteArrayAsImage(fileName, base64String, format);
                        }

                        string fname = name.EndsWith(fileExt) ? name : string.Join(".", new[] { name, fileExt });
                        return new Error("ok", string.Join(separator, new[] { fname, randomName, fileExt }));
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
            else
            {
                return new Error("extension", "Please upload PNG, JPG, or PDF file only.");
            }

        }

        private String GetFilename(String filename, String fileType, int eventId)
        {
            String newFilename = filename.Trim().Replace("  ", "_").Replace(" ", "_").Replace("-", "_");
            int count = 1;
            int n = 1;
            do
            {
                String fullname = newFilename + "." + fileType;
                count = _context.WebEventImages.Where(a => !a.IsDeleted && a.Filename.Equals(fullname) && a.EventId == eventId).Count();
                if(count > 0)
                {
                    newFilename = newFilename + "_" + n.ToString();
                    n++;
                }
            } while (count > 0);

            return newFilename + "." + fileType;
        }

        private Error SaveImage(IFormFile formFile, string[] exts, int eventId, int imageWidth = 0, int imageHeight = 0)
        {
            var fileExt = System.IO.Path.GetExtension(formFile.FileName).Substring(1).ToLower();
            if (!_fileService.checkFileExtension(fileExt, exts))
            {
                return new Error("extension", "Please upload PNG or JPG file only for file " + formFile.FileName);
            }

            try
            {
                string randomName = Path.GetRandomFileName() + "." + fileExt;

                string fileDir = GetEventDirectory(eventId);
                if (_fileService.CheckAndCreateDirectory(fileDir))
                {
                    var fileName = Path.Combine(fileDir, randomName);

                    Stream stream = formFile.OpenReadStream();
                    using (var imagedata = System.Drawing.Image.FromStream(stream))
                    {
                        if ((imagedata.Width > imageWidth || imagedata.Height > imageHeight) && imageWidth > 0 && imageHeight > 0)
                        {
                            _fileService.ResizeImage(imagedata, imageWidth, imageHeight, fileName, fileExt);
                        }
                        else
                        {
                            _fileService.ResizeImage(imagedata, imagedata.Width, imagedata.Height, randomName, fileExt);
                        }
                    }
                    stream.Dispose();

                    return new Error("ok", string.Join(separator, new[] { formFile.FileName, fileName, fileExt }));

                }
                else
                {
                    return new Error("directory", "Error in creating directory " + fileDir);
                }
            }
            catch
            {
                return new Error("error", "Error in saving file " + formFile.FileName);
            }
            
        }
        private Error SaveFile(IFormFile formFile, int eventId)
        {
            try
            {
                var fileExt = System.IO.Path.GetExtension(formFile.FileName).Substring(1).ToLower();
                string randomName = Path.GetRandomFileName() + "." + fileExt;

                string fileDir = GetEventDirectory(eventId);
                if (_fileService.CheckAndCreateDirectory(fileDir))
                {
                    var fileName = Path.Combine(fileDir, randomName);

                    Stream stream = formFile.OpenReadStream();
                    _fileService.CopyStream(stream, fileName);
                    stream.Dispose();

                    return new Error("ok", string.Join(separator, new[] { formFile.FileName, fileName, fileExt }));
                }
                else
                {
                    return new Error("directory", "Error in creating directory " + fileDir);
                }
            }
            catch
            {
                return new Error("error", "Error in saving file " + formFile.FileName);

            }
        }
        private string GetWebDirectory()
        {
            return Path.Combine(_options.AssetsRootDirectory, @"web");
        }

        private string GetEventDirectory(int eventId)
        {
            return Path.Combine(_options.AssetsRootDirectory, @"events", eventId.ToString());
        }

        private string GetSpeakerDirectory()
        {
            return Path.Combine(_options.AssetsRootDirectory, @"speakers");
        }

        private string getAssetsUrl(int eventId, string filename, string profileRandomName)
        {
            // if (string.IsNullOrEmpty(filename)) return "";

            // Get URL of assets other than profile picture of speakers
            if(string.IsNullOrEmpty(profileRandomName))
                return _options.AssetsBaseURL + "events/" + eventId.ToString() + "/" + filename;

            // Get URL profile pictures of speakers
            if(!string.IsNullOrEmpty(profileRandomName))
            {
                string fullpath = Path.Combine(_options.AssetsRootDirectory, "events", eventId.ToString(), profileRandomName);
                if (System.IO.File.Exists(fullpath))
                    return _options.AssetsBaseURL + "events/" + eventId.ToString() + "/" + profileRandomName;

                fullpath = Path.Combine(_options.AssetsRootDirectory, "speakers", profileRandomName);
                if (System.IO.File.Exists(fullpath))
                    return _options.AssetsBaseURL + "speakers/" + profileRandomName;
            }

            return "";
        }

        private string getAssetsRegistrationsUrl(int registrationId, string filename)
        {
            return _options.AssetsBaseURL + "registrations/" + registrationId.ToString() + "/" + filename;
        }

        private async Task<List<PublicWorkshopItem>> GetPublicWorkshopsByBranch(string fromMonth, string toMonth, int branchId)
        {
            DateTime now = DateTime.Now;

            string whereFromMonth = "'20200101'";
            string whereToMonth = "'20201231'";

            try
            {
                if (!fromMonth.Trim().Equals("0"))
                {
                    whereFromMonth = string.Join("", new[] { "'", fromMonth, "01", "'" });
                }
                if (!toMonth.Trim().Equals("0"))
                {
                    string year = toMonth.Substring(0, 4);
                    string month = toMonth.Substring(4);

                    int nDay = DateTime.DaysInMonth(Int32.Parse(year), Int32.Parse(month));
                    whereToMonth = string.Join("", new[] { "'", toMonth, nDay.ToString(), "'" });
                }
            }
            catch
            {
                return null;
            }

            int fromYear = 0;
            int toYear = 0;
            try
            {
                fromYear = Convert.ToInt32(whereFromMonth.Substring(1, 4));
                toYear = Convert.ToInt32(whereToMonth.Substring(1, 4));
            }
            catch
            {
                return null;
            }

            if(fromYear == toYear)
            {
                if(fromYear == 2020)
                {
                    string selectSql = "SELECT DISTINCT workshop.Id as WorkshopId, workshop.Title, workshop.CategoryId, workshop.MgrUp, workshop.Mgr, workshop.Spv, workshop.Tl, workshop.Staff, event.Id as EventId ";
                    string fromSql = "FROM dbo.WebPublicWorkshops AS workshop";
                    string joinSql0 = "JOIN dbo.WebPublicWorkshopEvents AS event ON event.WorkshopId = workshop.Id";
                    string joinSql0b = "JOIN dbo.WebPublicWorkshopEventDates AS eventDate ON eventDate.EventId = event.Id";
                    string orderBy = "ORDER BY workshop.CategoryId, workshop.Title";

                    string whereMonthClause = string.Join("", new[] { "(eventDate.StartDate >= ", whereFromMonth, " AND eventDate.StartDate <= ", whereToMonth, ")" }); // "deal.DealDate BETWEEN { ts '2008-12-20 00:00:00'} AND { ts '2008-12-20 23:59:59'}";
                    List<string> wheres = new List<string>();
                    wheres.Add(string.Join(" ", new[] { "WHERE", whereMonthClause, "AND workshop.IsDeleted = 0", "AND event.BranchId =", branchId.ToString() }));

                    string whereSql = string.Join(" ", wheres);
                    string sql = string.Join(" ", new[] { selectSql, fromSql, joinSql0, joinSql0b, whereSql, orderBy });

                    List<PublicWorkshopItem> items = await _context.PublicWorkshopItems.FromSql(sql).ToListAsync<PublicWorkshopItem>();
                    PublicWorkshopItem lastItem = null;
                    List<PublicWorkshopItem> response = new List<PublicWorkshopItem>();

                    foreach (PublicWorkshopItem item in items)
                    {
                        var query = from dt in _context.WebPublicWorkshopEventDates
                                    where dt.EventId == item.EventId
                                    select new GenericInfo()
                                    {
                                        Id = dt.PeriodId,
                                        Text = string.Join("-", new[] { dt.StartDate.Day.ToString(), dt.EndDate.Day.ToString() })
                                    };
                        List<GenericInfo> l = await query.ToListAsync();

                        if (lastItem == null)
                        {
                            item.Dates = new List<List<GenericInfo>>();
                            item.Dates.Add(l);
                            response.Add(item);
                            lastItem = item;
                        }
                        else if (item.WorkshopId == lastItem.WorkshopId)
                        {
                            PublicWorkshopItem ci = response.Last();
                            ci.Dates.Add(l);
                        }
                        else
                        {
                            item.Dates = new List<List<GenericInfo>>();
                            item.Dates.Add(l);
                            response.Add(item);
                            lastItem = item;
                        }
                    }


                    return response;
                }
                else
                {
                    DateTime fromDate = DateTime.ParseExact(whereFromMonth.Substring(1, 8), "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
                    DateTime toDate = DateTime.ParseExact(whereToMonth.Substring(1, 8), "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);

                    List<PublicWorkshopItem> response = new List<PublicWorkshopItem>();

                    List<string> titles = await _context.WebEvents.Where(a => a.FromDate >= fromDate && a.FromDate <= toDate && !a.IsDeleted && a.Publish && a.LocationId == branchId).Select(a => a.Title).Distinct().ToListAsync();
                    foreach(string title in titles)
                    {
                        PublicWorkshopItem item = new PublicWorkshopItem()
                        {
                            WorkshopId = 0,
                            EventId = 0,
                            Title = title,
                            CategoryId = 0,
                            MgrUp = true,
                            Mgr = true,
                            Spv = true,
                            Tl = true,
                            Staff = true,
                            Dates = new List<List<GenericInfo>>()
                        };

                        var query = from a in _context.WebEvents
                                    where a.Title.Equals(title.Trim()) && a.FromDate >= fromDate && a.FromDate <= toDate && !a.IsDeleted && a.Publish && a.LocationId == branchId
                                    select new GenericInfo()
                                    {
                                        Id = a.FromDate.Month,
                                        Text = a.FromDate.Day.Equals(a.ToDate.Day) ? a.FromDate.Day.ToString() : string.Join("-", new[] { a.FromDate.Day.ToString(), a.ToDate.Day.ToString() })
                                    };

                        item.Dates.Add(await query.ToListAsync());
                        response.Add(item);
                    }

                    return response;
                }
            }

            // From dan To harus berada pada tahun yang sama
            return null;

        }

        private async Task<int> SwitchBannerId(WebImage image1, WebImage image2, DateTime now, int userId)
        {
            int t = image1.BannerId;

            image1.BannerId = image2.BannerId;
            image1.LastUpdated = now;
            image1.LastUpdatedBy = userId;

            image2.BannerId = t;
            image2.LastUpdated = now;
            image2.LastUpdatedBy = userId;

            _context.Entry(image1).State = EntityState.Modified;
            _context.Entry(image2).State = EntityState.Modified;

            return await _context.SaveChangesAsync();

        }

        private async Task<int> UpdateWebCaption(string shortname, string caption, int userId, DateTime now)
        {
            WebText t1 = _context.WebTexts.Where(a => a.Shortname.Equals(shortname) && !a.IsDeleted).FirstOrDefault();
            if (t1 == null)
            {
                WebText t = new WebText()
                {
                    Shortname = shortname,
                    Caption = caption,
                    Publish = true,
                    CreatedDate = now,
                    CreatedBy = userId,
                    LastUpdated = now,
                    LastUpdatedBy = userId,
                    IsDeleted = false
                };
                _context.WebTexts.Add(t);
            }
            else
            {
                t1.Caption = caption;
                t1.LastUpdated = now;
                t1.LastUpdatedBy = userId;

                _context.Entry(t1).State = EntityState.Modified;
            }
            return await _context.SaveChangesAsync();
        }

        private async Task<int> UpdateVisitor()
        {
            int currentvisitor = GetVisitorById(1).Visitor;
            int addVisitor = (currentvisitor + 1);
            WebEventVisitors t1 = _context.WebEventVisitors.Where(a => a.Id == 1).FirstOrDefault();
            
            t1.Visitor = addVisitor;
            
            _context.Entry(t1).State = EntityState.Modified;
            return await _context.SaveChangesAsync();
        }

        private async Task<GetBannerResponse> GetBannerByCategory(int publish, int page, int perPage, string category)
        {
            GetBannerResponse response1 = new GetBannerResponse();

            page = page <= 0 ? 1 : page;
            perPage = perPage <= 0 ? 5 : perPage;
            int total = 0;
            List<WebImage> images;
            if (publish == 2)
            {
                total = _context.WebImages.Where(a => !a.IsDeleted && a.Category.Equals(category)).Count();
                images = await _context.WebImages.Where(a => !a.IsDeleted && a.Category.Equals(category)).OrderBy(a => a.BannerId).Skip(perPage * (page - 1)).Take(perPage).ToListAsync();
            }
            else
            {
                total = _context.WebImages.Where(a => a.Publish == (publish == 1) && !a.IsDeleted && a.Category.Equals(category)).Count();
                images = await _context.WebImages.Where(a => a.Publish == (publish == 1) && !a.IsDeleted && a.Category.Equals(category)).OrderBy(a => a.BannerId).Skip(perPage * (page - 1)).Take(perPage).ToListAsync();
            }

            response1.info = new PaginationInfo(page, perPage, total);
            response1.banners = new List<WebBannerResponse>();

            foreach (WebImage image in images)
            {
                WebBannerResponse response = new WebBannerResponse();
                response.BannerId = image.BannerId;
                response.Filename = image.Name;
                response.URL = _options.AssetsBaseURL + "web/" + image.Filename;
                response.MobileFilename = image.MobileName;
                response.MobileURL = _options.AssetsBaseURL + "web/" + image.MobileFilename;
                response.Publish = image.Publish;
                response.Link = image.Link;
                response.Title = image.Title;
                response.Description = image.Description;
                response.CreatedDate = image.CreatedDate.ToString();
                response.LastUpdated = image.LastUpdated.ToString();
                response1.banners.Add(response);
            }

            return response1;
        }

        private void SendEmail(string name, string email, string title, string content)
        {
            EmailMessage message = new EmailMessage();
            List<EmailAddress> senders = new List<EmailAddress>();
            senders.Add(new EmailAddress()
            {
                Name = "GML Performance Consulting",
                Address = "gml@gmlperformance.co.id"
            });

            List<EmailAddress> recipients = new List<EmailAddress>();

            recipients.Add(new EmailAddress()
            {
                Name = name,
                Address = email.Trim()
            });

            message.FromAddresses = senders;
            message.ToAddresses = recipients;
            message.Subject = title;
            message.Content = content;

            _emailService.Send(message);
        }

        private string GenerateSlug(string str)
        {
            Regex rgx = new Regex("[^a-zA-Z0-9 -]");
            str = rgx.Replace(str, "");

            Regex w = new Regex("[- ]+");
            str = w.Replace(str, "-").ToLower();

            if (str.Length > 90) str = str.Substring(0, 90);

            int n = 1;
            string ori = str;
            while(_context.WebEventCdhxCategories.Where(a => a.Slug.Equals(str) && !a.IsDeleted).Any())
            {
                str = ori + "-" + n.ToString();
            }
            return str;
        }

    }
}