using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KDMApi.DataContexts;
using KDMApi.Models.Helper;
using KDMApi.Models.Survey;
using KDMApi.Services;
using KDMApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using KDMApi.Models.Km;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Globalization;
using KDMApi.Models.Pipeline;
using Org.BouncyCastle.Crypto.Agreement.JPake;
using Org.BouncyCastle.Math.EC.Multiplier;
using ClosedXML.Excel;
using KDMApi.Models.Digital;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Wordprocessing;

namespace KDMApi.Controllers
{
    [Route("v1/[controller]")]
    [ApiController]
    [EnableCors("QuBisaPolicy")]
    public class SurveyController : ControllerBase
    {
        private readonly DefaultContext _context;
        private readonly FileService _fileService;
        private DataOptions _options;
        private readonly IEmailService _emailService;
        public SurveyController(DefaultContext context, Microsoft.Extensions.Options.IOptions<DataOptions> options, FileService fileService, IEmailService emailService)
        {
            _context = context;
            _options = options.Value;
            _fileService = fileService;
            _emailService = emailService;
        }

        /**
         * @api {get} /Survey/list/{publish} GET list survey
         * @apiVersion 1.0.0
         * @apiName GetSurveyList
         * @apiGroup Survey
         * @apiPermission Basic Auth
         * @apiParam {Number} publish         0 untuk draft, 1 untuk publish
         * 
         * @apiSuccessExample Success-Response:
         * [
         *     {
         *         "id": 1,
         *         "categoryId": 2,
         *         "title": "Process Maturity Level",
         *         "intro": "Survey to measure the maturity level of the process in your organization.",
         *         "addInfo": "",
         *         "grouping": false
         *     },
         *     {
         *         "id": 3,
         *         "categoryId": 4,
         *         "title": "HR Diagnostics",
         *         "intro": "Survey to measure the HR practices in your organization",
         *         "addInfo": "",
         *         "grouping": false
         *     },
         *     {
         *         "id": 4,
         *         "categoryId": 1,
         *         "title": "Strategy and Performance Execution Excellence\u00AE Audit",
         *         "intro": "Survey to measure the strategy and execution excellence in your organization",
         *         "addInfo": "",
         *         "grouping": true
         *     }
         * ]
         */
        [AllowAnonymous]
        [HttpGet("list/{publish}")]
        public async Task<ActionResult<List<SurveyListItem>>> GetSurveyList(int publish) 
        {
            if (!CheckBasicAuth(Request.Headers["Authorization"].ToString())) return Unauthorized();

            bool p = publish == 1;

            IEnumerable<SurveyListItem> query = from survey in _context.WebSurveys
                        where !survey.IsDeleted & survey.ExpiryDate > DateTime.Now && survey.Publish == p
                        select new SurveyListItem()
                        {
                            Id = survey.Id,
                            CategoryId = survey.CategoryId,
                            Title = survey.Title,
                            Intro = survey.Intro,
                            AddInfo = survey.AddInfo,
                            Grouping = survey.Grouping
                        };

            return query.ToList();
        }

        /**
         * @api {get} /Survey/uuid/{uuid} GET survey by UUID
         * @apiVersion 1.0.0
         * @apiName GetSurveyByUUID
         * @apiGroup Survey
         * @apiPermission Basic Auth
         * @apiParam {String} uuid         UUID dari group yang bersangkutan
         * 
         * @apiSuccessExample Success-Response:
         * {
         *     "surveyId": 4,
         *     "group": "Manager",
         *     "pages": 
         *     [
         *         {
         *             "id": 1,
         *             "pageNumber": 1,
         *             "title": {
         *                 "en": "Introduction",
         *                 "id": "Pengantar"
         *             },
         *             "intro": {
         *                 "en": "This survey measures the Human Resources Management practices ...",
         *                 "id": "Survey ini mengukur praktek pengelolaan Sumber Daya Manusia ..."
         *             },
         *             "items": []
         *         },
         *         {
         *             "id": 2,
         *             "pageNumber": 2,
         *             "title": {
         *                 "en": "Question 1 of 18",
         *                 "id": "Pertanyaan 1 dari 18"
         *             },
         *             "intro": {
         *                 "en": "",
         *                 "id": ""
         *             },
         *             "itemType": "single",
         *             "items": [
         *                 {
         *                     "id": 1,
         *                     "itemType": "single",
         *                     "text": {
         *                         "en": "Is your HR strategy aligned with the strategy cascaded from the corporate?",
         *                         "id": "Apakah strategi HR anda sudah selaras dengan strategi yang diturunkan dari korporat?"
         *                     },
         *                     "options": [
         *                         {
         *                             "id": 5,
         *                             "text": {
         *                                 "en": "Yes",
         *                                 "id": "Ya"
         *                             }
         *                         },
         *                         {
         *                             "id": 6,
         *                             "text": {
         *                                 "en": "No",
         *                                 "id": "Tidak"
         *                             }
         *                         }
         *                     ]
         *                 }
         *             ]
         *         },
         *         {
         *             "id": 3,
         *             "pageNumber": 3,
         *             "title": {
         *                 "en": "Organization Information",
         *                 "id": "Informasi Organisasi"
         *             },
         *             "intro": {
         *                 "en": "By completing this survey, ...",
         *                 "id": "Dengan mengikuti ini anda telah ..."
         *             },
         *             "itemType": "varied",
         *             "items": [
         *                 {
         *                     "id": 19,
         *                     "itemType": "textfield",
         *                     "text": {
         *                         "en": "Name",
         *                         "id": "Nama"
         *                     },
         *                     "options": []
         *                 },
         *                 {
         *                     "id": 20,
         *                     "itemType": "phone",
         *                     "text": {
         *                         "en": "Handphone No.",
         *                         "id": "No. Handphone"
         *                     },
         *                     "options": []
         *                 },
         *                 {
         *                     "id": 21,
         *                     "itemType": "email",
         *                     "text": {
         *                         "en": "Email",
         *                         "id": "Email"
         *                     },
         *                     "options": []
         *                 },
         *                 {
         *                     "id": 22,
         *                     "itemType": "textfield",
         *                     "text": {
         *                         "en": "Company Name",
         *                         "id": "Name Perusahaan"
         *                     },
         *                     "options": []
         *                 },
         *                 {
         *                     "id": 23,
         *                     "itemType": "single",
         *                     "text": {
         *                         "en": "Number of employees",
         *                         "id": "Jumlah Karyawan"
         *                     },
         *                     "options": [
         *                         {
         *                             "id": 7,
         *                             "text": {
         *                                 "en": "1 - 100",
         *                                 "id": "1 - 100"
         *                             }
         *                         },
         *                         {
         *                             "id": 8,
         *                             "text": {
         *                                 "en": "100 - 500",
         *                                 "id": "100 - 500"
         *                             }
         *                         },
         *                         {
         *                             "id": 9,
         *                             "text": {
         *                                 "en": "500 - 5000",
         *                                 "id": "500 - 5000"
         *                             }
         *                         },
         *                         {
         *                             "id": 10,
         *                             "text": {
         *                                 "en": "More than 5.000",
         *                                 "id": "Lebih dari 5.000"
         *                             }
         *                         }
         *                     ]
         *                 }
         *             ]
         *         }
         *    ]
         * }
         * 
         * @apiError NotFound UUID salah atau sudah melewati expiredTime.
         */
        [AllowAnonymous]
        [HttpGet("uuid/{uuid}")]
        public async Task<ActionResult<SurveyGroup>> GetSurveyByUUID(string uuid)
        {
            if (!CheckBasicAuth(Request.Headers["Authorization"].ToString())) return Unauthorized();

            SurveyGroup group = new SurveyGroup();

            if (!CheckBasicAuth(Request.Headers["Authorization"].ToString())) return Unauthorized();

            // Get survey by Group UUID
            var query = from g in _context.WebSurveyGroups
                        join o in _context.WebSurveyOwners
                        on g.OwnerId equals o.Id
                        join s in _context.WebSurveys
                        on o.SurveyId equals s.Id
                        where g.Uuid.Equals(uuid.Trim()) && g.ExpiredTime.AddDays(1) >= DateTime.Now
                        select new GenericInfo()
                        {
                            Id = s.Id,
                            Text = g.Name
                        };

            GenericInfo info = query.FirstOrDefault();
            if(info == null || info.Id <= 0)
            {
                return NotFound();
            }

            group.SurveyId = info.Id;
            group.Group = info.Text;

            group.Pages = await GetSurveyPages(info.Id);

            return group;
        }


        /**
         * @api {get} /Survey/{id} GET survey
         * @apiVersion 1.0.0
         * @apiName GetSurveyById
         * @apiGroup Survey
         * @apiPermission Basic Auth
         * @apiParam {Number} id         Id dari survey yang bersangkutan
         * 
         * @apiSuccessExample Success-Response:
         * [
         *     {
         *         "id": 1,
         *         "pageNumber": 1,
         *         "title": {
         *             "en": "Introduction",
         *             "id": "Pengantar"
         *         },
         *         "intro": {
         *             "en": "This survey measures the Human Resources Management practices ...",
         *             "id": "Survey ini mengukur praktek pengelolaan Sumber Daya Manusia ..."
         *         },
         *         "items": []
         *     },
         *     {
         *         "id": 2,
         *         "pageNumber": 2,
         *         "title": {
         *             "en": "Question 1 of 18",
         *             "id": "Pertanyaan 1 dari 18"
         *         },
         *         "intro": {
         *             "en": "",
         *             "id": ""
         *         },
         *         "itemType": "single",
         *         "items": [
         *             {
         *                 "id": 1,
         *                 "itemType": "single",
         *                 "text": {
         *                     "en": "Is your HR strategy aligned with the strategy cascaded from the corporate?",
         *                     "id": "Apakah strategi HR anda sudah selaras dengan strategi yang diturunkan dari korporat?"
         *                 },
         *                 "options": [
         *                     {
         *                         "id": 5,
         *                         "text": {
         *                             "en": "Yes",
         *                             "id": "Ya"
         *                         }
         *                     },
         *                     {
         *                         "id": 6,
         *                         "text": {
         *                             "en": "No",
         *                             "id": "Tidak"
         *                         }
         *                     }
         *                 ]
         *             }
         *         ]
         *     },
         *     {
         *         "id": 3,
         *         "pageNumber": 3,
         *         "title": {
         *             "en": "Organization Information",
         *             "id": "Informasi Organisasi"
         *         },
         *         "intro": {
         *             "en": "By completing this survey, ...",
         *             "id": "Dengan mengikuti ini anda telah ..."
         *         },
         *         "itemType": "varied",
         *         "items": [
         *             {
         *                 "id": 19,
         *                 "itemType": "textfield",
         *                 "text": {
         *                     "en": "Name",
         *                     "id": "Nama"
         *                 },
         *                 "options": []
         *             },
         *             {
         *                 "id": 20,
         *                 "itemType": "phone",
         *                 "text": {
         *                     "en": "Handphone No.",
         *                     "id": "No. Handphone"
         *                 },
         *                 "options": []
         *             },
         *             {
         *                 "id": 21,
         *                 "itemType": "email",
         *                 "text": {
         *                     "en": "Email",
         *                     "id": "Email"
         *                 },
         *                 "options": []
         *             },
         *             {
         *                 "id": 22,
         *                 "itemType": "textfield",
         *                 "text": {
         *                     "en": "Company Name",
         *                     "id": "Name Perusahaan"
         *                 },
         *                 "options": []
         *             },
         *             {
         *                 "id": 23,
         *                 "itemType": "single",
         *                 "text": {
         *                     "en": "Number of employees",
         *                     "id": "Jumlah Karyawan"
         *                 },
         *                 "options": [
         *                     {
         *                         "id": 7,
         *                         "text": {
         *                             "en": "1 - 100",
         *                             "id": "1 - 100"
         *                         }
         *                     },
         *                     {
         *                         "id": 8,
         *                         "text": {
         *                             "en": "100 - 500",
         *                             "id": "100 - 500"
         *                         }
         *                     },
         *                     {
         *                         "id": 9,
         *                         "text": {
         *                             "en": "500 - 5000",
         *                             "id": "500 - 5000"
         *                         }
         *                     },
         *                     {
         *                         "id": 10,
         *                         "text": {
         *                             "en": "More than 5.000",
         *                             "id": "Lebih dari 5.000"
         *                         }
         *                     }
         *                 ]
         *             }
         *         ]
         *     }
         * ]
         */
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<List<SurveyPage>>> GetSurveyById(int id)
        {
            if (!CheckBasicAuth(Request.Headers["Authorization"].ToString())) return Unauthorized();

            return await GetSurveyPages(id);
        }

        private async Task<List<SurveyPage>> GetSurveyPages(int id)
        {

            List<WebSurveyPage> pages = _context.WebSurveyPages.Where(a => !a.IsDeleted && a.SurveyId == id).OrderBy(a => a.PageNumber).ToList();

            List<SurveyPage> response = new List<SurveyPage>();

            foreach (WebSurveyPage page in pages)
            {
                SurveyPage newPage = new SurveyPage()
                {
                    Id = page.Id,
                    PageNumber = page.PageNumber,
                    Title = new DualLanguage(page.TitleEn, page.TitleId),
                    Intro = new DualLanguage(page.IntroEn, page.IntroId),
                    Items = new List<SurveyItem>(),
                    ItemType = ""
                };

                var query = from item in _context.WebSurveyItems
                            join itemType in _context.WebSurveyItemTypes
                            on item.TypeId equals itemType.Id
                            where item.PageId == page.Id && !item.IsDeleted
                            orderby item.OrderNumber
                            select new
                            {
                                item.Id,
                                item.RatingId,
                                itemType.ItemType,
                                item.ItemTextEn,
                                item.ItemTextId,
                                item.TitleTextEn,
                                item.TitleTextId
                            };
                var items = query.ToList();

                foreach (var item in items)
                {
                    SurveyItem newItem = new SurveyItem()
                    {
                        Id = item.Id,
                        ItemType = item.ItemType,
                        Title = new DualLanguage(item.TitleTextId, item.TitleTextId),
                        Text = new DualLanguage(item.ItemTextEn, item.ItemTextId),
                        Options = new List<DualLanguageId>()
                    };

                    if (newPage.ItemType.Equals(""))
                    {
                        newPage.ItemType = item.ItemType;
                    }
                    else if (!newPage.ItemType.Equals(item.ItemType) && !newPage.ItemType.Equals("varied"))
                    {
                        newPage.ItemType = "varied";
                    }

                    var q = from rating in _context.WebSurveyRatings
                            join i in _context.WebSurveyRatingItems
                            on rating.Id equals i.RatingId
                            where rating.Id == item.RatingId && !i.IsDeleted
                            select new
                            {
                                i.Id,
                                i.ItemTextEn,
                                i.ItemTextID
                            };
                    var ratings = q.ToList();

                    foreach (var rating in ratings)
                    {
                        DualLanguageId i = new DualLanguageId()
                        {
                            Id = rating.Id,
                            Text = new DualLanguage(rating.ItemTextEn, rating.ItemTextID)
                        };
                        newItem.Options.Add(i);
                    }
                    newPage.Items.Add(newItem);
                }

                response.Add(newPage);
            }
            return response;

        }
        /**
         * @api {post} /survey POST survey 
         * @apiVersion 1.0.0
         * @apiName PostSurveyResponse
         * @apiGroup Survey
         * @apiPermission Basic authentication
         * @apiParam {Number} surveyId         Id dari survey yang dipilih oleh survey
         * @apiParam {String} uuid             Group UUID. "" kalau tidak ada Group UUID.
         * @apiParam {Number} id               0 karena mengirimkan jawaban yang baru dari user.
         * @apiParam {Number} itemId           Id dari butir pertanyaan yang bersangkutan
         * @apiParam {Number} ratingId         Id dari jawaban yang dipilih oleh user. 0 untuk pertanyaan isian (nama, email, dll)
         * @apiParam {String} answerText       Jawaban yang diisi oleh user untuk pertanyaan isian (nama, email, dll). "" untuk pertanyaan pilihan
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *   "surveyId": 3,
         *   "uuid": "36ec2e2e-d9d3-462a-9bbc-5dcb3d587f09",
         *   "responses": [
         *     {
         *       "id": 0,
         *       "itemId": 2,
         *       "ratingId": 5,
         *       "answerText": ""
         *     }, 
         *     {
         *       "id": 0,
         *       "itemId": 3,
         *       "ratingId": 5,
         *       "answerText": ""
         *     },
         *   ]
         * }
         *   
         * @apiSuccessExample Success-Response:
         * {
         *     "id": 5,
         *     "text": {
         *         "en": "Your organization is in excellent state.",
         *         "id": "Organisasi anda ada di tahap sungguh sangat baik."
         *     }
         * }
         */
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult<DualLanguageId>> PostSurveyResponse(PostSurveyResponse request)
        {
            if (!CheckBasicAuth(Request.Headers["Authorization"].ToString())) return Unauthorized();

            WebSurvey survey = _context.WebSurveys.Find(request.SurveyId);

            if(survey == null)
            {
                return NotFound(new { error = "Survey tidak ditemukan. Periksa kembali surveyId" });
            }

            DateTime now = DateTime.Now;
            string uuid = GetUUID();

            int ranking = 0;
            int counter = 0;

            // Dibalik dulu biar yang soal ranking urutannya benar
            request.responses.Reverse();

            foreach(SurveyResponse response in request.responses)
            {
                if((survey.Title.StartsWith("HR Business Partner") && response.RatingId == 0 && response.AnswerText.Equals(""))
                    || (survey.Title.StartsWith("Employee Engagement & Digital") && response.RatingId == 0 && response.AnswerText.Equals("")))
                {
                    // Khusus untuk pertanyaan ranking di HRBP
                    counter++;
                    ranking = counter;
                }
                else
                {
                    ranking = 0;
                }

                WebSurveyResponse surveyResponse = new WebSurveyResponse()
                {
                    ItemId = response.ItemId,
                    RatingId = response.RatingId,
                    Ranking = ranking,
                    AnswerText = response.AnswerText,
                    Uuid = uuid,
                    GroupUUID = request.Uuid,
                    CreatedDate = now,
                    LastUpdated = now,
                    IsDeleted = false
                };

                _context.WebSurveyResponses.Add(surveyResponse);
            }

            await _context.SaveChangesAsync();

            return await GetFeedbackInfo(request.SurveyId, uuid);
        }


        /**
         * @api {get} /Survey/report/{surveyId}/{uuid} GET report
         * @apiVersion 1.0.0
         * @apiName GetSurveyReport
         * @apiGroup Survey
         * @apiPermission Basic Auth
         * @apiParam {Number} SurveyId     id dari survey yang bersangkutan
         * @apiParam {String} uuid         uuid dari report yang bersangkutan
         * 
         * @apiSuccessExample Success-Response:
         * {
         *     "surveyId": 3,
         *     "title": "HR Diagnostics",
         *     "cover": [
         *         {
         *             "id": 19,
         *             "indicator": "Nama",
         *             "text": "Nama Test"
         *         },
         *         {
         *             "id": 20,
         *             "indicator": "No. Handphone",
         *             "text": "08179879892"
         *         },
         *         {
         *             "id": 21,
         *             "indicator": "Email",
         *             "text": "email@email.com"
         *         },
         *         {
         *             "id": 22,
         *             "indicator": "Name Perusahaan",
         *             "text": "PT Bintang"
         *         },
         *         {
         *             "id": 23,
         *             "indicator": "Jumlah Karyawan",
         *             "text": "100 - 500"
         *         }
         *     ],
         *     "dimensions": [
         *         {
         *             "id": 1,
         *             "title": "Strategic",
         *             "description": "Lorem ipsum dolor sit amet",
         *             "indicators": [
         *                 {
         *                     "id": 1,
         *                     "indicator": "HR Strategy",
         *                     "value": 1.0,
         *                     "weight": 14.286
         *                 },
         *                 {
         *                     "id": 2,
         *                     "indicator": "Organization Structure",
         *                     "value": 1.0,
         *                     "weight": 14.286
         *                 },
         *                 {
         *                     "id": 3,
         *                     "indicator": "Manpower Planning",
         *                     "value": 1.0,
         *                     "weight": 28.571
         *                 },
         *                 {
         *                     "id": 4,
         *                     "indicator": "Job Profile",
         *                     "value": 1.0,
         *                     "weight": 28.571
         *                 },
         *                 {
         *                     "id": 5,
         *                     "indicator": "Competency Model and Profiling",
         *                     "value": 1.0,
         *                     "weight": 28.571
         *                 },
         *                 {
         *                     "id": 6,
         *                     "indicator": "Job Grading & Evaluation",
         *                     "value": 1.0,
         *                     "weight": 14.286
         *                 },
         *                 {
         *                     "id": 7,
         *                     "indicator": "Workload Analysis",
         *                     "value": 1.0,
         *                     "weight": 28.571
         *                 }
         *             ],
         *             "dimensions": []
         *         },
         *         {
         *             "id": 2,
         *             "title": "Primary",
         *             "description": "Lorem ipsum dolor sit amet",
         *             "indicators": [],
         *             "dimensions": [
         *                 {
         *                     "id": 11,
         *                     "title": "Attract & Select",
         *                     "description": null,
         *                     "indicators": [
         *                         {
         *                             "id": 1,
         *                             "indicator": "Recruitment",
         *                             "value": 1.0,
         *                             "weight": 12.5
         *                         }
         *                     ],
         *                     "dimensions": []
         *                 },
         *                 {
         *                     "id": 12,
         *                     "title": "Managing Productivity",
         *                     "description": null,
         *                     "indicators": [
         *                         {
         *                             "id": 2,
         *                             "indicator": "Performance Management",
         *                             "value": 1.0,
         *                             "weight": 12.5
         *                         },
         *                         {
         *                             "id": 3,
         *                             "indicator": "Training and Development",
         *                             "value": 1.0,
         *                             "weight": 12.5
         *                         }
         *                     ],
         *                     "dimensions": []
         *                 },
         *                 {
         *                     "id": 13,
         *                     "title": "Managing Talent",
         *                     "description": null,
         *                     "indicators": [
         *                         {
         *                             "id": 4,
         *                             "indicator": "Talent Mapping",
         *                             "value": 1.0,
         *                             "weight": 12.5
         *                         },
         *                         {
         *                             "id": 5,
         *                             "indicator": "Career Planning",
         *                             "value": 1.0,
         *                             "weight": 24.0
         *                         },
         *                         {
         *                             "id": 6,
         *                             "indicator": "Succession Planning",
         *                             "value": 1.0,
         *                             "weight": 24.0
         *                         }
         *                     ],
         *                     "dimensions": []
         *                 },
         *                 {
         *                     "id": 14,
         *                     "title": "Reward & Retain",
         *                     "description": null,
         *                     "indicators": [
         *                         {
         *                             "id": 7,
         *                             "indicator": "Reward and Recognition",
         *                             "value": 1.0,
         *                             "weight": 13.0
         *                         },
         *                         {
         *                             "id": 8,
         *                             "indicator": "Employee Engagement",
         *                             "value": 1.0,
         *                             "weight": 12.5
         *                         }
         *                     ],
         *                     "dimensions": []
         *                 }
         *             ]
         *         },
         *         {
         *             "id": 3,
         *             "title": "Foundation",
         *             "description": "Lorem ipsum dolor sit amet",
         *             "indicators": [
         *                 {
         *                     "id": 1,
         *                     "indicator": "Change Management and Culture Internalization",
         *                     "value": 0.5,
         *                     "weight": 20.0
         *                 },
         *                 {
         *                     "id": 3,
         *                     "indicator": "HRIS",
         *                     "value": 1.0,
         *                     "weight": 40.0
         *                 },
         *                 {
         *                     "id": 4,
         *                     "indicator": "Employee/Industrial Relation",
         *                     "value": 1.0,
         *                     "weight": 20.0
         *                 },
         *                 {
         *                     "id": 5,
         *                     "indicator": "SOP",
         *                     "value": 1.0,
         *                     "weight": 20.0
         *                 }
         *             ],
         *             "dimensions": []
         *         }
         *     ],
         *     "summary": {
         *         "id": 0,
         *         "title": "Well Managed",
         *         "description": "Selamat! Organisasi anda telah memiliki strategi pengelolaan SDM yang baku dan dilengkapi dengan program serta indikator keberhasilan yang objektif.Arahan strategi SDM sudah dapat diasumsikan merupakan turunan dari korporat sehingga sistem dan rencana aksi setiap program SDM telah sistematis dan menciptakan kolaborasi yang optimal antar pelaksana.Sebagai praktisi SDM, anda perlu mempertahankan kondisi ini untuk menjaga motivasi dan kontribusi para talent di dalam organisasi.",
         *         "indicators": [
         *             {
         *                 "id": 1,
         *                 "indicator": "Strategic",
         *                 "value": 82.0,
         *                 "weight": 100.0
         *             },
         *             {
         *                 "id": 2,
         *                 "indicator": "Primary",
         *                 "value": 100.0,
         *                 "weight": 100.0
         *             },
         *             {
         *                 "id": 3,
         *                 "indicator": "Foundation",
         *                 "value": 100.0,
         *                 "weight": 100.0
         *             }
         *         ],
         *         "dimensions": []
         *     }
         * }
         */
        [AllowAnonymous]
        [HttpGet("report/{surveyId}/{uuid}")]
        public async Task<ActionResult<SurveyReport>> GetSurveyReport(int surveyId, string uuid)
        {
            if (!CheckBasicAuth(Request.Headers["Authorization"].ToString())) return Unauthorized();

            if (!SurveyExists(surveyId))
            {
                return NotFound();
            }

            WebSurvey survey = _context.WebSurveys.Find(surveyId);
            if(survey != null)
            {
                if(survey.Title.StartsWith("Process Maturity Level"))
                {
                    return await GetProcessMaturityReport(surveyId, uuid);
                }
                else if (survey.Title.StartsWith("HR Diagnostics"))
                {
                    return await GetHRDiagnosticReport(surveyId, uuid);
                }
                else if (survey.Title.StartsWith("Strategy and Performance Execution"))
                {
                    return await GetSPEx2Report(surveyId, uuid);
                }
                else if (survey.Title.StartsWith("Organization Diagnostics"))
                {
                    return await GetODReport(surveyId, uuid);
                }
                else if(survey.Title.StartsWith("HR Business Partner"))
                {
                    return await GetHRBPReport(surveyId, uuid);
                }
                else if (survey.Title.Contains("Akhlak"))
                {
                    return await GetAkhlakReport(surveyId, uuid);
                }
                else if (survey.Title.Contains("Leadership"))
                {
                    return await GetLeadershipReport(surveyId, uuid);
                }
                else if (survey.Title.Equals("Online BSC Quiz"))
                {
                    return await GetOnlineBSCReport(surveyId, uuid);
                }
            }

            return NotFound(new { error = "Unknown surveyId. " });
        }

        /**
         * @api {get} /survey/dx/{fromDate}/{toDate}/{page}/{perPage}/{search} GET list responden DX
         * @apiVersion 1.0.0
         * @apiName GetDxIndividualReportList
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} userId                id dari user yang datanya mau diambil
         * @apiParam {Number} fromMonth             Mulai dari bulan berapa, misalnya 1 untuk bulan Januari
         * @apiParam {Number} toMonth               Sampai bulan berapa, misalnya 5 untuk bulan Mei
         * @apiParam {Number} year                  Tahun yang datanya ingin diambil, misal 2020
         * @apiParam {Number} page                  Halaman yang ditampilkan.
         * @apiParam {Number} perPage               Jumlah data per halaman.
         * @apiParam {String} search                * untuk tidak menggunakan search, atau kata yang dicari.
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "items": [
         *           {
         *               "proposalId": 38,
         *               "name": "License VirtualAC",
         *               "type": "Project",
         *               "sentById": 6,
         *               "sentBy": "Santi Susanti",
         *               "sentDate": "2020-04-22T00:00:00",
         *               "proposalValue": 72500000,
         *               "filename": "Proposal VirtualAC.pptx",
         *               "invoices": [
         *                  {
         *                      "id": 274,
         *                      "invoiceDate": "2020-08-18T00:00:00",
         *                      "invoiceAmount": 54545455,
         *                      "remarks": "inv 11 Sept"
         *                  }
         *               ],
         *               "receiverClients": [
         *                  {
         *                      "id": 38173,
         *                      "text": "Rio Lazuardy"
         *                  }
         *              ]
         *           },
         *           {
         *               "proposalId": 41,
         *               "name": "Roadmap BKKBN 2020",
         *               "type": "Workshop",
         *               "sentById": 6,
         *               "sentBy": "Santi Susanti",
         *               "sentDate": "2020-06-18T00:00:00",
         *               "proposalValue": 153500000,
         *               "filename": "Proposal Roadmap BKKBN 2020.pptx",
         *               "invoices": [
         *                  {
         *                      "id": 274,
         *                      "invoiceDate": "2020-08-18T00:00:00",
         *                      "invoiceAmount": 54545455,
         *                      "remarks": "inv 11 Sept"
         *                  }
         *               ],
         *               "receiverClients": [
         *                  {
         *                      "id": 38173,
         *                      "text": "Rio Lazuardy"
         *                  }
         *              ]
         *           }
         *       ],
         *       "info": {
         *           "page": 1,
         *           "perPage": 2,
         *           "total": 21
         *       }
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [AllowAnonymous]
        [HttpGet("dx/{fromDate}/{toDate}/{page}/{perPage}")]
        public async Task<ActionResult<DxIndividualReportList>> GetDxIndividualReportList(string fromDate, string toDate, int page, int perPage)
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

            DateTime f = new DateTime(1900, 1, 1);
            DateTime to = new DateTime(2100, 12, 31);
            if(!fromDate.Equals("0"))
            {
                f = DateTime.ParseExact(fromDate, "yyyyMMdd", CultureInfo.InvariantCulture);
            }
            if(!toDate.Equals("0"))
            {
                to = DateTime.ParseExact(toDate, "yyyyMMdd", CultureInfo.InvariantCulture).AddDays(1);
            }
            WebSurveyItem nm = GetDxSurveyItem("Digital Transformation Readiness Assessment", "Nama");
            if (nm == null) return NotFound();
            WebSurveyItem comp = GetDxSurveyItem("Digital Transformation Readiness Assessment", "Nama Perusahaan");
            if (comp == null) return NotFound();

            var query = from response in _context.WebSurveyResponses
                        where !response.IsDeleted && response.CreatedDate >= f && response.CreatedDate <= to && response.ItemId == nm.Id
                        orderby response.CreatedDate descending
                        select new DxIndividualReportItem()
                        {
                            Name = response.AnswerText,
                            Company = "",
                            SurveyDate = response.CreatedDate,
                            Uuid = response.Uuid
                        };

            DxIndividualReportList list = new DxIndividualReportList();
            list.Items = await query.Skip(perPage * (page - 1)).Take(perPage).ToListAsync<DxIndividualReportItem>();
            foreach(DxIndividualReportItem item in list.Items)
            {
                var q = from response in _context.WebSurveyResponses
                        where response.ItemId == comp.Id && response.Uuid.Equals(item.Uuid)
                        select new DxIndividualReportItem()
                        {
                            Name = "",
                            Company = response.AnswerText,
                            SurveyDate = response.CreatedDate,
                            Uuid = response.Uuid
                        };
                DxIndividualReportItem i = q.FirstOrDefault();
                if (i != null) item.Company = i.Company;
            }
            list.Info = new Models.Crm.PaginationInfo(page, perPage, query.Count());
            return list;

        }

        private WebSurveyItem GetDxSurveyItem(string survey, string item)
        {
            /*
             * select item.*
from WebSurveys as survey
join WebSurveyPages as p on p.SurveyId=survey.Id
join WebSurveyItems as item on p.Id=item.PageId
where survey.Title like 'Digital Transformation Readiness Assessment' 
and item.ItemTextId like 'Nama'
             */
            var query = from s in _context.WebSurveys
                        join p in _context.WebSurveyPages on s.Id equals p.SurveyId
                        join i in _context.WebSurveyItems on p.Id equals i.PageId
                        where s.Title.Contains(survey) && i.ItemTextId.Contains(item)
                        select i;
            return query.FirstOrDefault();
        }

        /**
         * @api {get} /Survey/report/edtra/{surveyId}/{uuid} GET report EDTRA
         * @apiVersion 1.0.0
         * @apiName GetEdtraReport
         * @apiGroup Survey
         * @apiPermission Basic Auth
         * @apiParam {Number} SurveyId     id dari survey yang bersangkutan
         * @apiParam {String} uuid         uuid dari report yang bersangkutan
         * 
         * @apiSuccessExample Success-Response:
         * {
         *     "id": 11,
         *     "surveyName": "Employee Engagement & Digital Transformation Readiness Assessment",
         *     "uuid": "c05a19cc-98c7-4f23-bfd4-ec7b035a0c3c",
         *     "groupName": "Mawar",
         *     "total": 3,
         *     "reportDate": "2021-08-12T23:44:45.9193664+07:00",
         *     "engagementIndex1": 3.708333333333333,
         *     "engagementIndex2": 0.92708333333333326,
         *     "quadrants": [
         *         {
         *             "id": 1,
         *             "indicator": "Actively Engaged",
         *             "value": 100.0,
         *             "weight": 100.0
         *         },
         *         {
         *             "id": 2,
         *             "indicator": "Passively Engaged",
         *             "value": 0.0,
         *             "weight": 100.0
         *         },
         *         {
         *             "id": 3,
         *             "indicator": "Disengaged",
         *             "value": 0.0,
         *             "weight": 100.0
         *         },
         *         {
         *             "id": 4,
         *             "indicator": "Potentially Engaged",
         *             "value": 0.0,
         *             "weight": 100.0
         *         }
         *     ],
         *     "charts": [
         *         {
         *             "items": [
         *                 {
         *                     "indicator": "Kepuasan",
         *                     "values": [
         *                         3.75
         *                     ]
         *                 }
         *             ],
         *             "legends": [
         *                 "Kepuasan"
         *             ]
         *         },
         *         {
         *             "items": [
         *                 {
         *                     "indicator": "Kepuasan terhadap Imbalan dan Fasilitas",
         *                     "values": [
         *                         3.25
         *                     ]
         *                 },
         *                 {
         *                     "indicator": "Kepuasan terhadap Keamanan dan Kenyamanan",
         *                     "values": [
         *                         4.0
         *                     ]
         *                 },
         *                 {
         *                     "indicator": "Kepuasan terhadap Kebersamaan",
         *                     "values": [
         *                         3.5
         *                     ]
         *                 },
         *                 {
         *                     "indicator": "Kepuasan terhadap Pengembangan Diri",
         *                     "values": [
         *                         4.0
         *                     ]
         *                 },
         *                 {
         *                     "indicator": "Kepuasan terehadap Aktualisasi",
         *                     "values": [
         *                         3.75
         *                     ]
         *                 },
         *                 {
         *                     "indicator": "Satisfaction in Pandemic",
         *                     "values": [
         *                         4.0
         *                     ]
         *                 }
         *             ],
         *             "legends": [
         *                 "Kepuasan terhadap Imbalan dan Fasilitas",
         *                 "Kepuasan terhadap Keamanan dan Kenyamanan",
         *                 "Kepuasan terhadap Kebersamaan",
         *                 "Kepuasan terhadap Pengembangan Diri",
         *                 "Kepuasan terehadap Aktualisasi",
         *                 "Satisfaction in Pandemic"
         *             ]
         *         },
         *         {
         *             "items": [
         *                 {
         *                     "indicator": "Komitmen",
         *                     "values": [
         *                         3.6666666666666665
         *                     ]
         *                 }
         *             ],
         *             "legends": [
         *                 "Komitmen"
         *             ]
         *         },
         *         {
         *             "items": [
         *                 {
         *                     "indicator": "Komitmen terhadap Organisasi",
         *                     "values": [
         *                         3.25
         *                     ]
         *                 },
         *                 {
         *                     "indicator": "Komitmen terhadap Tugas",
         *                     "values": [
         *                         3.75
         *                     ]
         *                 },
         *                 {
         *                     "indicator": "Komitmen terhadap Unit Kerja",
         *                     "values": [
         *                         4.0
         *                     ]
         *                 }
         *             ],
         *             "legends": [
         *                 "Komitmen terhadap Organisasi",
         *                 "Komitmen terhadap Tugas",
         *                 "Komitmen terhadap Unit Kerja"
         *             ]
         *         },
         *         {
         *             "items": [
         *                 {
         *                     "indicator": "Sosialisasi yang jelas dan transparan terkait transformasi dan perubahan di perusahaan",
         *                     "values": [
         *                         33.333333333333329
         *                     ]
         *                 },
         *                 {
         *                     "indicator": "Kepercayaan dan kepedulian atasan serta rekan kerja dalam menjalankan pekerjaan",
         *                     "values": [
         *                         26.666666666666668
         *                     ]
         *                 },
         *                 {
         *                     "indicator": "Pemberian penghargaan dan kompensasi yang sesuai dengan posisi dan kinerja.",
         *                     "values": [
         *                         20.0
         *                     ]
         *                 },
         *                 {
         *                     "indicator": "Kejelasan dan kesempatan memperoleh pengembangan diri dan pengembangan karir",
         *                     "values": [
         *                         13.333333333333334
         *                     ]
         *                 },
         *                 {
         *                     "indicator": "Kesempatan ikut berkontribusi (ide dan saran) baik dalam bekerja maupun dalam pengembangan perusahaan",
         *                     "values": [
         *                         6.666666666666667
         *                     ]
         *                 }
         *             ],
         *             "legends": [
         *                 "Sosialisasi yang jelas dan transparan terkait transformasi dan perubahan di perusahaan",
         *                 "Kepercayaan dan kepedulian atasan serta rekan kerja dalam menjalankan pekerjaan",
         *                 "Pemberian penghargaan dan kompensasi yang sesuai dengan posisi dan kinerja.",
         *                 "Kejelasan dan kesempatan memperoleh pengembangan diri dan pengembangan karir",
         *                 "Kesempatan ikut berkontribusi (ide dan saran) baik dalam bekerja maupun dalam pengembangan perusahaan"
         *             ]
         *         },
         *         {
         *             "items": [
         *                 {
         *                     "indicator": "Operasional",
         *                     "values": [
         *                         3.0
         *                     ]
         *                 },
         *                 {
         *                     "indicator": "Penjualan",
         *                     "values": [
         *                         0.0
         *                     ]
         *                 },
         *                 {
         *                     "indicator": "Keuangan",
         *                     "values": [
         *                         0.0
         *                     ]
         *                 },
         *                 {
         *                     "indicator": "Pemasaran",
         *                     "values": [
         *                         0.0
         *                     ]
         *                 },
         *                 {
         *                     "indicator": "Pengembangan Produk",
         *                     "values": [
         *                         0.0
         *                     ]
         *                 },
         *                 {
         *                     "indicator": "SDM",
         *                     "values": [
         *                         0.0
         *                     ]
         *                 },
         *                 {
         *                     "indicator": "Teknologi Informasi dan Digital",
         *                     "values": [
         *                         0.0
         *                     ]
         *                 },
         *                 {
         *                     "indicator": "Lainnya",
         *                     "values": [
         *                         0.0
         *                     ]
         *                 }
         *             ],
         *             "legends": [
         *                 "Operasional",
         *                 "Penjualan",
         *                 "Keuangan",
         *                 "Pemasaran",
         *                 "Pengembangan Produk",
         *                 "SDM",
         *                 "Teknologi Informasi dan Digital",
         *                 "Lainnya"
         *             ]
         *         },
         *         {
         *             "items": [
         *                 {
         *                     "indicator": "Di bawah 1 tahun",
         *                     "values": [
         *                         100.0
         *                     ]
         *                 },
         *                 {
         *                     "indicator": "1 - 5 tahun",
         *                     "values": [
         *                         0.0
         *                     ]
         *                 },
         *                 {
         *                     "indicator": "5 - 10 tahun",
         *                     "values": [
         *                         0.0
         *                     ]
         *                 },
         *                 {
         *                     "indicator": "Di atas 10 tahun",
         *                     "values": [
         *                         0.0
         *                     ]
         *                 }
         *             ],
         *             "legends": [
         *                 "Di bawah 1 tahun",
         *                 "1 - 5 tahun",
         *                 "5 - 10 tahun",
         *                 "Di atas 10 tahun"
         *             ]
         *         }
         *     ],
         *     "digital": {
         *         "total": 3,
         *         "groupName": [
         *             "Mawar"
         *         ],
         *         "chart1": {
         *             "quadrant1": 100.0,
         *             "quadrant2": 0.0,
         *             "quadrant3": 0.0,
         *             "quadrant4": 0.0
         *         },
         *         "chart2": {
         *             "items": [
         *                 {
         *                     "indicator": "Formulasi Strategi",
         *                     "values": [
         *                         3.375,
         *                         3.68,
         *                         3.75
         *                     ]
         *                 },
         *                 {
         *                     "indicator": "Pemetaan Strategi Level Organisasi",
         *                     "values": [
         *                         2.0,
         *                         3.68,
         *                         3.69
         *                     ]
         *                 },
         *                 {
         *                     "indicator": "Penyelarasan Organisasi",
         *                     "values": [
         *                         3.6666666666666665,
         *                         3.52,
         *                         3.63
         *                     ]
         *                 },
         *                 {
         *                     "indicator": "Eksekusi Operasional",
         *                     "values": [
         *                         3.75,
         *                         3.44,
         *                         3.75
         *                     ]
         *                 },
         *                 {
         *                     "indicator": "Pemantauan dan Penyelarasan Kembali",
         *                     "values": [
         *                         3.5,
         *                         3.44,
         *                         3.56
         *                     ]
         *                 }
         *             ],
         *             "legends": [
         *                 "Mawar",
         *                 "SPEx2 Winners (2016)",
         *                 "BSC Hall of Fame (2005)"
         *             ]
         *         },
         *         "chart3": [
         *             {
         *                 "indicator": "Digital Differentiating Capability",
         *                 "values": [
         *                     0.0,
         *                     2.5000000000000018
         *                 ]
         *             }
         *         ],
         *         "chart4": [
         *             {
         *                 "indicator": "Execution-biased Systems",
         *                 "values": [
         *                     0.0,
         *                     7.2222222222222214
         *                 ]
         *             },
         *             {
         *                 "indicator": "Empowered Structure",
         *                 "values": [
         *                     0.0,
         *                     10.0
         *                 ]
         *             },
         *             {
         *                 "indicator": "Entrepreneurial People",
         *                 "values": [
         *                     0.0,
         *                     1.6666666666666679
         *                 ]
         *             },
         *             {
         *                 "indicator": "Adhocracy Culture",
         *                 "values": [
         *                     0.0,
         *                     3.7500000000000009
         *                 ]
         *             },
         *             {
         *                 "indicator": "Ambidextrous Leadership",
         *                 "values": [
         *                     0.0,
         *                     10.0
         *                 ]
         *             }
         *         ]
         *     }
         * }
         */
        [AllowAnonymous]
        [HttpGet("report/edtra/{surveyId}/{uuid}")]
        public async Task<ActionResult<EdtraReport>> GetEdtraReport(int surveyId, string uuid)
        {
            if (!CheckBasicAuth(Request.Headers["Authorization"].ToString())) return Unauthorized();

            if (!SurveyExists(surveyId))
            {
                return NotFound();
            }

            WebSurvey survey = _context.WebSurveys.Find(surveyId);
            WebSurveyGroup group = _context.WebSurveyGroups.Where(a => a.Uuid.Equals(uuid)).FirstOrDefault();
            if (group == null) return NotFound();

            List<string> indUuids = await _context.WebSurveyResponses.Where(a => a.GroupUUID.Equals(uuid)).Select(a => a.Uuid).Distinct().ToListAsync();

            EdtraReport report = new EdtraReport();

            report.Id = surveyId;
            report.SurveyName = survey.Title;
            report.Uuid = uuid;
            report.GroupName = group.Name;
            report.ReportDate = DateTime.Now;
            report.Total = indUuids.Count();
            report.Charts = new List<DigitalBarChartData>();

            DigitalBarChartData motivation = new DigitalBarChartData();
            motivation.Items = new List<DigitalBarItem>();
            motivation.Legends = new List<string>();

            double totalMotivation = 0.0d;
            double totalTenure = 0.0d;

            int rankingId = _context.WebSurveyRatings.Where(a => a.RatingName.ToLower().Equals("ranking") && !a.IsDeleted).Select(a => a.Id).FirstOrDefault();
            if(rankingId != 0)
            {
                var query = from i in _context.WebSurveyItems
                            join p in _context.WebSurveyPages on i.PageId equals p.Id
                            where p.SurveyId == surveyId && i.RatingId == rankingId && !i.IsDeleted && !p.IsDeleted
                            select new GenericInfo()
                            {
                                Id = i.Id,
                                Text = i.ItemTextId
                            };
                List<GenericInfo> infos = await query.OrderBy(a => a.Id).ToListAsync();
                foreach(GenericInfo info in infos)
                {
                    motivation.Items.Add(new DigitalBarItem()
                    {
                        Indicator = info.Text,
                        Values = new List<double>(new[] { 0.0d })
                    });
                }
            }

            List<DigitalBarChartData> addCharts = new List<DigitalBarChartData>();

            var addQuery = from i in _context.WebSurveyItems
                           join p in _context.WebSurveyPages on i.PageId equals p.Id
                           where p.SurveyId == surveyId && i.GroupReport && !i.IsDeleted && !p.IsDeleted
                           select new 
                           {
                               ItemId = i.Id,
                               i.RatingId,
                               i.ItemTextId
                           };
            var addItems = await addQuery.ToListAsync();

            foreach(var ai in addItems)
            {
                var qris = from ri in _context.WebSurveyRatingItems
                           where ri.RatingId == ai.RatingId && !ri.IsDeleted
                           select new GenericInfo()
                           {
                               Id = ri.Id,
                               Text = ri.ItemTextID
                           };
                List<GenericInfo> ratingItems = await qris.ToListAsync();

                DigitalBarChartData ad = new DigitalBarChartData();
                ad.Items = new List<DigitalBarItem>();
                ad.Legends = new List<string>();
                foreach(GenericInfo ratingItem in ratingItems)
                {
                    string str = ai.ItemTextId.ToLower().StartsWith("lama") ? ratingItem.Text + @" tahun" : ratingItem.Text;

                    var countQuery = from resp in _context.WebSurveyResponses
                                     where resp.GroupUUID.Equals(uuid) && (resp.RatingId == ratingItem.Id || resp.AnswerText.Equals(ratingItem.Id.ToString())) && !resp.IsDeleted
                                     select resp.Id;
                    int count = countQuery.Count();
                    ad.Items.Add(new DigitalBarItem()
                    {
                        Indicator = str,
                        Values = new List<double>(new[] { Convert.ToDouble(count) })
                    });
                    ad.Legends.Add(str);

                    if (ai.ItemTextId.ToLower().StartsWith("lama")) totalTenure += count;
                }
                addCharts.Add(ad);
            }

            double index = 0.0d;
            double nindex = 0.0d;

            List<StringDoubleDouble> idds = new List<StringDoubleDouble>();

            List<WebSurveyDimension> dimensions = await _context.WebSurveyDimensions.Where(a => a.SurveyId == surveyId && a.Parent == 0 && !a.IsDeleted).ToListAsync();
            foreach(WebSurveyDimension dimension in dimensions)
            {
                List<WebSurveyDimension> childDimensions = await _context.WebSurveyDimensions.Where(a => a.SurveyId == surveyId && a.Parent == dimension.Id && !a.IsDeleted).ToListAsync();
                if(childDimensions == null || childDimensions.Count() == 0) continue;

                DigitalBarChartData parentChart = new DigitalBarChartData();
                parentChart.Legends = new List<string>(new[] { dimension.ItemText });
                parentChart.Items = new List<DigitalBarItem>();

                double parentVal = 0.0d;

                List<IndicatorValue> values = await GetGroupIndicatorValues(dimension.Id, uuid);
                DigitalBarChartData childChart = new DigitalBarChartData();
                childChart.Legends = new List<string>();
                childChart.Items = new List<DigitalBarItem>();

                foreach(IndicatorValue val in values)
                {
                    childChart.Legends.Add(val.Indicator);
                    childChart.Items.Add(new DigitalBarItem()
                    {
                        Indicator = val.Indicator,
                        Values = new List<double>(new[] { val.Value })
                    });
                    parentVal += val.Value;
                }

                double childAverage = values.Count() > 0 ? parentVal / values.Count : 0.0d;
                parentChart.Items.Add(new DigitalBarItem()
                {
                    Indicator = dimension.ItemText,
                    Values = new List<double>(new[] { childAverage }) 
                }); ;

                report.Charts.Add(parentChart);
                report.Charts.Add(childChart);

                index += childAverage;
                nindex += 1;

                foreach (string indUuid in indUuids)
                {
                    List<IndicatorValue> indValues = await GetIndicatorValues(dimension.Id, indUuid);
                    double indSum = 0.0d;
                    foreach (IndicatorValue indValue in indValues)
                    {
                        indSum += indValue.Value;
                    }
                    double indAve = indValues.Count() > 0 ? indSum / indValues.Count() : 0.0d;
                    StringDoubleDouble idd = idds.Where(a => a.Uuid.Equals(indUuid)).FirstOrDefault();
                    if (idd == null)
                    {
                        idd = new StringDoubleDouble();
                        idd.Uuid = indUuid;
                        idds.Add(idd);
                    }
                    if (dimension.ItemText.StartsWith("Kepuasan"))
                    {
                        idd.D1 = indAve;
                    }
                    else
                    {
                        idd.D2 = indAve;
                    }

                    var q = from resp in _context.WebSurveyResponses
                            join i in _context.WebSurveyItems on resp.ItemId equals i.Id
                            where resp.Ranking > 0 && resp.Uuid.Equals(indUuid)
                            select new
                            {
                                i.ItemTextId,
                                resp.Ranking
                            };
                    var objs = await q.ToListAsync();
                    foreach(var obj in objs)
                    {
                        foreach (DigitalBarItem bi in motivation.Items)
                        {
                            if(bi.Indicator.Equals(obj.ItemTextId))
                            {
                                if (bi.Values != null && bi.Values.Count() != 0)
                                {
                                    bi.Values[0] += obj.Ranking;
                                    totalMotivation += obj.Ranking;
                                }
                                continue;
                            }
                        }
                    }
                }
            }

            report.EngagementIndex1 = nindex == 0 ? 0.0d : index / nindex;
            report.EngagementIndex2 = report.EngagementIndex1 / 4;

            int q1 = 0;
            int q2 = 0;
            int q3 = 0;
            int q4 = 0;

            foreach(StringDoubleDouble idd in idds)
            {
                if(idd.D1 >= 3)
                {
                    if(idd.D2 >= 3)
                    {
                        q1++;
                    }
                    else
                    {
                        q2++;
                    }
                }
                else
                {
                    if(idd.D2 < 3)
                    {
                        q3++;
                    }
                    else
                    {
                        q4++;
                    }
                }
            }

            int total = q1 + q2 + q3 + q4;

            report.Quadrants = new List<IndicatorValue>();
            report.Quadrants.Add(new IndicatorValue()
            {
                Id = 1,
                Indicator = "Actively Engaged",
                Value = total != 0 ? Convert.ToDouble(q1) / Convert.ToDouble(total) * 100.0d : 0.0d,
                Weight = 100
            });
            report.Quadrants.Add(new IndicatorValue()
            {
                Id = 2,
                Indicator = "Passively Engaged",
                Value = total != 0 ? Convert.ToDouble(q2) / Convert.ToDouble(total) * 100.0d : 0.0d,
                Weight = 100
            });
            report.Quadrants.Add(new IndicatorValue()
            {
                Id = 3,
                Indicator = "Disengaged",
                Value = total != 0 ? Convert.ToDouble(q3) / Convert.ToDouble(total) * 100.0d : 0.0d,
                Weight = 100
            });
            report.Quadrants.Add(new IndicatorValue()
            {
                Id = 4,
                Indicator = "Potentially Engaged",
                Value = total != 0 ? Convert.ToDouble(q4) / Convert.ToDouble(total) * 100.0d : 0.0d,
                Weight = 100
            });

            // motivation.Items = new List<DigitalBarItem>(motivation.Items.OrderBy(item => item, new DigitalBarItem()));
            foreach(DigitalBarItem i in motivation.Items)
            {
                motivation.Legends.Add(i.Indicator);
                if(i.Values != null && i.Values.Count() > 0)
                {
                    i.Values[0] = i.Values[0] / totalMotivation * 100;
                }
            }

            report.Charts.Add(motivation);

            foreach(DigitalBarChartData dt in addCharts)
            {
                foreach(DigitalBarItem bi in dt.Items)
                {
                    if(bi.Indicator.Contains("tahun"))
                    {
                        if (bi.Values != null && bi.Values.Count() > 0)
                            bi.Values[0] = totalTenure != 0 ? bi.Values[0] / totalTenure * 100 : bi.Values[0];
                    }
                }
            }
            foreach(DigitalBarChartData dt in addCharts)
            {
                report.Charts.Add(dt);
            }

            report.Digital = await GetDigitalGroupReportByGroupUUID(surveyId, uuid, group.Name, indUuids);

            return report;
        }



        [AllowAnonymous]
        [HttpGet("report/edtra/rev/{surveyId}/{uuid}")]
        public async Task<ActionResult<EdtraReport>> GetEdtraReportRev(int surveyId, string uuid)
        {
            if (!CheckBasicAuth(Request.Headers["Authorization"].ToString())) return Unauthorized();

            if (!SurveyExists(surveyId))
            {
                return NotFound();
            }

            WebSurvey survey = _context.WebSurveys.Find(surveyId);
            WebSurveyGroup group = _context.WebSurveyGroups.Where(a => a.Uuid.Equals(uuid)).FirstOrDefault();
            if (group == null) return NotFound();

            List<string> indUuids = await _context.WebSurveyResponses.Where(a => a.GroupUUID.Equals(uuid)).Select(a => a.Uuid).Distinct().ToListAsync();

            EdtraReport report = new EdtraReport();

            report.Id = surveyId;
            report.SurveyName = survey.Title;
            report.Uuid = uuid;
            report.GroupName = group.Name;
            report.ReportDate = DateTime.Now;
            report.Total = indUuids.Count();
            report.Charts = new List<DigitalBarChartData>();

            DigitalBarChartData motivation = new DigitalBarChartData();
            motivation.Items = new List<DigitalBarItem>();
            motivation.Legends = new List<string>();

            double totalMotivation = 0.0d;
            double totalTenure = 0.0d;

            int rankingId = _context.WebSurveyRatings.Where(a => a.RatingName.ToLower().Equals("ranking") && !a.IsDeleted).Select(a => a.Id).FirstOrDefault();
            if (rankingId != 0)
            {
                var query = from i in _context.WebSurveyItems
                            join p in _context.WebSurveyPages on i.PageId equals p.Id
                            where p.SurveyId == surveyId && i.RatingId == rankingId && !i.IsDeleted && !p.IsDeleted
                            select new GenericInfo()
                            {
                                Id = i.Id,
                                Text = i.ItemTextId
                            };
                List<GenericInfo> infos = await query.OrderBy(a => a.Id).ToListAsync();
                foreach (GenericInfo info in infos)
                {
                    motivation.Items.Add(new DigitalBarItem()
                    {
                        Indicator = info.Text,
                        Values = new List<double>(new[] { 0.0d })
                    });
                }
            }

            List<DigitalBarChartData> addCharts = new List<DigitalBarChartData>();

            var addQuery = from i in _context.WebSurveyItems
                           join p in _context.WebSurveyPages on i.PageId equals p.Id
                           where p.SurveyId == surveyId && i.GroupReport && !i.IsDeleted && !p.IsDeleted
                           select new
                           {
                               ItemId = i.Id,
                               i.RatingId,
                               i.ItemTextId
                           };
            var addItems = await addQuery.ToListAsync();

            foreach (var ai in addItems)
            {
                var qris = from ri in _context.WebSurveyRatingItems
                           where ri.RatingId == ai.RatingId && !ri.IsDeleted
                           select new GenericInfo()
                           {
                               Id = ri.Id,
                               Text = ri.ItemTextID
                           };
                List<GenericInfo> ratingItems = await qris.ToListAsync();

                DigitalBarChartData ad = new DigitalBarChartData();
                ad.Items = new List<DigitalBarItem>();
                ad.Legends = new List<string>();
                foreach (GenericInfo ratingItem in ratingItems)
                {
                    string str = ai.ItemTextId.ToLower().StartsWith("lama") ? ratingItem.Text + @" tahun" : ratingItem.Text;

                    var countQuery = from resp in _context.WebSurveyResponses
                                     where resp.GroupUUID.Equals(uuid) && (resp.RatingId == ratingItem.Id || resp.AnswerText.Equals(ratingItem.Id.ToString())) && !resp.IsDeleted
                                     select resp.Id;
                    int count = countQuery.Count();
                    ad.Items.Add(new DigitalBarItem()
                    {
                        Indicator = str,
                        Values = new List<double>(new[] { Convert.ToDouble(count) })
                    });
                    ad.Legends.Add(str);

                    if (ai.ItemTextId.ToLower().StartsWith("lama")) totalTenure += count;
                }
                addCharts.Add(ad);
            }

            double index = 0.0d;
            double nindex = 0.0d;

            List<StringDoubleDouble> idds = new List<StringDoubleDouble>();

            List<WebSurveyDimension> dimensions = await _context.WebSurveyDimensions.Where(a => a.SurveyId == surveyId && a.Parent == 0 && !a.IsDeleted).ToListAsync();
            foreach (WebSurveyDimension dimension in dimensions)
            {
                List<WebSurveyDimension> childDimensions = await _context.WebSurveyDimensions.Where(a => a.SurveyId == surveyId && a.Parent == dimension.Id && !a.IsDeleted).ToListAsync();
                if (childDimensions == null || childDimensions.Count() == 0) continue;

                DigitalBarChartData parentChart = new DigitalBarChartData();
                parentChart.Legends = new List<string>(new[] { dimension.ItemText });
                parentChart.Items = new List<DigitalBarItem>();

                double parentVal = 0.0d;

                List<IndicatorValue> values = await GetGroupIndicatorValues(dimension.Id, uuid);
                DigitalBarChartData childChart = new DigitalBarChartData();
                childChart.Legends = new List<string>();
                childChart.Items = new List<DigitalBarItem>();

                foreach (IndicatorValue val in values)
                {
                    childChart.Legends.Add(val.Indicator);
                    childChart.Items.Add(new DigitalBarItem()
                    {
                        Indicator = val.Indicator,
                        Values = new List<double>(new[] { val.Value })
                    });
                    parentVal += val.Value;
                }

                double childAverage = values.Count() > 0 ? parentVal / values.Count : 0.0d;
                parentChart.Items.Add(new DigitalBarItem()
                {
                    Indicator = dimension.ItemText,
                    Values = new List<double>(new[] { childAverage })
                }); ;

                report.Charts.Add(parentChart);
                report.Charts.Add(childChart);

                index += childAverage;
                nindex += 1;

                foreach (string indUuid in indUuids)
                {
                    List<IndicatorValue> indValues = await GetIndicatorValues(dimension.Id, indUuid);
                    double indSum = 0.0d;
                    foreach (IndicatorValue indValue in indValues)
                    {
                        indSum += indValue.Value;
                    }
                    double indAve = indValues.Count() > 0 ? indSum / indValues.Count() : 0.0d;
                    StringDoubleDouble idd = idds.Where(a => a.Uuid.Equals(indUuid)).FirstOrDefault();
                    if (idd == null)
                    {
                        idd = new StringDoubleDouble();
                        idd.Uuid = indUuid;
                        idds.Add(idd);
                    }
                    if (dimension.ItemText.StartsWith("Kepuasan"))
                    {
                        idd.D1 = indAve;
                    }
                    else
                    {
                        idd.D2 = indAve;
                    }

                    var q = from resp in _context.WebSurveyResponses
                            join i in _context.WebSurveyItems on resp.ItemId equals i.Id
                            where resp.Ranking > 0 && resp.Uuid.Equals(indUuid)
                            select new
                            {
                                i.ItemTextId,
                                resp.Ranking
                            };
                    var objs = await q.ToListAsync();
                    foreach (var obj in objs)
                    {
                        foreach (DigitalBarItem bi in motivation.Items)
                        {
                            if (bi.Indicator.Equals(obj.ItemTextId))
                            {
                                if (bi.Values != null && bi.Values.Count() != 0)
                                {
                                    bi.Values[0] += obj.Ranking;
                                    totalMotivation += obj.Ranking;
                                }
                                continue;
                            }
                        }
                    }
                }
            }

            report.EngagementIndex1 = nindex == 0 ? 0.0d : index / nindex;
            report.EngagementIndex2 = report.EngagementIndex1 / 4;

            int q1 = 0;
            int q2 = 0;
            int q3 = 0;
            int q4 = 0;

            foreach (StringDoubleDouble idd in idds)
            {
                if (idd.D1 >= 3)
                {
                    if (idd.D2 >= 3)
                    {
                        q1++;
                    }
                    else
                    {
                        q2++;
                    }
                }
                else
                {
                    if (idd.D2 < 3)
                    {
                        q3++;
                    }
                    else
                    {
                        q4++;
                    }
                }
            }

            int total = q1 + q2 + q3 + q4;

            report.Quadrants = new List<IndicatorValue>();
            report.Quadrants.Add(new IndicatorValue()
            {
                Id = 1,
                Indicator = "Actively Engaged",
                Value = total != 0 ? Convert.ToDouble(q1) / Convert.ToDouble(total) * 100.0d : 0.0d,
                Weight = 100
            });
            report.Quadrants.Add(new IndicatorValue()
            {
                Id = 2,
                Indicator = "Passively Engaged",
                Value = total != 0 ? Convert.ToDouble(q2) / Convert.ToDouble(total) * 100.0d : 0.0d,
                Weight = 100
            });
            report.Quadrants.Add(new IndicatorValue()
            {
                Id = 3,
                Indicator = "Disengaged",
                Value = total != 0 ? Convert.ToDouble(q3) / Convert.ToDouble(total) * 100.0d : 0.0d,
                Weight = 100
            });
            report.Quadrants.Add(new IndicatorValue()
            {
                Id = 4,
                Indicator = "Potentially Engaged",
                Value = total != 0 ? Convert.ToDouble(q4) / Convert.ToDouble(total) * 100.0d : 0.0d,
                Weight = 100
            });

            // motivation.Items = new List<DigitalBarItem>(motivation.Items.OrderBy(item => item, new DigitalBarItem()));
            foreach (DigitalBarItem i in motivation.Items)
            {
                motivation.Legends.Add(i.Indicator);
                if (i.Values != null && i.Values.Count() > 0)
                {
                    i.Values[0] = i.Values[0] / totalMotivation * 100;
                }
            }

            report.Charts.Add(motivation);

            foreach (DigitalBarChartData dt in addCharts)
            {
                foreach (DigitalBarItem bi in dt.Items)
                {
                    if (bi.Indicator.Contains("tahun"))
                    {
                        if (bi.Values != null && bi.Values.Count() > 0)
                            bi.Values[0] = totalTenure != 0 ? bi.Values[0] / totalTenure * 100 : bi.Values[0];
                    }
                }
            }
            foreach (DigitalBarChartData dt in addCharts)
            {
                report.Charts.Add(dt);
            }

            report.Digital = await GetDigitalGroupReportByGroupUUIDRev(surveyId, uuid, group.Name, indUuids);

            return report;
        }



        /**
         * @api {get} /Survey/options/{surveyId} GET options
         * @apiVersion 1.0.0
         * @apiName GetRatingOptions
         * @apiGroup Survey
         * @apiPermission Basic Auth
         * 
         * @apiSuccessExample Success-Response:
         * [
         *     {
         *         "id": 208,
         *         "text": {
         *             "en": "Agro Industry",
         *             "id": "Agro Industri"
         *         }
         *     },
         *     {
         *         "id": 209,
         *         "text": {
         *             "en": "Heavy Equipment, Mining",
         *             "id": "Alat Berat, Pertambangan"
         *         }
         *     }
         * ]
         */
        [AllowAnonymous]
        [HttpGet("options/{surveyId}")]
        public async Task<ActionResult<List<DualLanguageId>>> GetRatingOptions(int surveyId)
        {
            if (!CheckBasicAuth(Request.Headers["Authorization"].ToString())) return Unauthorized();

            var survey = _context.WebSurveys.Find(surveyId);

            if (survey == null) return NotFound();

            var q = from s in _context.WebSurveys
                    join p in _context.WebSurveyPages on s.Id equals p.SurveyId
                    join item in _context.WebSurveyItems on p.Id equals item.PageId
                    where item.GroupReport && p.SurveyId == surveyId
                    select item;

            var i = q.FirstOrDefault();
            if (i == null) return NotFound();

            int ratingId = i.RatingId;

            List<DualLanguageId> list = new List<DualLanguageId>();

            var items = await _context.WebSurveyRatingItems.Where(a => a.RatingId == ratingId && !a.IsDeleted).ToListAsync();
            foreach(WebSurveyRatingItem item in items)
            {
                list.Add(new DualLanguageId()
                {
                    Id = item.Id,
                    Text = new DualLanguage(item.ItemTextEn, item.ItemTextID)
                });
            }

            return list;
        }

        /**
         * @api {get} /Survey/industry/{surveyId}/{industries} GET report by industry
         * @apiVersion 1.0.0
         * @apiName GetIndustryReports
         * @apiGroup Survey
         * @apiPermission Basic Auth
         * 
         * @apiSuccessExample Success-Response:
         * [
         *     {
         *         "id": 208,
         *         "text": {
         *             "en": "Agro Industry",
         *             "id": "Agro Industri"
         *         }
         *     },
         *     {
         *         "id": 209,
         *         "text": {
         *             "en": "Heavy Equipment, Mining",
         *             "id": "Alat Berat, Pertambangan"
         *         }
         *     }
         * ]
         */
        [AllowAnonymous]
        [HttpGet("industry/{surveyId}/{industries}")]
        public async Task<ActionResult<List<SurveyReport>>> GetIndustryReports(int surveyId, string industries)
        {
            if (!CheckBasicAuth(Request.Headers["Authorization"].ToString())) return Unauthorized();

            WebSurvey survey = _context.WebSurveys.Where(a => a.Id == surveyId && !a.IsDeleted).FirstOrDefault();

            if (survey == null) return NotFound();
            /*
            var q = from s in _context.WebSurveys
                    join p in _context.WebSurveyPages on s.Id equals p.SurveyId
                    join i in _context.WebSurveyItems on p.Id equals i.PageId
                    where i.GroupReport 
                    select new
                    {
                        i.Id,
                        i.RatingId
                    };
            */

            var q = from p in _context.WebSurveyPages 
                    join i in _context.WebSurveyItems on p.Id equals i.PageId
                    where i.GroupReport && p.SurveyId == surveyId
                    select new
                    {
                        i.Id,
                        i.RatingId
                    };
            var obj = q.FirstOrDefault();
            if (obj == null) return NotFound();

            List<SurveyReport> response = new List<SurveyReport>();

            foreach (string s in industries.Split(","))
            {

                try
                {
                    int ratingItemId = Int32.Parse(s);
                    IQueryable<string> q2;

                    if (ratingItemId == 0)
                    {
                        q2 = from rsp in _context.WebSurveyResponses
                             where rsp.ItemId == obj.Id
                             select rsp.Uuid;
                    }
                    else
                    {
                        q2 = from rsp in _context.WebSurveyResponses
                             where rsp.ItemId == obj.Id && rsp.RatingId == ratingItemId
                             select rsp.Uuid;

                    }
                    List<string> uuids = await q2.Distinct().ToListAsync();

                    var ratingItem = _context.WebSurveyRatingItems.Find(ratingItemId);

                    SurveyReport report = new SurveyReport();
                    report.SurveyId = survey.Id;
                    if(ratingItemId == 0)
                    {
                        report.Title = "Nilai rata-rata";
                    }
                    else
                    {
                        if (ratingItem != null)
                        {
                            report.Title = ratingItem.ItemTextID;
                        }
                    }

                    report.Description = survey.Description;

                    report.SurveyDate = DateTime.Now;
                    //report.Cover = await GetCoverItems(surveyId, uuid);
                    
                    report.Dimensions = new List<SurveyDimension>();



                    //
                    List<WebSurveyDimension> dimensions = _context.WebSurveyDimensions.Where(a => !a.IsDeleted && a.SurveyId == surveyId && a.Parent == 0).OrderBy(a => a.OrderNumber).ToList();
                    foreach (WebSurveyDimension dimension in dimensions)
                    {
                        SurveyDimension dim = new SurveyDimension();
                        dim.Id = dimension.Id;
                        dim.Title = dimension.ItemText;
                        dim.Description = dimension.Description;

                        if(uuids != null && uuids.Count > 0)
                        {
                            dim.Indicators = await GetIndicatorValuesByList(dimension.Id, uuids);

                            if (dim.Indicators.Count > 0)
                            {
                                IndicatorValue value = dim.Indicators[0];
                                value.Weight = 100;
                                if (value.Value < 3.00)
                                {
                                    dim.Description = "Anda perlu menyadari bahwa nilai ini penting serta mau berusaha untuk mendorong diri kita menjadi yang lebih baik.";
                                }
                                else if (value.Value <= 3.50)
                                {
                                    dim.Description = "Anda perlu meningkatkan komitmen diri di dalam melakukan implementasi nilai ini dengan lebih konsisten.";
                                }
                                else
                                {
                                    dim.Description = "Anda perlu mempertahankan perilaku kerja yang positif, dengan selalu menerapkan nilai ini secara konsisten";
                                }
                            }

                        }
                        report.Dimensions.Add(dim);
                    }

                    response.Add(report);

                }
                catch
                {
                    return BadRequest(new { error = "Error converting to integer." });
                }

            }

            return response;
        }


        /**
         * @api {post} /survey/owner POST survey owner
         * @apiVersion 1.0.0
         * @apiName PostSurveyOwner
         * @apiGroup Survey
         * @apiPermission Basic authentication
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "id": 0,
         *     "surveyId": 4,
         *     "userId": 35,
         *     "name": "Dita Kartika",
         *     "company": "PT Maju Terus",
         *     "email": "coba@coba.com",
         *     "phone": "0817-9090",
         *     "department": "HRD",
         *     "position": "Manager HRD",
         *     "groups": [
         *       {
         *         "text": "Atasan",
         *         "time": "2020-09-30T03:10:00.000Z"
         *       },
         *       {
         *         "text": "Bawahan",
         *         "time": "2020-09-30T03:10:00.000Z"
         *       }
         *     ]
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   [
         *       {
         *           "id": 1,
         *           "text": "Atasan",
         *           "url": "140bc382-1ffc-4274-aaa1-86ed39c00d29",
         *           "time": "2020-09-30T03:10:00.000Z"
         *       },
         *       {
         *           "id": 2,
         *           "text": "Bawahan",
         *           "url": "e0412a7a-1396-41bb-b5f4-954870df3a68",
         *           "time": "2020-09-30T03:10:00.000Z"
         *       }
         *   ]
         */
        [AllowAnonymous]
        [HttpPost("owner")]
        public async Task<ActionResult<List<GenericURL>>> PostSurveyOwner(SurveyOwnerInfo request)
        {
            if (!CheckBasicAuth(Request.Headers["Authorization"].ToString())) return Unauthorized();

            if (!SurveyExists(request.SurveyId))
            {
                return NotFound();
            }

            DateTime now = DateTime.Now;
            List<GenericURL> response = new List<GenericURL>();

            DateTime dt = DateTime.Now;
            string u = "";

            WebSurveyOwner owner = new WebSurveyOwner()
            {
                SurveyId = request.SurveyId,
                Name = request.Name,
                Company = request.Company,
                Email = request.Email,
                Phone = request.Phone,
                Department = request.Department,
                Position = request.Position,
                CreatedDate = now,
                CreatedBy = request.UserId,
                LastUpdated = now,
                LastUpdatedBy = request.UserId
            };

            try
            {
                _context.WebSurveyOwners.Add(owner);
                await _context.SaveChangesAsync();

                foreach(TextTime n in request.Groups)
                {
                    string uuid = GetUUID();
                    WebSurveyGroup group = new WebSurveyGroup()
                    {
                        OwnerId = owner.Id,
                        Name = n.Text,
                        Uuid = uuid,
                        ExpiredTime = n.Time,
                        CreatedDate = now,
                        CreatedBy = request.UserId,
                        LastUpdated = now,
                        LastUpdatedBy = request.UserId
                    };
                    _context.WebSurveyGroups.Add(group);
                    await _context.SaveChangesAsync();

                    response.Add(new GenericURL()
                    {
                        Id = group.Id,
                        Text = n.Text,
                        URL = uuid,
                        Time = n.Time
                    });

                    dt = dt < n.Time ? n.Time : dt;
                    u = u.Equals("") ? uuid : u;
                }
            }
            catch
            {
                return BadRequest(new { error = "Error updating database." });
            }

            await SendEmailNotificationToOwner(request.SurveyId, u, owner, dt);

            return response;
        }

        /**
         * @api {get} /Survey/lps/result/{directorates} GET survey LPS 
         * @apiVersion 1.0.0
         * @apiName GetDigitalSurveyResultLPS
         * @apiGroup Survey
         * @apiPermission Basic auth
         * 
         * @apiSuccessExample Success-Response:
         * {
         *     "total": 2,
         *     "groupName": [
         *         "IT, Telekomunikasi",
         *         "Industri Kebutuhan Rumah Tangga"
         *     ],
         *     "chart1": {
         *         "quadrant1": 100.0,
         *         "quadrant2": 0.0,
         *         "quadrant3": 0.0,
         *         "quadrant4": 0.0
         *     },
         *     "chart2": {
         *         "items": [
         *             {
         *                 "indicator": "Formulasi Strategi",
         *                 "values": [
         *                     3.125,
         *                     2.875,
         *                     3.68,
         *                     3.75
         *                 ]
         *             },
         *             {
         *                 "indicator": "Pemetaan Strategi Level Organisasi",
         *                 "values": [
         *                     2.5,
         *                     3.5,
         *                     3.68,
         *                     3.69
         *                 ]
         *             },
         *             {
         *                 "indicator": "Penyelarasan Organisasi",
         *                 "values": [
         *                     3.0,
         *                     2.3333333333333335,
         *                     3.52,
         *                     3.63
         *                 ]
         *             },
         *             {
         *                 "indicator": "Eksekusi Operasional",
         *                 "values": [
         *                     2.75,
         *                     3.25,
         *                     3.44,
         *                     3.75
         *                 ]
         *             },
         *             {
         *                 "indicator": "Pemantauan dan Penyelarasan Kembali",
         *                 "values": [
         *                     3.25,
         *                     3.5,
         *                     3.44,
         *                     3.56
         *                 ]
         *             }
         *         ],
         *         "legends": [
         *             "IT, Telekomunikasi",
         *             "Industri Kebutuhan Rumah Tangga",
         *             "SPEx2 Winners (2016)",
         *             "BSC Hall of Fame (2005)"
         *         ]
         *     },
         *     "chart3": [
         *         {
         *             "indicator": "Digital Differentiating Capability",
         *             "values": [
         *                 -0.98214285714285687,
         *                 0.0
         *             ]
         *         }
         *     ],
         *     "chart4": [
         *         {
         *             "indicator": "Execution-biased Systems",
         *             "values": [
         *                 -1.220238095238094,
         *                 0.0
         *             ]
         *         },
         *         {
         *             "indicator": "Empowered Structure",
         *             "values": [
         *                 -0.62499999999999933,
         *                 0.0
         *             ]
         *         },
         *         {
         *             "indicator": "Entrepreneurial People",
         *             "values": [
         *                 -0.62499999999999933,
         *                 0.0
         *             ]
         *         },
         *         {
         *             "indicator": "Adhocracy Culture",
         *             "values": [
         *                 -0.73660714285714235,
         *                 0.0
         *             ]
         *         },
         *         {
         *             "indicator": "Ambidextrous Leadership",
         *             "values": [
         *                 -1.0714285714285707,
         *                 0.0
         *             ]
         *         }
         *     ]
         * }
         */
        [HttpGet("lps/result/{directorates}")]
        public async Task<ActionResult<SurveyResult>> GetDigitalSurveyResultLPS(string directorates)
        {
            if (!CheckBasicAuth(Request.Headers["Authorization"].ToString())) return Unauthorized();

            int surveyId = _context.WebSurveys.Where(a => a.Title.ToLower().Contains("lps")).Select(a => a.Id).FirstOrDefault();

            if (surveyId == 0) return NotFound();

            SurveyResult result = await GetDigitalGroupReport(surveyId, directorates);

            if (result == null) BadRequest(new { error = "Error converting to integer." });

            return result;
        }


        /**
         * @api {get} /Survey/digital/result/{industries} GET survey digital by industries 
         * @apiVersion 1.0.0
         * @apiName GetDigitalSurveyResultByIndustries
         * @apiGroup Survey
         * @apiPermission Basic auth
         * 
         * @apiSuccessExample Success-Response:
         * {
         *     "total": 2,
         *     "groupName": [
         *         "IT, Telekomunikasi",
         *         "Industri Kebutuhan Rumah Tangga"
         *     ],
         *     "chart1": {
         *         "quadrant1": 100.0,
         *         "quadrant2": 0.0,
         *         "quadrant3": 0.0,
         *         "quadrant4": 0.0
         *     },
         *     "chart2": {
         *         "items": [
         *             {
         *                 "indicator": "Formulasi Strategi",
         *                 "values": [
         *                     3.125,
         *                     2.875,
         *                     3.68,
         *                     3.75
         *                 ]
         *             },
         *             {
         *                 "indicator": "Pemetaan Strategi Level Organisasi",
         *                 "values": [
         *                     2.5,
         *                     3.5,
         *                     3.68,
         *                     3.69
         *                 ]
         *             },
         *             {
         *                 "indicator": "Penyelarasan Organisasi",
         *                 "values": [
         *                     3.0,
         *                     2.3333333333333335,
         *                     3.52,
         *                     3.63
         *                 ]
         *             },
         *             {
         *                 "indicator": "Eksekusi Operasional",
         *                 "values": [
         *                     2.75,
         *                     3.25,
         *                     3.44,
         *                     3.75
         *                 ]
         *             },
         *             {
         *                 "indicator": "Pemantauan dan Penyelarasan Kembali",
         *                 "values": [
         *                     3.25,
         *                     3.5,
         *                     3.44,
         *                     3.56
         *                 ]
         *             }
         *         ],
         *         "legends": [
         *             "IT, Telekomunikasi",
         *             "Industri Kebutuhan Rumah Tangga",
         *             "SPEx2 Winners (2016)",
         *             "BSC Hall of Fame (2005)"
         *         ]
         *     },
         *     "chart3": [
         *         {
         *             "indicator": "Digital Differentiating Capability",
         *             "values": [
         *                 -0.98214285714285687,
         *                 0.0
         *             ]
         *         }
         *     ],
         *     "chart4": [
         *         {
         *             "indicator": "Execution-biased Systems",
         *             "values": [
         *                 -1.220238095238094,
         *                 0.0
         *             ]
         *         },
         *         {
         *             "indicator": "Empowered Structure",
         *             "values": [
         *                 -0.62499999999999933,
         *                 0.0
         *             ]
         *         },
         *         {
         *             "indicator": "Entrepreneurial People",
         *             "values": [
         *                 -0.62499999999999933,
         *                 0.0
         *             ]
         *         },
         *         {
         *             "indicator": "Adhocracy Culture",
         *             "values": [
         *                 -0.73660714285714235,
         *                 0.0
         *             ]
         *         },
         *         {
         *             "indicator": "Ambidextrous Leadership",
         *             "values": [
         *                 -1.0714285714285707,
         *                 0.0
         *             ]
         *         }
         *     ]
         * }
         */
        [HttpGet("digital/result/{industries}")]
        public async Task<ActionResult<SurveyResult>> GetDigitalSurveyResultByIndustries(string industries)
        {
            if (!CheckBasicAuth(Request.Headers["Authorization"].ToString())) return Unauthorized();


            int surveyId = _context.WebSurveys.Where(a => a.Title.StartsWith("Digital")).Select(a => a.Id).FirstOrDefault();
            if (surveyId == 0) return NotFound();

            SurveyResult result = await GetDigitalGroupReport(surveyId, industries);

            if(result == null) BadRequest(new { error = "Error converting to integer." });

            return result;
        }


        [HttpGet("lps/result/individual/{uuid}")]
        public async Task<ActionResult<SurveyResult>> GetLPSSurveyResultByUUIDs(string uuid)
        {
            if (!CheckBasicAuth(Request.Headers["Authorization"].ToString())) return Unauthorized();

            int surveyId = _context.WebSurveys.Where(a => a.Title.ToLower().Contains("lps")).Select(a => a.Id).FirstOrDefault();
            if (surveyId == 0) return NotFound();

            SurveyResult result = await GetDigitalIndividualReport(surveyId, uuid);
            if (result == null) BadRequest(new { error = "Error converting to integer." });

            return result;
        }

        [HttpGet("digital/result/individual/{uuid}")]
        public async Task<ActionResult<SurveyResult>> GetDigitalSurveyResultByUUIDs(string uuid)
        {
            if (!CheckBasicAuth(Request.Headers["Authorization"].ToString())) return Unauthorized();


            int surveyId = _context.WebSurveys.Where(a => a.Title.StartsWith("Digital")).Select(a => a.Id).FirstOrDefault();
            if (surveyId == 0) return NotFound();

            /*
            var q = from p in _context.WebSurveyPages
                    join i in _context.WebSurveyItems on p.Id equals i.PageId
                    where i.GroupReport && p.SurveyId == surveyId
                    select new
                    {
                        i.Id,
                        i.RatingId
                    };
            var obj = q.FirstOrDefault();
            if (obj == null) return NotFound();
            */

            SurveyResult result = await GetDigitalIndividualReport(surveyId, uuid);
            if (result == null) BadRequest(new { error = "Error converting to integer." });

            return result;
        }

        /**
         * @api {get} /Survey/groups/stellar/{surveyId} GET groups
         * @apiVersion 1.0.0
         * @apiName GetSurveyGroups
         * @apiGroup Survey
         * @apiPermission Basic auth
         * 
         * @apiSuccessExample Success-Response:
         * {
         *     "id": 11,
         *     "title": "Employee Engagement & Digital Transformation Readiness Assessment",
         *     "groups": [
         *         {
         *             "id": 136,
         *             "text": "PT Maju Terus",
         *             "url": "27c89bb0-ec7e-49c7-8cb7-f631c9a5af8c",
         *             "time": "2021-08-25T09:02:13.71"
         *         }
         *     ]
         * }
         */
        [AllowAnonymous]
        [HttpGet("groups/{nm}/{surveyId}")]
        public async Task<ActionResult<SurveyGroupsData>> GetSurveyGroups(string nm, int surveyId)
        {
            if (!CheckBasicAuth(Request.Headers["Authorization"].ToString())) return Unauthorized();

            WebSurvey survey = _context.WebSurveys.Where(a => a.Id == surveyId && !a.IsDeleted).FirstOrDefault();
            if (survey == null) return NotFound(new { error = "Survey not found. Please check surveyId" });

            WebSurveyOwner owner = await GetSurveyOwner(surveyId, nm, DateTime.Now, 0);
            if (owner == null) return BadRequest();

            List<GenericURL> groups = await GetCurrentGroups(owner.Id);
            return new SurveyGroupsData()
            {
                Id = survey.Id,
                Title = survey.Title,
                Groups = groups
            };
        }

        /**
         * @api {post} /survey/groups/stellar POST groups 
         * @apiVersion 1.0.0
         * @apiName PostSurveyGroups
         * @apiGroup Survey
         * @apiPermission Basic authentication
         * @apiParam {Number} surveyId         Id dari survey yang dipilih oleh survey
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "surveyId": 11,
         *     "groups": [
         *       {
         *         "text": "PT Maju Terus",
         *         "time": "2021-08-25T09:02:13.710Z"
         *       }
         *     ]
         *   }
         * 
         */
        [AllowAnonymous]
        [HttpPost("groups/{nm}")]
        public async Task<ActionResult<SurveyGroupsData>> PostSurveyGroups(string nm, PostGroupInfo request)
        {
            if (!CheckBasicAuth(Request.Headers["Authorization"].ToString())) return Unauthorized();

            DateTime now = DateTime.Now;
            WebSurveyOwner owner = await GetSurveyOwner(request.SurveyId, nm, now, 0);
            if (owner == null) return BadRequest();

            
            List<GenericURL> curGroups = await GetCurrentGroups(owner.Id);
            /*
            foreach(GenericURL g in curGroups)
            {
                if(request.Groups.Where(a => a.Text.Equals(g)).FirstOrDefault() == null)
                {
                    WebSurveyGroup group = _context.WebSurveyGroups.Find(g.Id);
                    if(group != null)
                    {
                        _context.WebSurveyGroups.Remove(group);
                    }
                }
            }
            */

            foreach(TextTime t in request.Groups)
            {
                if(curGroups.Where(a => a.Text.Equals(t.Text.Trim())).FirstOrDefault() == null)
                {
                    WebSurveyGroup newGroup = new WebSurveyGroup()
                    {
                        OwnerId = owner.Id,
                        Name = t.Text,
                        Uuid = GetUUID(),
                        ExpiredTime = t.Time,
                        CreatedDate = now,
                        CreatedBy = 0,
                        LastUpdated = now,
                        LastUpdatedBy = 0,
                        IsDeleted = false,
                        DeletedBy = 0
                    };
                    _context.WebSurveyGroups.Add(newGroup);
                }
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }


        private async Task<List<GenericURL>> GetCurrentGroups(int ownerId)
        {
            var query = from g in _context.WebSurveyGroups
                        where g.OwnerId == ownerId
                        select new GenericURL()
                        {
                            Id = g.Id,
                            Text = g.Name,
                            URL = g.Uuid,
                            Time = g.ExpiredTime
                        };
            return await query.ToListAsync();
        }

        private async Task<WebSurveyOwner> GetSurveyOwner(int surveyId, string startName, DateTime now, int userId)
        {
            WebSurveyOwner owner = _context.WebSurveyOwners.Where(a => a.Name.StartsWith(startName) && a.SurveyId == surveyId && !a.IsDeleted).FirstOrDefault();
            if(owner == null)
            {
                owner = new WebSurveyOwner()
                {
                    SurveyId = surveyId,
                    Name = startName,
                    Company = "GML Performance Consulting",
                    Email = "gml@gmlperformance.co.id",
                    Phone = "",
                    Department = "",
                    Position = "",
                    CreatedDate = now,
                    CreatedBy = userId,
                    LastUpdated = now,
                    LastUpdatedBy = userId,
                    IsDeleted = false,
                    DeletedBy = 0
                };

                _context.WebSurveyOwners.Add(owner);
                await _context.SaveChangesAsync();
            }

            return owner;
        }

        private async Task<SurveyResult> GetDigitalGroupReportByGroupUUIDRev(int surveyId, string groupUUID, string groupName, List<string> indUuids)
        {
            double cutoff = 2.5d;

            SurveyResult response = new SurveyResult();
            response.Total = _context.WebSurveyResponses.Where(a => a.GroupUUID.Equals(groupUUID)).Select(a => a.Uuid).Distinct().Count();
            response.GroupName = new List<string>();

            int countQ1 = 0;
            int countQ2 = 0;
            int countQ3 = 0;
            int countQ4 = 0;

            string itemText1 = "";
            string itemText2 = "";
            string itemText3 = "";
            string itemText4 = "";
            string itemText5 = "";

            List<double> bar1data = new List<double>();
            List<double> bar2data = new List<double>();
            List<double> bar3data = new List<double>();
            List<double> bar4data = new List<double>();
            List<double> bar5data = new List<double>();

            string chartText1 = "";
            string chartText2 = "";
            string chartText3 = "";
            string chartText4 = "";
            string chartText5 = "";
            string chartText6 = "";

            List<double> chart1data = new List<double>();
            List<double> chart2data = new List<double>();
            List<double> chart3data = new List<double>();
            List<double> chart4data = new List<double>();
            List<double> chart5data = new List<double>();
            List<double> chart6data = new List<double>();

            string nm = "";
            try
            {
                /*                var nq = from item in _context.WebSurveyItems
                                         join p in _context.WebSurveyPages on item.PageId equals p.Id
                                         join resp in _context.WebSurveyResponses on item.Id equals resp.ItemId
                                         where p.SurveyId == surveyId && item.RatingId == 1 && resp.GroupUUID.Equals(groupUUID.Trim()) && item.ItemTextId.Contains("Nama")
                                         orderby item.Id
                                         select new GenericInfo()
                                         {
                                             Id = resp.Id,
                                             Text = resp.AnswerText
                                         };
                                List<GenericInfo> txts = await nq.ToListAsync();
                                foreach (GenericInfo txt in txts)
                                {
                                    response.GroupName.Add(txt.Text);
                                }

                */
                response.GroupName.Add(groupName);
                nm = groupName;

                /*                if (txts.Count() > 1) nm = txts[1].Text;
                                else if (txts.Count() > 0) nm = txts[0].Text;
                */

                List<double> bar1 = new List<double>();
                List<double> bar2 = new List<double>();
                List<double> bar3 = new List<double>();
                List<double> bar4 = new List<double>();
                List<double> bar5 = new List<double>();

                List<double> chart1 = new List<double>();
                List<double> chart2 = new List<double>();
                List<double> chart3 = new List<double>();
                List<double> chart4 = new List<double>();
                List<double> chart5 = new List<double>();
                List<double> chart6 = new List<double>();


                // For each UUID
                List<List<ResultItem>> inditems = new List<List<ResultItem>>();
                foreach (string iuuid in indUuids)
                {
                    inditems.Add(await GetQuadrantData(surveyId, iuuid));
                }

                foreach (List<ResultItem> item in inditems)
                {
                    if (item.Count() >= 20)
                    {
                        ResultItem item0 = item[6];
                        ResultItem item1 = item[7];
                        ResultItem item2 = item[8];
                        ResultItem item3 = item[9];
                        ResultItem item4 = item[10];
                        ResultItem item5 = item[11];

                        double d1 = item0.Val;
                        double d2 = (item1.Val + item2.Val + item3.Val + item4.Val + item5.Val) / 5.0d;

                        if (d1 >= cutoff)
                        {
                            if (d2 >= cutoff)
                            {
                                countQ1++;
                            }
                            else
                            {
                                countQ4++;
                            }
                        }
                        else
                        {
                            if (d2 >= cutoff)
                            {
                                countQ2++;
                            }
                            else
                            {
                                countQ3++;
                            }
                        }
                    }

                }

                // Group UUID
                List<ResultItem> nitems = await GetGroupQuadrantData(surveyId, groupUUID);

                // Quadrant data

                foreach (ResultItem nitem in nitems)
                {
                    // Chart3
                    if (nitem.OrderNumber == 7)
                    {
                        chartText1 = nitem.ItemText;
                        chart1.Add(nitem.Val);
                    }
                    else if (nitem.OrderNumber == 8)
                    {
                        chartText2 = nitem.ItemText;
                        chart2.Add(nitem.Val);
                    }
                    else if (nitem.OrderNumber == 9)
                    {
                        chartText3 = nitem.ItemText;
                        chart3.Add(nitem.Val);
                    }
                    else if (nitem.OrderNumber == 10)
                    {
                        chartText4 = nitem.ItemText;
                        chart4.Add(nitem.Val);
                    }
                    else if (nitem.OrderNumber == 11)
                    {
                        chartText5 = nitem.ItemText;
                        chart5.Add(nitem.Val);
                    }
                    else if (nitem.OrderNumber == 12)
                    {
                        chartText6 = nitem.ItemText;
                        chart6.Add(nitem.Val);
                    }
                    // bar chart data
                    else if (nitem.OrderNumber == 13)
                    {
                        itemText1 = nitem.ItemText;
                        bar1.Add(nitem.Val);
                    }
                    else if (nitem.OrderNumber == 14)
                    {
                        itemText2 = nitem.ItemText;
                        bar2.Add(nitem.Val);
                    }
                    else if (nitem.OrderNumber == 15)
                    {
                        itemText3 = nitem.ItemText;
                        bar3.Add(nitem.Val);
                    }
                    else if (nitem.OrderNumber == 16)
                    {
                        itemText4 = nitem.ItemText;
                        bar4.Add(nitem.Val);
                    }
                    else if (nitem.OrderNumber == 17)
                    {
                        itemText5 = nitem.ItemText;
                        bar5.Add(nitem.Val);
                    }
                }

                // Chart 3
                if (chart1.Count() == 0)
                {
                    chart1data.Add(0.0d);
                }
                else
                {
                    chart1data.Add(chart1.Average());
                }

                if (chart2.Count() == 0)
                {
                    chart2data.Add(0.0d);
                }
                else
                {
                    chart2data.Add(chart2.Average());
                }

                if (chart3.Count() == 0)
                {
                    chart3data.Add(0.0d);
                }
                else
                {
                    chart3data.Add(chart3.Average());
                }

                if (chart4.Count() == 0)
                {
                    chart4data.Add(0.0d);
                }
                else
                {
                    chart4data.Add(chart4.Average());
                }

                if (chart5.Count() == 0)
                {
                    chart5data.Add(0.0d);
                }
                else
                {
                    chart5data.Add(chart5.Average());
                }

                if (chart6.Count() == 0)
                {
                    chart6data.Add(0.0d);
                }
                else
                {
                    chart6data.Add(chart6.Average());
                }


                // Chart 2
                if (bar1.Count() == 0)
                {
                    bar1data.Add(0.0d);
                }
                else
                {
                    bar1data.Add(bar1.Average());
                }

                if (bar2.Count() == 0)
                {
                    bar2data.Add(0.0d);
                }
                else
                {
                    bar2data.Add(bar2.Average());
                }

                if (bar3.Count() == 0)
                {
                    bar3data.Add(0.0d);
                }
                else
                {
                    bar3data.Add(bar3.Average());
                }

                if (bar4.Count() == 0)
                {
                    bar4data.Add(0.0d);
                }
                else
                {
                    bar4data.Add(bar4.Average());
                }

                if (bar5.Count() == 0)
                {
                    bar5data.Add(0.0d);
                }
                else
                {
                    bar5data.Add(bar5.Average());
                }
            }
            catch
            {
                return null;
            }

            response.Chart1 = new DigitalQuadrantData();

            response.Chart1.Quadrant1 = Convert.ToDouble(countQ1) / (countQ1 + countQ2 + countQ3 + countQ4) * 100;
            response.Chart1.Quadrant2 = Convert.ToDouble(countQ2) / (countQ1 + countQ2 + countQ3 + countQ4) * 100;
            response.Chart1.Quadrant3 = Convert.ToDouble(countQ3) / (countQ1 + countQ2 + countQ3 + countQ4) * 100;
            response.Chart1.Quadrant4 = Convert.ToDouble(countQ4) / (countQ1 + countQ2 + countQ3 + countQ4) * 100;

            response.Chart2 = new DigitalBarChartData();
            response.Chart2.Items = new List<DigitalBarItem>();
            response.Chart2.Legends = new List<string>();
            response.Chart2.Legends.Add(nm);
            response.Chart2.Legends.AddRange(new[] { "SPEx2 Winners (2016)", "BSC Hall of Fame (2005)" });

            response.Chart2.Items.Add(CreateBarItem(itemText1, bar1data, 3.68d, 3.75d));
            response.Chart2.Items.Add(CreateBarItem(itemText2, bar2data, 3.68d, 3.69d));
            response.Chart2.Items.Add(CreateBarItem(itemText3, bar3data, 3.52d, 3.63d));
            response.Chart2.Items.Add(CreateBarItem(itemText4, bar4data, 3.44d, 3.75d));
            response.Chart2.Items.Add(CreateBarItem(itemText5, bar5data, 3.44d, 3.56d));

            response.Chart3 = new List<DigitalBarItem>();
            response.Chart3.Add(new DigitalBarItem()
            {
                Indicator = chartText1,
                Values = new List<double>(new[] { 0.0d, chart1data.Average() })
            });

            response.Chart4 = new List<DigitalBarItem>();
            response.Chart4.Add(new DigitalBarItem()
            {
                Indicator = chartText2,
                Values = new List<double>(new[] { 0.0d, chart2data.Average() })
            });
            response.Chart4.Add(new DigitalBarItem()
            {
                Indicator = chartText3,
                Values = new List<double>(new[] { 0.0d, chart3data.Average() })
            });
            response.Chart4.Add(new DigitalBarItem()
            {
                Indicator = chartText4,
                Values = new List<double>(new[] { 0.0d, chart4data.Average() })
            });
            response.Chart4.Add(new DigitalBarItem()
            {
                Indicator = chartText5,
                Values = new List<double>(new[] { 0.0d, chart5data.Average() })
            });
            response.Chart4.Add(new DigitalBarItem()
            {
                Indicator = chartText6,
                Values = new List<double>(new[] { 0.0d, chart6data.Average() })
            });
            return response;

        }


        private async Task<SurveyResult> GetDigitalGroupReportByGroupUUID(int surveyId, string groupUUID, string groupName, List<string> indUuids)
        {
            double cutoff = 2.8d;
            
            SurveyResult response = new SurveyResult();
            response.Total = _context.WebSurveyResponses.Where(a => a.GroupUUID.Equals(groupUUID)).Select(a => a.Uuid).Distinct().Count(); 
            response.GroupName = new List<string>();

            int countQ1 = 0;
            int countQ2 = 0;
            int countQ3 = 0;
            int countQ4 = 0;

            string itemText1 = "";
            string itemText2 = "";
            string itemText3 = "";
            string itemText4 = "";
            string itemText5 = "";

            List<double> bar1data = new List<double>();
            List<double> bar2data = new List<double>();
            List<double> bar3data = new List<double>();
            List<double> bar4data = new List<double>();
            List<double> bar5data = new List<double>();

            string chartText1 = "";
            string chartText2 = "";
            string chartText3 = "";
            string chartText4 = "";
            string chartText5 = "";
            string chartText6 = "";

            List<double> chart1data = new List<double>();
            List<double> chart2data = new List<double>();
            List<double> chart3data = new List<double>();
            List<double> chart4data = new List<double>();
            List<double> chart5data = new List<double>();
            List<double> chart6data = new List<double>();

            string nm = "";
            try
            {
/*                var nq = from item in _context.WebSurveyItems
                         join p in _context.WebSurveyPages on item.PageId equals p.Id
                         join resp in _context.WebSurveyResponses on item.Id equals resp.ItemId
                         where p.SurveyId == surveyId && item.RatingId == 1 && resp.GroupUUID.Equals(groupUUID.Trim()) && item.ItemTextId.Contains("Nama")
                         orderby item.Id
                         select new GenericInfo()
                         {
                             Id = resp.Id,
                             Text = resp.AnswerText
                         };
                List<GenericInfo> txts = await nq.ToListAsync();
                foreach (GenericInfo txt in txts)
                {
                    response.GroupName.Add(txt.Text);
                }

*/                
                response.GroupName.Add(groupName);
                nm = groupName;

/*                if (txts.Count() > 1) nm = txts[1].Text;
                else if (txts.Count() > 0) nm = txts[0].Text;
*/

                List<double> bar1 = new List<double>();
                List<double> bar2 = new List<double>();
                List<double> bar3 = new List<double>();
                List<double> bar4 = new List<double>();
                List<double> bar5 = new List<double>();

                List<double> chart1 = new List<double>();
                List<double> chart2 = new List<double>();
                List<double> chart3 = new List<double>();
                List<double> chart4 = new List<double>();
                List<double> chart5 = new List<double>();
                List<double> chart6 = new List<double>();


                // For each UUID
                List<List<ResultItem>> inditems = new List<List<ResultItem>>();
                foreach(string iuuid in indUuids)
                {
                    inditems.Add(await GetQuadrantData(surveyId, iuuid));
                }

                foreach(List<ResultItem> item in inditems)
                {
                    if (item.Count() >= 20)
                    {
                        ResultItem item0 = item[6];
                        ResultItem item1 = item[7];
                        ResultItem item2 = item[8];
                        ResultItem item3 = item[9];
                        ResultItem item4 = item[10];
                        ResultItem item5 = item[11];

                        double d1 = item0.Val;
                        double d2 = (item1.Val + item2.Val + item3.Val + item4.Val + item5.Val) / 5.0d;

                        if (d1 >= cutoff)
                        {
                            if (d2 >= cutoff)
                            {
                                countQ1++;
                            }
                            else
                            {
                                countQ4++;
                            }
                        }
                        else
                        {
                            if (d2 >= cutoff)
                            {
                                countQ2++;
                            }
                            else
                            {
                                countQ3++;
                            }
                        }
                    }

                }

                // Group UUID
                List<ResultItem> nitems = await GetGroupQuadrantData(surveyId, groupUUID);
                
                // Quadrant data

                foreach (ResultItem nitem in nitems)
                {
                    // Chart3
                    if (nitem.OrderNumber == 7)
                    {
                        chartText1 = nitem.ItemText;
                        chart1.Add(nitem.Val);
                    }
                    else if (nitem.OrderNumber == 8)
                    {
                        chartText2 = nitem.ItemText;
                        chart2.Add(nitem.Val);
                    }
                    else if (nitem.OrderNumber == 9)
                    {
                        chartText3 = nitem.ItemText;
                        chart3.Add(nitem.Val);
                    }
                    else if (nitem.OrderNumber == 10)
                    {
                        chartText4 = nitem.ItemText;
                        chart4.Add(nitem.Val);
                    }
                    else if (nitem.OrderNumber == 11)
                    {
                        chartText5 = nitem.ItemText;
                        chart5.Add(nitem.Val);
                    }
                    else if (nitem.OrderNumber == 12)
                    {
                        chartText6 = nitem.ItemText;
                        chart6.Add(nitem.Val);
                    }
                    // bar chart data
                    else if (nitem.OrderNumber == 13)
                    {
                        itemText1 = nitem.ItemText;
                        bar1.Add(nitem.Val);
                    }
                    else if (nitem.OrderNumber == 14)
                    {
                        itemText2 = nitem.ItemText;
                        bar2.Add(nitem.Val);
                    }
                    else if (nitem.OrderNumber == 15)
                    {
                        itemText3 = nitem.ItemText;
                        bar3.Add(nitem.Val);
                    }
                    else if (nitem.OrderNumber == 16)
                    {
                        itemText4 = nitem.ItemText;
                        bar4.Add(nitem.Val);
                    }
                    else if (nitem.OrderNumber == 17)
                    {
                        itemText5 = nitem.ItemText;
                        bar5.Add(nitem.Val);
                    }
                }

                // Chart 3
                if (chart1.Count() == 0)
                {
                    chart1data.Add(0.0d);
                }
                else
                {
                    chart1data.Add(chart1.Average());
                }

                if (chart2.Count() == 0)
                {
                    chart2data.Add(0.0d);
                }
                else
                {
                    chart2data.Add(chart2.Average());
                }

                if (chart3.Count() == 0)
                {
                    chart3data.Add(0.0d);
                }
                else
                {
                    chart3data.Add(chart3.Average());
                }

                if (chart4.Count() == 0)
                {
                    chart4data.Add(0.0d);
                }
                else
                {
                    chart4data.Add(chart4.Average());
                }

                if (chart5.Count() == 0)
                {
                    chart5data.Add(0.0d);
                }
                else
                {
                    chart5data.Add(chart5.Average());
                }

                if (chart6.Count() == 0)
                {
                    chart6data.Add(0.0d);
                }
                else
                {
                    chart6data.Add(chart6.Average());
                }


                // Chart 2
                if (bar1.Count() == 0)
                {
                    bar1data.Add(0.0d);
                }
                else
                {
                    bar1data.Add(bar1.Average());
                }

                if (bar2.Count() == 0)
                {
                    bar2data.Add(0.0d);
                }
                else
                {
                    bar2data.Add(bar2.Average());
                }

                if (bar3.Count() == 0)
                {
                    bar3data.Add(0.0d);
                }
                else
                {
                    bar3data.Add(bar3.Average());
                }

                if (bar4.Count() == 0)
                {
                    bar4data.Add(0.0d);
                }
                else
                {
                    bar4data.Add(bar4.Average());
                }

                if (bar5.Count() == 0)
                {
                    bar5data.Add(0.0d);
                }
                else
                {
                    bar5data.Add(bar5.Average());
                }
            }
            catch
            {
                return null;
            }

            response.Chart1 = new DigitalQuadrantData();

            response.Chart1.Quadrant1 = Convert.ToDouble(countQ1) / (countQ1 + countQ2 + countQ3 + countQ4) * 100;
            response.Chart1.Quadrant2 = Convert.ToDouble(countQ2) / (countQ1 + countQ2 + countQ3 + countQ4) * 100;
            response.Chart1.Quadrant3 = Convert.ToDouble(countQ3) / (countQ1 + countQ2 + countQ3 + countQ4) * 100;
            response.Chart1.Quadrant4 = Convert.ToDouble(countQ4) / (countQ1 + countQ2 + countQ3 + countQ4) * 100;

            response.Chart2 = new DigitalBarChartData();
            response.Chart2.Items = new List<DigitalBarItem>();
            response.Chart2.Legends = new List<string>();
            response.Chart2.Legends.Add(nm);
            response.Chart2.Legends.AddRange(new[] { "SPEx2 Winners (2016)", "BSC Hall of Fame (2005)" });

            response.Chart2.Items.Add(CreateBarItem(itemText1, bar1data, 3.68d, 3.75d));
            response.Chart2.Items.Add(CreateBarItem(itemText2, bar2data, 3.68d, 3.69d));
            response.Chart2.Items.Add(CreateBarItem(itemText3, bar3data, 3.52d, 3.63d));
            response.Chart2.Items.Add(CreateBarItem(itemText4, bar4data, 3.44d, 3.75d));
            response.Chart2.Items.Add(CreateBarItem(itemText5, bar5data, 3.44d, 3.56d));

            response.Chart3 = new List<DigitalBarItem>();
            response.Chart3.Add(new DigitalBarItem()
            {
                Indicator = chartText1,
                Values = GetRange(chart1data.Average())
            });

            response.Chart4 = new List<DigitalBarItem>();
            response.Chart4.Add(new DigitalBarItem()
            {
                Indicator = chartText2,
                Values = GetRange(chart2data.Average())
            });
            response.Chart4.Add(new DigitalBarItem()
            {
                Indicator = chartText3,
                Values = GetRange(chart3data.Average())
            });
            response.Chart4.Add(new DigitalBarItem()
            {
                Indicator = chartText4,
                Values = GetRange(chart4data.Average())
            });
            response.Chart4.Add(new DigitalBarItem()
            {
                Indicator = chartText5,
                Values = GetRange(chart5data.Average())
            });
            response.Chart4.Add(new DigitalBarItem()
            {
                Indicator = chartText6,
                Values = GetRange(chart6data.Average())
            });
            return response;

        }
        private async Task<SurveyResult> GetDigitalGroupReport(int surveyId, string industries)
        {
            double cutoff = 2.8d;

            var q = from p in _context.WebSurveyPages
                    join i in _context.WebSurveyItems on p.Id equals i.PageId
                    where i.GroupReport && p.SurveyId == surveyId
                    select new
                    {
                        i.Id,
                        i.RatingId
                    };
            var obj = q.FirstOrDefault();
            if (obj == null) return null;

            SurveyResult response = new SurveyResult();
            response.Total = 0;
            response.GroupName = new List<string>();

            int countQ1 = 0;
            int countQ2 = 0;
            int countQ3 = 0;
            int countQ4 = 0;

            string itemText1 = "";
            string itemText2 = "";
            string itemText3 = "";
            string itemText4 = "";
            string itemText5 = "";

            List<double> bar1data = new List<double>();
            List<double> bar2data = new List<double>();
            List<double> bar3data = new List<double>();
            List<double> bar4data = new List<double>();
            List<double> bar5data = new List<double>();

            string chartText1 = "";
            string chartText2 = "";
            string chartText3 = "";
            string chartText4 = "";
            string chartText5 = "";
            string chartText6 = "";

            List<double> chart1data = new List<double>();
            List<double> chart2data = new List<double>();
            List<double> chart3data = new List<double>();
            List<double> chart4data = new List<double>();
            List<double> chart5data = new List<double>();
            List<double> chart6data = new List<double>();

            foreach (string s in industries.Split(","))
            {

                try
                {
                    int ratingItemId = Int32.Parse(s);
                    IQueryable<string> q2;

                    if (ratingItemId == 0)
                    {
                        q2 = from rsp in _context.WebSurveyResponses
                             where rsp.ItemId == obj.Id
                             select rsp.Uuid;

                    }
                    else
                    {
                        q2 = from rsp in _context.WebSurveyResponses
                             where rsp.ItemId == obj.Id && rsp.RatingId == ratingItemId
                             select rsp.Uuid;
                    }
                    List<string> uuids = await q2.Distinct().ToListAsync();

                    var ratingItem = _context.WebSurveyRatingItems.Find(ratingItemId);

                    if (ratingItemId == 0)
                    {
                        response.GroupName.Add("Nilai rata-rata");
                        response.Total = uuids.Count();
                    }
                    else
                    {
                        if (ratingItem != null)
                        {
                            response.GroupName.Add(ratingItem.ItemTextID);
                            response.Total += uuids.Count();
                        }
                    }

                    List<double> bar1 = new List<double>();
                    List<double> bar2 = new List<double>();
                    List<double> bar3 = new List<double>();
                    List<double> bar4 = new List<double>();
                    List<double> bar5 = new List<double>();

                    List<double> chart1 = new List<double>();
                    List<double> chart2 = new List<double>();
                    List<double> chart3 = new List<double>();
                    List<double> chart4 = new List<double>();
                    List<double> chart5 = new List<double>();
                    List<double> chart6 = new List<double>();

                    foreach (string uuid in uuids)
                    {
                        List<ResultItem> items = await GetQuadrantData(surveyId, uuid);

                        // Quadrant data
                        if (items.Count() > 6)
                        {
                            ResultItem item0 = items[0];
                            ResultItem item1 = items[1];
                            ResultItem item2 = items[2];
                            ResultItem item3 = items[3];
                            ResultItem item4 = items[4];
                            ResultItem item5 = items[5];

                            double d1 = item0.Val;
                            double d2 = (item1.Val + item2.Val + item3.Val + item4.Val + item5.Val) / 5.0d;

                            if (d1 >= cutoff)
                            {
                                if (d2 >= cutoff)
                                {
                                    countQ1++;
                                }
                                else
                                {
                                    countQ4++;
                                }
                            }
                            else
                            {
                                if (d2 >= cutoff)
                                {
                                    countQ2++;
                                }
                                else
                                {
                                    countQ3++;
                                }
                            }
                        }

                        foreach (ResultItem item in items)
                        {
                            // Chart3
                            if (item.OrderNumber == 1)
                            {
                                chartText1 = item.ItemText;
                                chart1.Add(item.Val);
                            }
                            else if (item.OrderNumber == 2)
                            {
                                chartText2 = item.ItemText;
                                chart2.Add(item.Val);
                            }
                            else if (item.OrderNumber == 3)
                            {
                                chartText3 = item.ItemText;
                                chart3.Add(item.Val);
                            }
                            else if (item.OrderNumber == 4)
                            {
                                chartText4 = item.ItemText;
                                chart4.Add(item.Val);
                            }
                            else if (item.OrderNumber == 5)
                            {
                                chartText5 = item.ItemText;
                                chart5.Add(item.Val);
                            }
                            else if (item.OrderNumber == 6)
                            {
                                chartText6 = item.ItemText;
                                chart6.Add(item.Val);
                            }
                            // bar chart data
                            else if (item.OrderNumber == 10)
                            {
                                itemText1 = item.ItemText;
                                bar1.Add(item.Val);
                            }
                            else if (item.OrderNumber == 11)
                            {
                                itemText2 = item.ItemText;
                                bar2.Add(item.Val);
                            }
                            else if (item.OrderNumber == 12)
                            {
                                itemText3 = item.ItemText;
                                bar3.Add(item.Val);
                            }
                            else if (item.OrderNumber == 13)
                            {
                                itemText4 = item.ItemText;
                                bar4.Add(item.Val);
                            }
                            else if (item.OrderNumber == 14)
                            {
                                itemText5 = item.ItemText;
                                bar5.Add(item.Val);
                            }
                        }
                    }

                    // Chart 3
                    if (chart1.Count() == 0)
                    {
                        chart1data.Add(0.0d);
                    }
                    else
                    {
                        chart1data.Add(chart1.Average());
                    }

                    if (chart2.Count() == 0)
                    {
                        chart2data.Add(0.0d);
                    }
                    else
                    {
                        chart2data.Add(chart2.Average());
                    }

                    if (chart3.Count() == 0)
                    {
                        chart3data.Add(0.0d);
                    }
                    else
                    {
                        chart3data.Add(chart3.Average());
                    }

                    if (chart4.Count() == 0)
                    {
                        chart4data.Add(0.0d);
                    }
                    else
                    {
                        chart4data.Add(chart4.Average());
                    }

                    if (chart5.Count() == 0)
                    {
                        chart5data.Add(0.0d);
                    }
                    else
                    {
                        chart5data.Add(chart5.Average());
                    }

                    if (chart6.Count() == 0)
                    {
                        chart6data.Add(0.0d);
                    }
                    else
                    {
                        chart6data.Add(chart6.Average());
                    }


                    // Chart 2
                    if (bar1.Count() == 0)
                    {
                        bar1data.Add(0.0d);
                    }
                    else
                    {
                        bar1data.Add(bar1.Average());
                    }

                    if (bar2.Count() == 0)
                    {
                        bar2data.Add(0.0d);
                    }
                    else
                    {
                        bar2data.Add(bar2.Average());
                    }

                    if (bar3.Count() == 0)
                    {
                        bar3data.Add(0.0d);
                    }
                    else
                    {
                        bar3data.Add(bar3.Average());
                    }

                    if (bar4.Count() == 0)
                    {
                        bar4data.Add(0.0d);
                    }
                    else
                    {
                        bar4data.Add(bar4.Average());
                    }

                    if (bar5.Count() == 0)
                    {
                        bar5data.Add(0.0d);
                    }
                    else
                    {
                        bar5data.Add(bar5.Average());
                    }
                }
                catch
                {
                    return null;
                }
            }

            response.Chart1 = new DigitalQuadrantData();

            response.Chart1.Quadrant1 = Convert.ToDouble(countQ1) / (countQ1 + countQ2 + countQ3 + countQ4) * 100;
            response.Chart1.Quadrant2 = Convert.ToDouble(countQ2) / (countQ1 + countQ2 + countQ3 + countQ4) * 100;
            response.Chart1.Quadrant3 = Convert.ToDouble(countQ3) / (countQ1 + countQ2 + countQ3 + countQ4) * 100;
            response.Chart1.Quadrant4 = Convert.ToDouble(countQ4) / (countQ1 + countQ2 + countQ3 + countQ4) * 100;

            response.Chart2 = new DigitalBarChartData();
            response.Chart2.Items = new List<DigitalBarItem>();
            response.Chart2.Legends = new List<string>();
            response.Chart2.Legends.AddRange(response.GroupName);
            response.Chart2.Legends.AddRange(new[] { "SPEx2 Winners (2016)", "BSC Hall of Fame (2005)" });

            response.Chart2.Items.Add(CreateBarItem(itemText1, bar1data, 3.68d, 3.75d));
            response.Chart2.Items.Add(CreateBarItem(itemText2, bar2data, 3.68d, 3.69d));
            response.Chart2.Items.Add(CreateBarItem(itemText3, bar3data, 3.52d, 3.63d));
            response.Chart2.Items.Add(CreateBarItem(itemText4, bar4data, 3.44d, 3.75d));
            response.Chart2.Items.Add(CreateBarItem(itemText5, bar5data, 3.44d, 3.56d));

            response.Chart3 = new List<DigitalBarItem>();
            response.Chart3.Add(new DigitalBarItem()
            {
                Indicator = chartText1,
                Values = GetRange(chart1data.Average())
            });

            response.Chart4 = new List<DigitalBarItem>();
            response.Chart4.Add(new DigitalBarItem()
            {
                Indicator = chartText2,
                Values = GetRange(chart2data.Average())
            });
            response.Chart4.Add(new DigitalBarItem()
            {
                Indicator = chartText3,
                Values = GetRange(chart3data.Average())
            });
            response.Chart4.Add(new DigitalBarItem()
            {
                Indicator = chartText4,
                Values = GetRange(chart4data.Average())
            });
            response.Chart4.Add(new DigitalBarItem()
            {
                Indicator = chartText5,
                Values = GetRange(chart5data.Average())
            });
            response.Chart4.Add(new DigitalBarItem()
            {
                Indicator = chartText6,
                Values = GetRange(chart6data.Average())
            });
            return response;
        }
        

        private async Task<SurveyResult> GetDigitalIndividualReport(int surveyId, string uuid)
        {
            double cutoff = 2.8d;

            SurveyResult response = new SurveyResult();
            response.Total = 1;
            response.GroupName = new List<string>();

            int countQ1 = 0;
            int countQ2 = 0;
            int countQ3 = 0;
            int countQ4 = 0;

            string itemText1 = "";
            string itemText2 = "";
            string itemText3 = "";
            string itemText4 = "";
            string itemText5 = "";

            List<double> bar1data = new List<double>();
            List<double> bar2data = new List<double>();
            List<double> bar3data = new List<double>();
            List<double> bar4data = new List<double>();
            List<double> bar5data = new List<double>();

            string chartText1 = "";
            string chartText2 = "";
            string chartText3 = "";
            string chartText4 = "";
            string chartText5 = "";
            string chartText6 = "";

            List<double> chart1data = new List<double>();
            List<double> chart2data = new List<double>();
            List<double> chart3data = new List<double>();
            List<double> chart4data = new List<double>();
            List<double> chart5data = new List<double>();
            List<double> chart6data = new List<double>();

            string nm = "";
            try
            {
                /*
                int ratingItemId = Int32.Parse(s);
                IQueryable<string> q2;

                if (ratingItemId == 0)
                {
                    q2 = from rsp in _context.WebSurveyResponses
                         where rsp.ItemId == obj.Id
                         select rsp.Uuid;

                }
                else
                {
                    q2 = from rsp in _context.WebSurveyResponses
                         where rsp.ItemId == obj.Id && rsp.RatingId == ratingItemId
                         select rsp.Uuid;
                }
                List<string> uuids = await q2.Distinct().ToListAsync();
                
                var ratingItem = _context.WebSurveyRatingItems.Find(ratingItemId);

                if (ratingItemId == 0)
                {
                    response.GroupName.Add("Nilai rata-rata");
                    response.Total = uuids.Count();
                }
                else
                {
                    if (ratingItem != null)
                    {
                        response.GroupName.Add(ratingItem.ItemTextID);
                        response.Total += uuids.Count();
                    }
                }
                */

                var nq = from item in _context.WebSurveyItems
                         join p in _context.WebSurveyPages on item.PageId equals p.Id
                         join resp in _context.WebSurveyResponses on item.Id equals resp.ItemId
                         where p.SurveyId == surveyId && item.RatingId == 1 && resp.Uuid.Equals(uuid.Trim()) && item.ItemTextId.Contains("Nama")
                         orderby item.Id
                         select new GenericInfo()
                         {
                             Id = resp.Id,
                             Text = resp.AnswerText
                         };
                List<GenericInfo> txts = await nq.ToListAsync();
                foreach (GenericInfo txt in txts)
                {
                    response.GroupName.Add(txt.Text);
                }

                // response.GroupName.Add("Lembaga Penjamin Simpanan");

                if (txts.Count() > 1) nm = txts[1].Text;
                else if (txts.Count() > 0) nm = txts[0].Text;

                List<double> bar1 = new List<double>();
                List<double> bar2 = new List<double>();
                List<double> bar3 = new List<double>();
                List<double> bar4 = new List<double>();
                List<double> bar5 = new List<double>();

                List<double> chart1 = new List<double>();
                List<double> chart2 = new List<double>();
                List<double> chart3 = new List<double>();
                List<double> chart4 = new List<double>();
                List<double> chart5 = new List<double>();
                List<double> chart6 = new List<double>();

                // For each UUID
                List<ResultItem> nitems = await GetQuadrantData(surveyId, uuid);

                // Quadrant data
                if (nitems.Count() > 6)
                {
                    ResultItem item0 = nitems[0];
                    ResultItem item1 = nitems[1];
                    ResultItem item2 = nitems[2];
                    ResultItem item3 = nitems[3];
                    ResultItem item4 = nitems[4];
                    ResultItem item5 = nitems[5];

                    double d1 = item0.Val;
                    double d2 = (item1.Val + item2.Val + item3.Val + item4.Val + item5.Val) / 5.0d;

                    if (d1 >= cutoff)
                    {
                        if (d2 >= cutoff)
                        {
                            countQ1++;
                        }
                        else
                        {
                            countQ4++;
                        }
                    }
                    else
                    {
                        if (d2 >= cutoff)
                        {
                            countQ2++;
                        }
                        else
                        {
                            countQ3++;
                        }
                    }
                }

                foreach (ResultItem nitem in nitems)
                {
                    // Chart3
                    if (nitem.OrderNumber == 1)
                    {
                        chartText1 = nitem.ItemText;
                        chart1.Add(nitem.Val);
                    }
                    else if (nitem.OrderNumber == 2)
                    {
                        chartText2 = nitem.ItemText;
                        chart2.Add(nitem.Val);
                    }
                    else if (nitem.OrderNumber == 3)
                    {
                        chartText3 = nitem.ItemText;
                        chart3.Add(nitem.Val);
                    }
                    else if (nitem.OrderNumber == 4)
                    {
                        chartText4 = nitem.ItemText;
                        chart4.Add(nitem.Val);
                    }
                    else if (nitem.OrderNumber == 5)
                    {
                        chartText5 = nitem.ItemText;
                        chart5.Add(nitem.Val);
                    }
                    else if (nitem.OrderNumber == 6)
                    {
                        chartText6 = nitem.ItemText;
                        chart6.Add(nitem.Val);
                    }
                    // bar chart data
                    else if (nitem.OrderNumber == 10)
                    {
                        itemText1 = nitem.ItemText;
                        bar1.Add(nitem.Val);
                    }
                    else if (nitem.OrderNumber == 11)
                    {
                        itemText2 = nitem.ItemText;
                        bar2.Add(nitem.Val);
                    }
                    else if (nitem.OrderNumber == 12)
                    {
                        itemText3 = nitem.ItemText;
                        bar3.Add(nitem.Val);
                    }
                    else if (nitem.OrderNumber == 13)
                    {
                        itemText4 = nitem.ItemText;
                        bar4.Add(nitem.Val);
                    }
                    else if (nitem.OrderNumber == 14)
                    {
                        itemText5 = nitem.ItemText;
                        bar5.Add(nitem.Val);
                    }
                }

                // Chart 3
                if (chart1.Count() == 0)
                {
                    chart1data.Add(0.0d);
                }
                else
                {
                    chart1data.Add(chart1.Average());
                }

                if (chart2.Count() == 0)
                {
                    chart2data.Add(0.0d);
                }
                else
                {
                    chart2data.Add(chart2.Average());
                }

                if (chart3.Count() == 0)
                {
                    chart3data.Add(0.0d);
                }
                else
                {
                    chart3data.Add(chart3.Average());
                }

                if (chart4.Count() == 0)
                {
                    chart4data.Add(0.0d);
                }
                else
                {
                    chart4data.Add(chart4.Average());
                }

                if (chart5.Count() == 0)
                {
                    chart5data.Add(0.0d);
                }
                else
                {
                    chart5data.Add(chart5.Average());
                }

                if (chart6.Count() == 0)
                {
                    chart6data.Add(0.0d);
                }
                else
                {
                    chart6data.Add(chart6.Average());
                }


                // Chart 2
                if (bar1.Count() == 0)
                {
                    bar1data.Add(0.0d);
                }
                else
                {
                    bar1data.Add(bar1.Average());
                }

                if (bar2.Count() == 0)
                {
                    bar2data.Add(0.0d);
                }
                else
                {
                    bar2data.Add(bar2.Average());
                }

                if (bar3.Count() == 0)
                {
                    bar3data.Add(0.0d);
                }
                else
                {
                    bar3data.Add(bar3.Average());
                }

                if (bar4.Count() == 0)
                {
                    bar4data.Add(0.0d);
                }
                else
                {
                    bar4data.Add(bar4.Average());
                }

                if (bar5.Count() == 0)
                {
                    bar5data.Add(0.0d);
                }
                else
                {
                    bar5data.Add(bar5.Average());
                }
            }
            catch
            {
                return null;
            }

            response.Chart1 = new DigitalQuadrantData();

            response.Chart1.Quadrant1 = Convert.ToDouble(countQ1) / (countQ1 + countQ2 + countQ3 + countQ4) * 100;
            response.Chart1.Quadrant2 = Convert.ToDouble(countQ2) / (countQ1 + countQ2 + countQ3 + countQ4) * 100;
            response.Chart1.Quadrant3 = Convert.ToDouble(countQ3) / (countQ1 + countQ2 + countQ3 + countQ4) * 100;
            response.Chart1.Quadrant4 = Convert.ToDouble(countQ4) / (countQ1 + countQ2 + countQ3 + countQ4) * 100;

            response.Chart2 = new DigitalBarChartData();
            response.Chart2.Items = new List<DigitalBarItem>();
            response.Chart2.Legends = new List<string>();
            response.Chart2.Legends.Add(nm);
            response.Chart2.Legends.AddRange(new[] { "SPEx2 Winners (2016)", "BSC Hall of Fame (2005)" });

            response.Chart2.Items.Add(CreateBarItem(itemText1, bar1data, 3.68d, 3.75d));
            response.Chart2.Items.Add(CreateBarItem(itemText2, bar2data, 3.68d, 3.69d));
            response.Chart2.Items.Add(CreateBarItem(itemText3, bar3data, 3.52d, 3.63d));
            response.Chart2.Items.Add(CreateBarItem(itemText4, bar4data, 3.44d, 3.75d));
            response.Chart2.Items.Add(CreateBarItem(itemText5, bar5data, 3.44d, 3.56d));

            response.Chart3 = new List<DigitalBarItem>();
            response.Chart3.Add(new DigitalBarItem()
            {
                Indicator = chartText1,
                Values = GetRange(chart1data.Average())
            });

            response.Chart4 = new List<DigitalBarItem>();
            response.Chart4.Add(new DigitalBarItem()
            {
                Indicator = chartText2,
                Values = GetRange(chart2data.Average())
            });
            response.Chart4.Add(new DigitalBarItem()
            {
                Indicator = chartText3,
                Values = GetRange(chart3data.Average())
            });
            response.Chart4.Add(new DigitalBarItem()
            {
                Indicator = chartText4,
                Values = GetRange(chart4data.Average())
            });
            response.Chart4.Add(new DigitalBarItem()
            {
                Indicator = chartText5,
                Values = GetRange(chart5data.Average())
            });
            response.Chart4.Add(new DigitalBarItem()
            {
                Indicator = chartText6,
                Values = GetRange(chart6data.Average())
            });
            return response;

        }
        private List<double> GetRange(double x)
        {
            double cutoff = 2.8d;

            if (x >= cutoff)
            {
                double dx = x - cutoff;
                double maxpos = 4 - cutoff;

                double scaledX = (dx / maxpos) * 10;

                return new List<double>(new[] { 0.0d, scaledX });
            }

            double dxmin = (cutoff - x) * -1;
            double minpos = (cutoff - 1) * -1;

            double scaledXMin = dxmin / minpos * -10;

            return new List<double>(new[] { scaledXMin, 0.0d });
        }

        private DigitalBarItem CreateBarItem(string indicator, List<double> ds, double d2, double d3)
        {
            DigitalBarItem barItem = new DigitalBarItem();
            barItem.Indicator = indicator;
            barItem.Values = new List<double>();
            barItem.Values.AddRange(ds);
            barItem.Values.AddRange(new[] { d2, d3 });
            return barItem;
        }

        /**
         * @api {get} /Survey/name/{nm} GET survey by name
         * @apiVersion 1.0.0
         * @apiName GetSurveyByName
         * @apiGroup Survey
         * @apiPermission Basic Auth
         * 
         * @apiSuccessExample Success-Response:
         * {
         *     "id": 6,
         *     "title": "Digital Transformation Readiness Assessment",
         *     "intro": "Petunjuk Pengisian Digital Transformation Readiness Assessment",
         *     "description": "Dalam era industri 4.0, ... ",
         *     "categoryId": 1,
         *     "expiryDate": "2100-12-31T00:00:00",
         *     "publish": false,
         *     "addInfo": "",
         *     "emailToUserIds": "",
         *     "grouping": false,
         *     "createdDate": "2020-11-06T00:00:00",
         *     "createdBy": 1,
         *     "lastUpdated": "2020-11-06T00:00:00",
         *     "lastUpdatedBy": 1,
         *     "isDeleted": false,
         *     "deletedBy": 0,
         *     "deletedDate": "1970-01-01T00:00:00"
         * }
         */
        [HttpGet("name/{nm}")]
        public async Task<ActionResult<WebSurvey>> GetSurveyByName(string nm)
        {
            if (!CheckBasicAuth(Request.Headers["Authorization"].ToString())) return Unauthorized();

            WebSurvey survey = _context.WebSurveys.Where(a => a.Title.ToLower().Contains(nm) && !a.IsDeleted).FirstOrDefault();

            if (survey == null) return NotFound();

            return survey;
        }

        private async Task<ActionResult<SurveyReport>> GetAkhlakReport(int surveyId, string uuid)
        {
            WebSurvey survey = _context.WebSurveys.Where(a => a.Id == surveyId && !a.IsDeleted).FirstOrDefault();
            if (survey == null) return null;

            SurveyReport report = new SurveyReport();

            if (survey != null)
            {
                report.SurveyId = survey.Id;
                report.Title = survey.Title;
                report.Description = survey.Description;

                report.SurveyDate = await GetSurveyDate(uuid, false);
                report.Cover = await GetCoverItems(surveyId, uuid);
                report.Dimensions = await GetSurveyDimensions(surveyId, uuid, 0, survey);
            }

            report.Summary = new SurveyDimension();

            return report;
        }

        private async Task<ActionResult<SurveyReport>> GetProcessMaturityReport(int surveyId, string uuid)
        {
            WebSurvey survey = _context.WebSurveys.Where(a => a.Id == surveyId && !a.IsDeleted).FirstOrDefault();
            if (survey == null) return null;

            SurveyReport report = new SurveyReport();

            if (survey != null)
            {
                report.SurveyId = survey.Id;
                report.Title = survey.Title;
                report.Description = survey.Description;

                report.SurveyDate = await GetSurveyDate(uuid, false);
                report.Cover = await GetCoverItems(surveyId, uuid);
                report.Dimensions = await GetSurveyDimensions(surveyId, uuid, 0, survey);
            }

            report.Summary = new SurveyDimension();

            return report;
        }
        private async Task<SurveyReport> GetOnlineBSCReport(int surveyId, string uuid)
        {
            WebSurvey survey = _context.WebSurveys.Where(a => a.Id == surveyId && !a.IsDeleted).FirstOrDefault();
            if (survey == null) return null;

            SurveyReport report = new SurveyReport();

            if (survey != null)
            {
                report.SurveyId = survey.Id;
                report.Title = survey.Title;
                report.Description = "";
                report.SurveyDate = await GetSurveyDate(uuid, false);

                report.Cover = await GetCoverItems(surveyId, uuid);

                report.Dimensions = new List<SurveyDimension>();
                report.data = new List<SurveyChartTableData>();
                report.quadrant = new QuadrantData();
                report.Summary = new SurveyDimension();

                report.Summary.Id = report.SurveyId;
                report.Summary.Title = report.Title;

                report.Summary.Indicators = new List<IndicatorValue>();
                report.Summary.Dimensions = new List<SurveyDimension>();

                var query = from r in _context.WebSurveyResponses
                            join i in _context.WebSurveyItems on r.ItemId equals i.Id
                            join ratingItem in _context.WebSurveyRatingItems on r.RatingId equals ratingItem.Id
                            join rating in _context.WebSurveyRatings on ratingItem.RatingId equals rating.Id
                            where r.Uuid.Equals(uuid) && !i.ShowInCover && rating.RatingName.StartsWith("bsc")
                            select (ratingItem.Value * i.Weight);

                report.Summary.Score = query.Sum();
                report.Summary.Description = survey.Description;
            }

            return report;
        }

        private async Task<ActionResult<SurveyReport>> GetLeadershipReport(int surveyId, string uuid)
        {
            WebSurvey survey = _context.WebSurveys.Where(a => a.Id == surveyId && !a.IsDeleted).FirstOrDefault();
            if (survey == null) return null;

            SurveyReport report = new SurveyReport();

            if (survey != null)
            {
                report.SurveyId = survey.Id;
                report.Title = survey.Title;
                report.Description = survey.Description;
                report.SurveyDate = await GetSurveyDate(uuid, false);

                report.Cover = await GetCoverItems(surveyId, uuid);
                report.Dimensions = await GetSurveyDimensions(surveyId, uuid, 0, survey);
            }

            report.Summary = new SurveyDimension();

            return report;
        }


        private async Task<ActionResult<SurveyReport>> GetHRDiagnosticReport(int surveyId, string uuid)
        {
            WebSurvey survey = _context.WebSurveys.Where(a => a.Id == surveyId && !a.IsDeleted).FirstOrDefault();
            if (survey == null) return null;

            SurveyReport report = new SurveyReport();

            if (survey != null)
            {
                report.SurveyId = survey.Id;
                report.Title = survey.Title;
                report.Description = survey.Description;
                report.SurveyDate = await GetSurveyDate(uuid, false);

                report.Cover = await GetCoverItems(surveyId, uuid);
                report.Dimensions = await GetSurveyDimensions(surveyId, uuid, 0, survey);
            }

            report.Summary = GetHRReportSummary(report.Dimensions);

            return report;
        }
        private async Task<ActionResult<SurveyReport>> GetSPEx2Report(int surveyId, string uuid)
        {
            WebSurvey survey = _context.WebSurveys.Where(a => a.Id == surveyId && !a.IsDeleted).FirstOrDefault();
            if (survey == null) return null;

            SurveyReport report = new SurveyReport();

            if (survey != null)
            {
                report.SurveyId = survey.Id;
                report.Title = survey.Title;
                report.Description = survey.Description;
                report.SurveyDate = await GetSurveyDate(uuid, false);

                report.Cover = await GetCoverItems(surveyId, uuid);
                report.Dimensions = await GetSurveyDimensions(surveyId, uuid, 0, survey);
            }

            report.Summary = new SurveyDimension();

            return report;
        }
        private WebSurveyOwner GetSurveyOwnerByGroupUUID(string uuid)
        {
            var query = from g in _context.WebSurveyGroups
                        join o in _context.WebSurveyOwners
                        on g.OwnerId equals o.Id
                        where g.Uuid.Equals(uuid) && !g.IsDeleted
                        select new WebSurveyOwner()
                        {
                            Id = o.Id,
                            SurveyId = o.SurveyId,
                            Name = o.Name,
                            Company = o.Company,
                            Email = o.Email,
                            Phone = o.Phone,
                            Department = o.Department,
                            Position = o.Position,
                            CreatedDate = o.CreatedDate,
                            CreatedBy = o.CreatedBy,
                            LastUpdated = o.LastUpdated,
                            LastUpdatedBy = o.LastUpdatedBy,
                            IsDeleted = o.IsDeleted,
                            DeletedBy = o.DeletedBy,
                            DeletedDate = o.DeletedDate
                        };

            return query.FirstOrDefault();
        }

        private async Task<SurveyResult> GetEngagementDigitalReport(int surveyId, string groupUUID)
        {
            return await GetDigitalGroupReport(surveyId, "0");
        }

        private async Task<ActionResult<SurveyReport>> GetHRBPReport(int surveyId, string groupUUID)
        {
            WebSurvey survey = _context.WebSurveys.Where(a => a.Id == surveyId && !a.IsDeleted).FirstOrDefault();
            if (survey == null) return null;

            SurveyReport report = new SurveyReport();

            report.SurveyId = survey.Id;
            report.Title = survey.Title;
            report.Description = survey.Description;
            report.SurveyDate = await GetSurveyDate(groupUUID, true);

            WebSurveyOwner owner = GetSurveyOwnerByGroupUUID(groupUUID);
            if (owner == null) return null;

            List<string> ts1 = await _context.WebSurveyGroups.Where(a => a.OwnerId == owner.Id && !a.IsDeleted).Select(a => a.Uuid).ToListAsync();
            List<string> groupsUUIDs = new List<string>();

            foreach (string uuid in ts1)
            {
                if(_context.WebSurveyResponses.Where(a => a.GroupUUID.Equals(uuid)).Any())
                {
                    groupsUUIDs.Add(uuid);
                }
            }

            report.Cover = await GetCoverItemsFromOwner(owner);
            report.data = new List<SurveyChartTableData>();

            SurveyChartTableData dt1 = await GetChartTableData1(surveyId, owner.Id, groupsUUIDs);
            SurveyChartTableData dt2 = await GetChartTableData2(surveyId, owner.Id, groupsUUIDs);

            report.data.Add(dt1);
            report.data.Add(dt2);

            report.quadrant = await GetQuadrantData(dt1.Table.Rows, dt2.Table.Rows);

            report.Dimensions = new List<SurveyDimension>();
            report.Summary = new SurveyDimension();

            return report;
        }
        private async Task<QuadrantData> GetQuadrantData(List<KDMApi.Models.Survey.TableRow> vals, List<KDMApi.Models.Survey.TableRow> ranks)
        {
            QuadrantData data = new QuadrantData();

            GenericInfoString q1 = new GenericInfoString(1, "Q1");
            GenericInfoString q2 = new GenericInfoString(2, "Q2");
            GenericInfoString q3 = new GenericInfoString(3, "Q3");
            GenericInfoString q4 = new GenericInfoString(4, "Q4");

            List<KDMApi.Models.Survey.TableRow> nvals = new List<KDMApi.Models.Survey.TableRow>(vals);
            List<KDMApi.Models.Survey.TableRow> nranks = new List<KDMApi.Models.Survey.TableRow>(ranks);

            double valMidPoint = GetMidPoint(nvals);
            double rankMidPoint = GetMidPoint(nranks);

            nvals.Sort();
            nranks.Sort();

            double max = 8.5d;
            double min = 0.5d;
            double range = max - min;

            double curMin = 10.0d;
            double curMax = 0.0f;

            foreach (KDMApi.Models.Survey.TableRow row in nvals)
            {
                double curVal = row.Data[row.Data.Count - 1];

                if (curVal < curMin)
                {
                    curMin = curVal;
                }
                if (curVal > curMax)
                {
                    curMax = curVal;
                }
            }

            if (nvals.Count() == nranks.Count())
            {
                for(int i = 0; i < nvals.Count(); i++)
                {
                    KDMApi.Models.Survey.TableRow curVal = nvals[i];
                    KDMApi.Models.Survey.TableRow curRank = nranks[i];

                    if (curVal.Title.Equals(curRank.Title))
                    {
                        double x = ScalePoint(curVal.Data[curVal.Data.Count - 1], max, min, curMax, curMin, range);
                        double y = curRank.Data[curRank.Data.Count - 1];
                        string title = curVal.Title;

                        data.Data.Add(new XY(x, y));

                        // Q1: value low, ranking high
                        // Q2: value high, ranking high
                        // Q3: value high, ranking low
                        // Q4: value low, ranking low

                        if(x < valMidPoint && y >= rankMidPoint)
                        {
                            q1.Children.Add(title);
                        }
                        if (x >= valMidPoint && y >= rankMidPoint)
                        {
                            q2.Children.Add(title);
                        }
                        else if (x >= valMidPoint && y < rankMidPoint)
                        {
                            q3.Children.Add(title);
                        }
                        else if (x < valMidPoint && y < rankMidPoint)
                        {
                            q4.Children.Add(title);
                        }
                    }

                }
            }

            data.Quadrant.AddRange(new[] { q1, q2, q3, q4 });
            return data;
        }
        private double ScalePoint(double v, double max, double min, double curMax, double curMin, double range)
        {
            double d = min + ((v - curMin) / (curMax - curMin) * (max - min));
            return d < min ? min : d;
        }
        private double GetMidPoint(List<KDMApi.Models.Survey.TableRow> rows)
        {
            return 4.5d;
        }
        private async Task<SurveyChartTableData> GetChartTableData(int surveyId, int ownerId, List<string> groupsUUIDs, string title, string presql, string andsql)
        {
            SurveyChartTableData data = new SurveyChartTableData();

            TableData dt1 = new TableData();
            dt1.Rows = new List<KDMApi.Models.Survey.TableRow>();
            dt1.Id = 1;
            dt1.Title = title;
            dt1.Description = "";

            dt1.Header = new List<string>();

            var q = from g in _context.WebSurveyGroups
                    where !g.IsDeleted && g.OwnerId == ownerId
                    orderby g.Uuid
                    select new
                    {
                        Uuid = g.Uuid,
                        Name = g.Name
                    };

            var ts = await q.ToListAsync();

            foreach(var t in ts)
            {
                if (_context.WebSurveyResponses.Where(a => a.GroupUUID.Equals(t.Uuid)).Any())
                {
                    dt1.Header.Add(t.Name);
                }
            }
            dt1.Header.Add("Mid-Point");

            SurveyChartData dt2 = new SurveyChartData();
            dt2.Data = new List<IndicatorValueArray>();
            dt2.Id = 2;
            dt2.Title = title;
            dt2.Description = "";
            dt2.Legend = dt1.Header;

            var q1 = from dim in _context.WebSurveyDimensions
                     where !dim.IsDeleted && dim.SurveyId == surveyId
                     select new GenericInfo()
                     {
                         Id = dim.Id,
                         Text = dim.ItemText
                     };
            List<GenericInfo> dimensions = await q1.ToListAsync();

            string wheregroup = " WHERE (";
            foreach (string uuid in groupsUUIDs)
            {
                if (wheregroup.Equals(" WHERE ("))
                {
                    wheregroup += " response.GroupUUID LIKE '" + uuid + "' ";
                }
                else
                {
                    wheregroup += " OR response.GroupUUID LIKE '" + uuid + "' ";
                }
            }
            wheregroup += ")";

            foreach (GenericInfo dimension in dimensions)
            {
                string sql = presql + wheregroup + " AND dim.Id = " + dimension.Id.ToString() + andsql + " group by response.GroupUUID order by response.GroupUUID";

                IndicatorValueArray indicator = new IndicatorValueArray();
                indicator.Id = dimension.Id;
                indicator.Indicator = dimension.Text;
                indicator.Weight = 100;

                KDMApi.Models.Survey.TableRow row = new KDMApi.Models.Survey.TableRow();
                row.Data = new List<double>();

                row.Title = dimension.Text;

                row.Data = new List<double>();
                List<DoubleLong> dls = await _context.GetDoubleLongs.FromSql(sql).ToListAsync();

                Double total = 0.0d;
                foreach (DoubleLong dl in dls)
                {
                    Double a = Convert.ToDouble(dl.Amount1) / dl.Amount2;
                    row.Data.Add(a);
                    total += a;
                }
                row.Data.Add(total / dls.Count());

                indicator.Value = row.Data;

                dt1.Rows.Add(row);
                dt2.Data.Add(indicator);
            }

            data.Table = dt1;
            data.Chart = dt2;

            return data;

        }
        private async Task<SurveyChartTableData> GetChartTableData2(int surveyId, int ownerId, List<string> groupsUUIDs)
        {
            string title = "Ranking";

            string presql = "SELECT cast(sum(response.Ranking) as bigint) as Amount1, cast(count(response.Ranking) as bigint) as Amount2 " +
                            "FROM dbo.WebSurveyItems AS item " +
                            "JOIN dbo.WebSurveyItemDimensions as itemDimension on item.Id = itemDimension.ItemId " +
                            "JOIN dbo.WebSurveyDimensions as dim on itemDimension.DimensionId = dim.Id " +
                            "JOIN dbo.WebSurveyResponses as response on item.Id = response.ItemId ";

            string andsql = " AND response.Ranking > 0 AND response.RatingId = 0 ";

            return await GetChartTableData(surveyId, ownerId, groupsUUIDs, title, presql, andsql);
        }
        private async Task<SurveyChartTableData> GetChartTableData1(int surveyId, int ownerId, List<string> groupsUUIDs)
        {
            string title = "Value Data";

            string presql = "SELECT cast(sum(ratingItem.value) as bigint) as Amount1, cast(count(ratingItem.value) as bigint) as Amount2 " +
                            "FROM dbo.WebSurveyItems AS item " +
                            "JOIN dbo.WebSurveyItemDimensions as itemDimension on item.Id = itemDimension.ItemId " +
                            "JOIN dbo.WebSurveyDimensions as dim on itemDimension.DimensionId = dim.Id " +
                            "JOIN dbo.WebSurveyResponses as response on item.Id = response.ItemId " +
                            "JOIN dbo.WebSurveyRatingItems as ratingItem on response.RatingId = ratingItem.Id  ";

            string andsql = " AND response.Ranking = 0 AND response.RatingId > 0 ";

            return await GetChartTableData(surveyId, ownerId, groupsUUIDs, title, presql, andsql);
        }
        private async Task<ActionResult<SurveyReport>> GetODReport(int surveyId, string uuid)
        {
            WebSurvey survey = _context.WebSurveys.Where(a => a.Id == surveyId && !a.IsDeleted).FirstOrDefault();
            if (survey == null) return null;

            SurveyReport report = new SurveyReport();

            report.SurveyId = survey.Id;
            report.Title = survey.Title;
            report.Description = survey.Description;
            report.SurveyDate = await GetSurveyDate(uuid, false);

            report.Cover = await GetCoverItems(surveyId, uuid);
            report.Dimensions = await GetSurveyDimensions(surveyId, uuid, 0, survey);
            report.Summary = new SurveyDimension();

            return report;
        }

        private async Task<List<IndicatorText>> GetCoverItemsFromOwner(WebSurveyOwner owner)
        {
            IndicatorText name = new IndicatorText()
            {
                Id = 1,
                Indicator = "Nama",
                Text = owner.Name
            };
            IndicatorText company = new IndicatorText()
            {
                Id = 1,
                Indicator = "Organisasi",
                Text = owner.Company
            };
            IndicatorText phone = new IndicatorText()
            {
                Id = 1,
                Indicator = "Telepon",
                Text = owner.Phone
            };
            IndicatorText dept = new IndicatorText()
            {
                Id = 1,
                Indicator = "Unit kerja",
                Text = owner.Department
            };
            IndicatorText post = new IndicatorText()
            {
                Id = 1,
                Indicator = "Jabatan",
                Text = owner.Position
            };

            List<IndicatorText> txs = new List<IndicatorText>();
            txs.AddRange(new[] { name, company, phone, dept, post });

            return txs;
        }

        private async Task<List<IndicatorText>> GetCoverItems(int surveyId, string uuid)
        {
            var query = from item in _context.WebSurveyItems
                        join response in _context.WebSurveyResponses
                        on item.Id equals response.ItemId
                        join page in _context.WebSurveyPages
                        on item.PageId equals page.Id
                        where page.SurveyId == surveyId && item.ShowInCover && response.Uuid.Equals(uuid) && !item.IsDeleted && !response.IsDeleted
                        orderby item.OrderNumber
                        select new IndicatorText()
                        {
                            Id = response.RatingId,
                            Indicator = item.ItemTextId,
                            Text = response.AnswerText
                        };

            List<IndicatorText> txs = await query.ToListAsync();

            foreach(IndicatorText tx in txs)
            {
                if(tx.Id != 0)
                {
                    WebSurveyRatingItem rating = await _context.WebSurveyRatingItems.Where(a => !a.IsDeleted && a.Id == tx.Id).FirstOrDefaultAsync();
                    if(rating != null)
                    {
                        tx.Text = rating.ItemTextID;
                    }
                }
            }

            return txs;
        }
        private async Task<List<SurveyDimension>> GetSurveyDimensions(int surveyId, string uuid, int parent, WebSurvey survey)
        {
            List<SurveyDimension> response = new List<SurveyDimension>();

            List<WebSurveyDimension> dimensions = _context.WebSurveyDimensions.Where(a => !a.IsDeleted && a.SurveyId == surveyId && a.Parent == parent).OrderBy(a => a.OrderNumber).ToList();

            int i = 0;          // Digunakan untuk leadership
            foreach(WebSurveyDimension dimension in dimensions)
            {
                SurveyDimension dim = new SurveyDimension();
                dim.Id = dimension.Id;
                dim.Title = dimension.ItemText;
                dim.Description = dimension.Description;
                dim.Score = 0.0d;

                if (survey.Title.StartsWith("Process Maturity Level"))
                {
                    dim.Indicators = await GetSelfIndicatorValues(dimension.Id, uuid);
                }
                else if (survey.Title.StartsWith("HR Diagnostics"))
                {
                    dim.Indicators = await GetIndicatorValues(dimension.Id, uuid);
                }
                else if (survey.Title.StartsWith("Strategy and Performance Execution"))
                {
                    dim.Indicators = await GetSelfIndicatorValues(dimension.Id, uuid);
                }
                else if (survey.Title.StartsWith("Organization Diagnostics"))
                {
                    dim.Indicators = await GetIndicatorValues(dimension.Id, uuid);
                }
                else if (survey.Title.Contains("Leadership"))
                {
                    dim.Indicators = await GetIndicatorValues(dimension.Id, uuid);
                    if(dim.Indicators != null)
                    {
                        double tv = 0.0d;
                        double nv = 0;
                        foreach(IndicatorValue v in dim.Indicators)
                        {
                            tv += v.Value;
                            nv++;
                        }
                        dim.Score = nv == 0 ? 0 : tv / nv;
                        dim.Description = GetScoreDescription(i, dim.Score);
                        i++;
                    }
                }
                else if(survey.Title.Contains("Akhlak"))
                {
                    dim.Indicators = await GetSelfIndicatorValues(dimension.Id, uuid);
                    if(dim.Indicators.Count > 0)
                    {
                        IndicatorValue value = dim.Indicators[0];
                        value.Weight = 100;
                        if(value.Value < 3.00)
                        {
                            dim.Description = "Anda perlu menyadari bahwa nilai ini penting serta mau berusaha untuk mendorong diri kita menjadi yang lebih baik.";
                        }
                        else if(value.Value <= 3.50)
                        {
                            dim.Description = "Anda perlu meningkatkan komitmen diri di dalam melakukan implementasi nilai ini dengan lebih konsisten.";
                        }
                        else
                        {
                            dim.Description = "Anda perlu mempertahankan perilaku kerja yang positif, dengan selalu menerapkan nilai ini secara konsisten";
                        }
                    }
                }

                response.Add(dim);
            }

            foreach(SurveyDimension surveyDimension in response)
            {
                if(surveyDimension.Indicators.Count == 0)
                {
                    surveyDimension.Dimensions = await GetSurveyDimensions(surveyId, uuid, surveyDimension.Id, survey);
                }
            }

            return response;
        }

        private string GetScoreDescription(int index, double score)
        {
            List<string> str0 = new List<string>(new[] { "Organisasi perlu mensosialisasikan Integritas sebagai bagian penting dalam mendorong kinerja", "Organisasi perlu meningkatkan kompetensi Integritas sebagai bagian penting dalam mendorong kinerja", "Organisasi perlu mempertahankan kompetensi Integritas sebagai bagian penting dalam mendorong kinerja" });
            List<string> str1 = new List<string>(new[] { "Organisasi perlu mensosialisasikan Enthusiasthic sebagai bagian penting dalam mendorong kinerja", "Organisasi perlu meningkatkan kompetensi  Enthusiasthic sebagai bagian penting dalam mendorong kinerja", "Organisasi perlu mempertahankan kompetensi  Enthusiasthic sebagai bagian penting dalam mendorong kinerja" });
            List<string> str2 = new List<string>(new[] { "Organisasi perlu mensosialisasikan Creativity & Innovation sebagai bagian penting dalam mendorong kinerja", "Organisasi perlu meningkatkan kompetensi Creativity & Innovation sebagai bagian penting dalam mendorong kinerja", "Organisasi perlu mempertahankan kompetensi Creativity & Innovation sebagai bagian penting dalam mendorong kinerja" });
            List<string> str3 = new List<string>(new[] { "Organisasi perlu mensosialisasikan Building Partnership sebagai bagian penting dalam mendorong kinerja", "Organisasi perlu meningkatkan kompetensi  Building Partnership sebagai bagian penting dalam mendorong kinerja", "Organisasi perlu mempertahankan kompetensi Building Partnership sebagai bagian penting dalam mendorong kinerja" });
            List<string> str4 = new List<string>(new[] { "Organisasi perlu mensosialisasikan pemahaman Business Acumen sebagai bagian penting dalam mendorong kinerja", "Organisasi perlu meningkatkan kompetensi pemahaman Business Acumen sebagai bagian penting dalam mendorong kinerja", "Organisasi perlu mempertahankan kompetensi pemahaman Business Acumen sebagai bagian penting dalam mendorong kinerja" });
            List<string> str5 = new List<string>(new[] { "Organisasi perlu mensosialisasikan pentingnya Customer Focus sebagai bagian dalam mendorong kinerja", "Organisasi perlu meningkatkan kompetensi pentingnya Customer Focus sebagai bagian dalam mendorong kinerja", "Organisasi perlu mempertahankan kompetensi pentingnya Customer Focus sebagai bagian dalam mendorong kinerja" });
            List<string> str6 = new List<string>(new[] { "Organisasi perlu mensosialisasikan Driving Execution sebagai bagian penting dalam mendorong kinerja", "Organisasi perlu meningkatkan kompetensi Driving Execution sebagai bagian penting dalam mendorong kinerja", "Organisasi perlu mempertahankan kompetensi Driving Execution sebagai bagian penting dalam mendorong kinerja" });
            List<string> str7 = new List<string>(new[] { "Organisasi perlu mensosialisasikan Visionary Leadership sebagai bagian penting dalam mendorong kinerja", "Organisasi perlu meningkatkan kompetensi  Visionary Leadership sebagai bagian penting dalam mendorong kinerja", "Organisasi perlu mempertahankan kompetensi  Visionary Leadership sebagai bagian penting dalam mendorong kinerja" });
            List<string> str8 = new List<string>(new[] { "Organisasi perlu mensosialisasikan Aligning Performance for Success sebagai bagian penting dalam mendorong kinerja", "Organisasi perlu meningkatkan kompetensi  Aligning Performance for Success sebagai bagian penting dalam mendorong kinerja", "Organisasi perlu mempertahankan kompetensi  Aligning Performance for Success sebagai bagian penting dalam mendorong kinerja" });
            List<string> str9 = new List<string>(new[] { "Organisasi perlu mensosialisasikan Empowering sebagai bagian penting dalam mendorong kinerja", "Organisasi perlu meningkatkan kompetensi  Empowering sebagai bagian penting dalam mendorong kinerja", "Organisasi perlu mempertahankan kompetensi  Empowering sebagai bagian penting dalam mendorong kinerja" });
            List<string> str10 = new List<string>(new[] { "Organisasi perlu mensosialisasikan Change Leadership sebagai bagian penting dalam mendorong kinerja", "Organisasi perlu meningkatkan kompetensi  Change Leadership sebagai bagian penting dalam mendorong kinerja", "Organisasi perlu mempertahankan kompetensi  Change Leadership sebagai bagian penting dalam mendorong kinerja" });
            List<string> str11 = new List<string>(new[] { "Organisasi perlu mensosialisasikan Strategic Orientation sebagai bagian penting dalam mendorong kinerja", "Organisasi perlu meningkatkan kompetensi  Strategic Orientation sebagai bagian penting dalam mendorong kinerja", "Organisasi perlu mempertahankan kompetensi  Strategic Orientation sebagai bagian penting dalam mendorong kinerja" });
            List<List<string>> strs = new List<List<string>>();
            strs.AddRange(new[] { str0, str1, str2, str3, str4, str5, str6, str7, str8, str9, str10, str11 });

            int i = 0;
            if(score > 3.5)
            {
                i = 2;
            }
            else if(score > 3.0)
            {
                i = 1;
            }
            
            if(index >= 0 && index < strs.Count())
            {
                List<string> s = strs[index];
                if(i >= 0 && i < s.Count())
                {
                    return s[i];
                }
            }

            return "";
        }

        private SurveyDimension GetHRReportSummary(List<SurveyDimension> dimensions)
        {
            SurveyDimension report = new SurveyDimension();
            report.Indicators = new List<IndicatorValue>();
            Double total = 0.0d;

            foreach(SurveyDimension dimension in dimensions)
            {
                if(dimension.Indicators.Count > 0)
                {
                    double tv = GetTotalWeight(dimension.Indicators);
                    total += tv;

                    IndicatorValue v1 = new IndicatorValue();
                    v1.Id = dimension.Id;
                    v1.Indicator = dimension.Title;

                    if(dimension.Title.Equals("Strategic"))
                    {
                        v1.Value = Math.Round(tv / 1.57142);
                    }
                    else
                    {
                        v1.Value = Math.Round(tv);
                    }
                    v1.Value = v1.Value > 100.0d ? 100.0d : v1.Value;
                    v1.Weight = 100.0d;

                    report.Indicators.Add(v1);
                }
                else
                {
                    double subtotal = 0.0d;
                    foreach(SurveyDimension d in dimension.Dimensions)
                    {
                        subtotal += GetTotalWeight(d.Indicators);
                    }
                    total += subtotal;

                    IndicatorValue v2 = new IndicatorValue();
                    v2.Id = dimension.Id;
                    v2.Indicator = dimension.Title;
                    v2.Value = Math.Round(subtotal / 1.235d);
                    v2.Weight = 100.0d;

                    v2.Value = v2.Value > 100.0d ? 100.0d : v2.Value;

                    report.Indicators.Add(v2);
                }

            }

            // Jika semua dijawab 5, maka total score adalah 380.642
            if (total < 211.0d)
            {
                report.Title = "Poorly Managed";
                report.Description = "Sesungguhnya organisasi anda telah menangkap pentingnya pengelolaan SDM yang komprehensif dan sepertinya sudah mulai melangkah pada pembentukan sistem yang baku. Akan tetapi saat ini masih banyak program dan fungsi pengelolaan SDM yang belum terealisasi secara ideal. Masih banyak pula fungsi HR yang belum dikaji lebih lanjut sehingga implementasi programnya masih reaktif, belum konsisten, dan tidak terintegrasi satu sama lain. Fungsi pengelolaan SDM didominasi oleh peran personel admin.";
            }
            else if (total < 321.0d)
            {
                report.Title = "Need improvement";
                report.Description = "Organisasi anda telah memiliki pola dan target dalam mengelola dan mengimplementasikan proses-proses yang ada di fungsi HR. Namun masih ada sistem dan program HR yang perlu distandarisasi kembali, baik dari sisi prosedur, proses bisnis, maupun rencana implementasinya agar sesuai dengan standard pengelolaan SDM yang ideal dan memenuhi kebutuhan para karyawan. Pengelolaan SDM yang ada saat ini perlu diselaraskan kembali dengan tujuan organisasi sehingga peran dan kontribusi HR menjadi lebih nyata.";
            }
            else
            {
                report.Title = "Well Managed";
                report.Description = "Selamat! Organisasi anda telah memiliki strategi pengelolaan SDM yang baku dan dilengkapi dengan program serta indikator keberhasilan yang objektif. Arahan strategi SDM sudah dapat diasumsikan merupakan turunan dari korporat sehingga sistem dan rencana aksi setiap program SDM telah sistematis dan menciptakan kolaborasi yang optimal antar pelaksana. Sebagai praktisi SDM, anda perlu mempertahankan kondisi ini untuk menjaga motivasi dan kontribusi para talent di dalam organisasi.";
            }

            return report;
        }

        private Double GetTotalWeight(List<IndicatorValue> vals)
        {
            Double total = 0.0d;
            foreach(IndicatorValue v in vals)
            {
                total += v.Weight * v.Value;
            }

            return total;
        }
        private async Task<DateTime> GetSurveyDate(string uuid, bool groupUUID = false)
        {
            DateTime def = DateTime.Today;

            if(groupUUID)
            {
                WebSurveyOwner owner = GetSurveyOwnerByGroupUUID(uuid);
                if (owner == null) return def;

                List<WebSurveyGroup> groups = await _context.WebSurveyGroups.Where(a => a.OwnerId == owner.Id && !a.IsDeleted).ToListAsync();

                DateTime greatest = new DateTime(1);
                foreach(WebSurveyGroup group in groups)
                {
                    greatest = group.ExpiredTime > greatest ? group.ExpiredTime : greatest;
                }
                def = greatest;
            }
            else
            {
                WebSurveyResponse response = _context.WebSurveyResponses.Where(a => a.Uuid.Equals(uuid)).FirstOrDefault();
                if (response != null) return response.LastUpdated;
            }

            return def;
        }

        private async Task<List<IndicatorValue>> GetSelfIndicatorValues(int parentDimensionId, string uuid)
        {
            string sql = string.Join(" ", new[] { "SELECT d.OrderNumber AS Id, d.ItemText As Indicator, AVG(CAST(rating.Value AS float)) AS Value, SUM(item.Weight) as Weight",
                "FROM dbo.WebSurveyItemDimensions AS itemDimension",
                "JOIN WebSurveyDimensions AS d",
                "ON itemDimension.DimensionId = d.Id",
                "JOIN dbo.WebSurveyItems AS item",
                "ON itemDimension.ItemId = item.Id",
                "JOIN dbo.WebSurveyResponses AS resp",
                "ON item.Id = resp.ItemId",
                "JOIN dbo.WebSurveyRatingItems AS rating",
                "ON resp.RatingId = rating.Id",
                string.Join("", new[] { "WHERE resp.uuid LIKE '", uuid, "'"}),
                "AND d.Id = ", parentDimensionId.ToString(),
                "GROUP BY d.ItemText, d.OrderNumber",
                "ORDER BY d.OrderNumber" });

            return await _context.IndicatorValues.FromSql(sql).ToListAsync<IndicatorValue>();
        }

        private async Task<List<IndicatorValue>> GetGroupIndicatorValues(int parentDimensionId, string uuid)
        {
            string sql = string.Join(" ", new[] { "SELECT d.OrderNumber AS Id, d.ItemText As Indicator, AVG(CAST(rating.Value AS float)) AS Value, SUM(item.Weight) as Weight",
                "FROM dbo.WebSurveyItemDimensions AS itemDimension",
                "JOIN WebSurveyDimensions AS d",
                "ON itemDimension.DimensionId = d.Id",
                "JOIN dbo.WebSurveyItems AS item",
                "ON itemDimension.ItemId = item.Id",
                "JOIN dbo.WebSurveyResponses AS resp",
                "ON item.Id = resp.ItemId",
                "JOIN dbo.WebSurveyRatingItems AS rating",
                "ON resp.RatingId = rating.Id",
                string.Join("", new[] { "WHERE resp.GroupUUID LIKE '", uuid, "'"}),
                "AND d.Parent = ", parentDimensionId.ToString(),
                "GROUP BY d.ItemText, d.OrderNumber",
                "ORDER BY d.OrderNumber" });

            return await _context.IndicatorValues.FromSql(sql).ToListAsync<IndicatorValue>();
        }
        private async Task<List<IndicatorValue>> GetGroupSelfIndicatorValues(int dimensionId, string uuid)
        {
            string sql = string.Join(" ", new[] { "SELECT d.OrderNumber AS Id, d.ItemText As Indicator, AVG(CAST(rating.Value AS float)) AS Value, SUM(item.Weight) as Weight",
                "FROM dbo.WebSurveyItemDimensions AS itemDimension",
                "JOIN WebSurveyDimensions AS d",
                "ON itemDimension.DimensionId = d.Id",
                "JOIN dbo.WebSurveyItems AS item",
                "ON itemDimension.ItemId = item.Id",
                "JOIN dbo.WebSurveyResponses AS resp",
                "ON item.Id = resp.ItemId",
                "JOIN dbo.WebSurveyRatingItems AS rating",
                "ON resp.RatingId = rating.Id",
                string.Join("", new[] { "WHERE resp.GroupUUID LIKE '", uuid, "'"}),
                "AND d.Id = ", dimensionId.ToString(),
                "GROUP BY d.ItemText, d.OrderNumber",
                "ORDER BY d.OrderNumber" });

            return await _context.IndicatorValues.FromSql(sql).ToListAsync<IndicatorValue>();
        }

        private async Task<List<IndicatorValue>> GetIndicatorValues(int parentDimensionId, string uuid)
        {
            string sql = string.Join(" ", new[] { "SELECT d.OrderNumber AS Id, d.ItemText As Indicator, AVG(CAST(rating.Value AS float)) AS Value, SUM(item.Weight) as Weight",
                "FROM dbo.WebSurveyItemDimensions AS itemDimension",
                "JOIN WebSurveyDimensions AS d",
                "ON itemDimension.DimensionId = d.Id",
                "JOIN dbo.WebSurveyItems AS item",
                "ON itemDimension.ItemId = item.Id",
                "JOIN dbo.WebSurveyResponses AS resp",
                "ON item.Id = resp.ItemId",
                "JOIN dbo.WebSurveyRatingItems AS rating",
                "ON resp.RatingId = rating.Id",
                string.Join("", new[] { "WHERE resp.uuid LIKE '", uuid, "'"}),
                "AND d.Parent = ", parentDimensionId.ToString(),
                "GROUP BY d.ItemText, d.OrderNumber",
                "ORDER BY d.OrderNumber" });

            return await _context.IndicatorValues.FromSql(sql).ToListAsync<IndicatorValue>();
        }

        private string BuildWhereClause(List<string> uuids)
        {
            string str = @"(";

            foreach(string uuid in uuids)
            {

                str += str.Equals(@"(") ? @" resp.uuid LIKE '" + uuid + @"' " : @" or resp.uuid LIKE '" + uuid + @"' ";
            }
            str += @")";

            return str;
        }
        private async Task<List<IndicatorValue>> GetIndicatorValuesByList(int parentDimensionId, List<string> uuids)
        {
            string sql = string.Join(" ", new[] { "SELECT d.OrderNumber AS Id, d.ItemText As Indicator, AVG(CAST(rating.Value AS float)) AS Value, SUM(item.Weight) as Weight",
                "FROM dbo.WebSurveyItemDimensions AS itemDimension",
                "JOIN WebSurveyDimensions AS d",
                "ON itemDimension.DimensionId = d.Id",
                "JOIN dbo.WebSurveyItems AS item",
                "ON itemDimension.ItemId = item.Id",
                "JOIN dbo.WebSurveyResponses AS resp",
                "ON item.Id = resp.ItemId",
                "JOIN dbo.WebSurveyRatingItems AS rating",
                "ON resp.RatingId = rating.Id",
                "WHERE", BuildWhereClause(uuids),
                "AND d.Id = ", parentDimensionId.ToString(),
                "GROUP BY d.ItemText, d.OrderNumber",
                "ORDER BY d.OrderNumber" });

            return await _context.IndicatorValues.FromSql(sql).ToListAsync<IndicatorValue>();
        }

        private string GetUUID()
        {
            return Guid.NewGuid().ToString();
        }

        private async Task<DualLanguageId> GetFeedbackInfo(int surveyId, string uuid)
        {

            var query = from r in _context.WebSurveyResponses
                        join i in _context.WebSurveyItems
                        on r.ItemId equals i.Id
                        join rating in _context.WebSurveyRatingItems
                        on r.RatingId equals rating.Id
                        where r.Uuid.Equals(uuid) && !i.ShowInCover
                        select rating.Value;

            List<int> vals = query.ToList();
            double total = Convert.ToDouble(vals.Sum());
            double avg = 0.0d;

            string prefixId = "Organisasi anda ada di tahap";
            string prefixEn = "Your organization is in";
            string contentId = "";
            string contentEn = "";

            DualLanguageId response = new DualLanguageId();

            WebSurvey survey = _context.WebSurveys.Find(surveyId);
            if (survey != null)
            {
                if (survey.Title.StartsWith("Process Maturity Level"))
                {
                    prefixId = "Pengelolaan proses di organisasi anda";
                    prefixEn = "Process management in your organization is";
                    avg = total / 8;
                    if (avg > 4.5)
                    {
                        response.Id = 5;
                        contentId = "sungguh sangat baik.";
                        contentEn = "excellent state.";
                    }
                    else if (avg > 4.0)
                    {
                        response.Id = 4;
                        contentId = "sangat baik.";
                        contentEn = "very good state.";
                    }
                    else if (avg > 3.0)
                    {
                        response.Id = 3;
                        contentId = "baik.";
                        contentEn = "good state.";
                    }
                    else if (avg > 2.0)
                    {
                        response.Id = 2;
                        contentId = "cukup.";
                        contentEn = "fair state.";
                    }
                    else
                    {
                        response.Id = 1;
                        contentId = "kurang.";
                        contentEn = "poor state.";
                    }
                }
                else if (survey.Title.StartsWith("HR Diagnostics"))
                {
                    prefixId = "Pengelolaan SDM di organisasi anda";
                    prefixEn = "HR management in your organization is";
                    if (total >= 17.0)
                    {
                        response.Id = 5;
                        contentId = "sungguh sangat baik.";
                        contentEn = "excellent state.";
                    }
                    else if (total >= 14.0)
                    {
                        response.Id = 4;
                        contentId = "sangat baik.";
                        contentEn = "very good state.";
                    }
                    else if (total >= 10.0)
                    {
                        response.Id = 3;
                        contentId = "baik.";
                        contentEn = "good state.";
                    }
                    else if (total >= 6.0)
                    {
                        response.Id = 2;
                        contentId = "cukup.";
                        contentEn = "fair state.";
                    }
                    else
                    {
                        response.Id = 1;
                        contentId = "kurang.";
                        contentEn = "poor state.";
                    }
                }
                else if (survey.Title.StartsWith("Strategy and Performance Execution"))
                {
                    prefixId = "Pengelolaan strategi di organisasi anda";
                    prefixEn = "Strategy management in your organization is";
                    avg = total / 27;
                    if (avg > 3.5)
                    {
                        response.Id = 5;
                        contentId = "sungguh sangat baik.";
                        contentEn = "excellent state.";
                    }
                    else if (avg > 3.0)
                    {
                        response.Id = 4;
                        contentId = "sangat baik.";
                        contentEn = "very good state.";
                    }
                    else if (avg > 2.5)
                    {
                        response.Id = 3;
                        contentId = "baik.";
                        contentEn = "good state.";
                    }
                    else if (avg > 2.0)
                    {
                        response.Id = 2;
                        contentId = "cukup.";
                        contentEn = "fair state.";
                    }
                    else
                    {
                        response.Id = 1;
                        contentId = "kurang.";
                        contentEn = "poor state.";
                    }
                }
                else if (survey.Title.StartsWith("Organization Diagnostics"))
                {
                    avg = total / 29;
                    if (avg > 4.5)
                    {
                        response.Id = 5;
                        contentId = "sungguh sangat baik.";
                        contentEn = "excellent state.";
                    }
                    else if (avg > 4.0)
                    {
                        response.Id = 4;
                        contentId = "sangat baik.";
                        contentEn = "very good state.";
                    }
                    else if (avg > 3.0)
                    {
                        response.Id = 3;
                        contentId = "baik.";
                        contentEn = "good state.";
                    }
                    else if (avg > 2.0)
                    {
                        response.Id = 2;
                        contentId = "cukup.";
                        contentEn = "fair state.";
                    }
                    else
                    {
                        response.Id = 1;
                        contentId = "kurang.";
                        contentEn = "poor state.";
                    }
                }
                else if (survey.Title.StartsWith("HR Business Partner")) {
                    response.Id = 1;
                    prefixEn = "";
                    prefixId = "";
                    contentId = "Terima kasih untuk keikutsertaan anda.";
                    contentEn = "Thank you for your participation.";
                }
                else if (survey.Title.StartsWith("Digital"))
                {
                    response.Id = 4;
                    prefixEn = "";
                    prefixId = "";
                    contentId = uuid;
                    contentEn = uuid;
                    /*
                    prefixId = "Process digitalisasi di organisasi anda";
                    prefixEn = "Digitalization process in your organization is";
                    avg = total / 21;
                    if (avg > 3.5)
                    {
                        response.Id = 5;
                        contentId = "sungguh sangat baik.";
                        contentEn = "excellent state.";
                    }
                    else if (avg > 3.0)
                    {
                        response.Id = 4;
                        contentId = "sangat baik.";
                        contentEn = "very good state.";
                    }
                    else if (avg > 2.5)
                    {
                        response.Id = 3;
                        contentId = "baik.";
                        contentEn = "good state.";
                    }
                    else if (avg > 2.0)
                    {
                        response.Id = 2;
                        contentId = "cukup.";
                        contentEn = "fair state.";
                    }
                    else
                    {
                        response.Id = 1;
                        contentId = "kurang.";
                        contentEn = "poor state.";
                    }
                    */
                }
                else if (survey.Title.Contains("Leadership"))
                {
                    /*
                    prefixId = "Kepemimpinan di organisasi anda";
                    prefixEn = "Leadership in your organization is";
                    avg = total / 22;
                    if (avg > 3.5)
                    {
                        response.Id = 5;
                        contentId = "sungguh sangat baik.";
                        contentEn = "excellent.";
                    }
                    else if (avg > 3.0)
                    {
                        response.Id = 4;
                        contentId = "sangat baik.";
                        contentEn = "very good.";
                    }
                    else
                    {
                        response.Id = 2;
                        contentId = "cukup.";
                        contentEn = "fair state.";
                    }
                    */
                    prefixId = "";
                    prefixEn = "";
                    response.Id = 5;
                    contentId = uuid;
                    contentEn = uuid;
                }
                else if (survey.Title.Contains("Akhlak"))
                {
                    prefixId = "";
                    prefixEn = "";
                    response.Id = 5;
                    contentId = uuid;
                    contentEn = uuid;
                }
                else if (survey.Title.Equals("Online BSC Quiz"))
                {
                    prefixId = "";
                    prefixEn = "";
                    response.Id = 6;
                    contentId = uuid;
                    contentEn = uuid;
                }
                else if (survey.Title.StartsWith("Employee Engagement & Digital"))
                {
                    response.Id = 4;
                    prefixEn = "";
                    prefixId = "";
                    contentId = "Terima kasih untuk keikutsertaan anda.";
                    contentEn = "Thank you for your participation.";
                }
            }

            if (!prefixId.Equals("") && !prefixEn.Equals(""))
            {
                response.Text = new DualLanguage(string.Join(" ", new[] { prefixEn, contentEn }), string.Join(" ", new[] { prefixId, contentId }));
            }
            else
            {
                response.Text = new DualLanguage(contentEn, contentId);
            }

            if (!survey.Title.StartsWith("HR Business Partner") && /* !survey.Title.StartsWith("Digital") && */ !survey.Title.Contains("Akhlak") && !survey.Title.Contains("Leadership") && !survey.Title.Contains("Engagement"))
            {
                // await SendEmailNotification(surveyId, uuid);
            }

            return response;
        }

        private async Task<int> SendEmailNotificationToOwner(int surveyId, string uuid, WebSurveyOwner owner, DateTime time)
        {
            if (_options.Environment.Equals("Production"))
            {
                WebSurvey survey = _context.WebSurveys.Find(surveyId);

                List<IndicatorText> txs = await GetCoverItemsFromOwner(owner);

                string title = survey.Title;
                List<int> userIds = new List<int>();

                foreach (string s in survey.EmailToUserIds.Split(","))
                {
                    try
                    {
                        userIds.Add(Int32.Parse(s));
                    }
                    catch
                    {
                    }
                }

                var query = from u in _context.Users
                            where userIds.Contains(u.ID)
                            select new
                            {
                                u.FirstName,
                                u.UserName
                            };


                var us = await query.ToListAsync();

                EmailMessage message = new EmailMessage();
                List<EmailAddress> senders = new List<EmailAddress>();
                senders.Add(new EmailAddress()
                {
                    Name = "Web Admin",
                    Address = "cs@gmlperformance.co.id"
                });

                List<EmailAddress> recipients = new List<EmailAddress>();
                foreach (var u in us)
                {
                    recipients.Add(new EmailAddress()
                    {
                        Name = u.FirstName,
                        Address = u.UserName
                    });
                }

                message.FromAddresses = senders;
                message.ToAddresses = recipients;
                message.Subject = "URGENT: Survey Group Request";
                message.Content = "<p>" + "" + "</p>";
                message.Content += "<p>User berikut ini telah membuat group untuk survey " + title + "</p>";
                message.Content += "<p>";

                foreach (IndicatorText tx in txs)
                {
                    message.Content += tx.Indicator + ": " + tx.Text + "<br/>";
                }
                message.Content += "</p>";

                message.Content += "<p>Silakan klik link di bawah ini untuk melihat laporan survey </p>";
                message.Content += string.Join("", new[] { "<p>", "https://assessment.gmlperformance.com/survey/report/", surveyId.ToString(), "/", uuid, "</p>" });

                message.Content += "<p>Harap dicatat bahwa link di atas sebaiknya diakses <b>SESUDAH tanggal " + time.ToString("d MMM yyyy") + "</b> untuk memberikan kesempatan kepada responden untuk mengisi survey ini terlebih dahulu.</p>";

                _emailService.Send(message);

                return recipients.Count();
            }

            return 0;
        }


        private async Task<int> SendEmailNotification(int surveyId, string uuid)
        {
            if (_options.Environment.Equals("Production"))
            {
                WebSurvey survey = _context.WebSurveys.Find(surveyId);

                List<IndicatorText> txs = await GetCoverItems(surveyId, uuid);

                string title = survey.Title;
                List<int> userIds = new List<int>();

                foreach (string s in survey.EmailToUserIds.Split(","))
                {
                    try
                    {
                        userIds.Add(Int32.Parse(s));
                    }
                    catch
                    {
                    }
                }

                var query = from u in _context.Users
                         where userIds.Contains(u.ID)
                         select new
                         {
                             u.FirstName,
                             u.UserName
                         };


                var us = await query.ToListAsync();

                EmailMessage message = new EmailMessage();
                List<EmailAddress> senders = new List<EmailAddress>();
                senders.Add(new EmailAddress()
                {
                    Name = "Web Admin",
                    Address = "cs@gmlperformance.co.id"
                });

                List<EmailAddress> recipients = new List<EmailAddress>();
                foreach (var u in us)
                {
                    recipients.Add(new EmailAddress()
                    {
                        Name = u.FirstName,
                        Address = u.UserName
                    });
                }
                
                message.FromAddresses = senders;
                message.ToAddresses = recipients;
                message.Subject = "URGENT: Survey Completion";
                message.Content = "<p>" + "" + "</p>";
                message.Content += "<p>User berikut ini telah mengisi survey " + title + "</p>";
                message.Content += "<p>";

                foreach (IndicatorText tx in txs)
                {
                    message.Content += tx.Indicator + ": " + tx.Text + "<br/>";
                }
                message.Content += "</p>";

                message.Content += "<p>Silakan klik link di bawah ini untuk melihat laporan survey </p>";
                message.Content += string.Join("", new[] { "<p>", "https://assessment.gmlperformance.com/survey/report/", surveyId.ToString(), "/", uuid, "</p>" });

                _emailService.Send(message);
                return recipients.Count();
            }

            return 0;
        }
        private bool SurveyExists(int id)
        {
            return _context.WebSurveys.Any(e => e.Id == id);
        }
        private bool CheckBasicAuth(string authHeader)
        {
            authHeader = authHeader.Trim();
            if (authHeader.Equals(""))
            {
                return false;
            }

            string encodedCredentials = authHeader.Substring(6);
            var credentialBytes = Convert.FromBase64String(encodedCredentials);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
            var username = credentials[0];
            var password = credentials[1];
            if (username != "onegmlapi" || password != "O1n6e0G4M7L")
            {
                return false;
            }

            return true;
        }


        private async Task<List<ResultItem>> GetGroupQuadrantData(int surveyId, string uuid)
        {
            string sql = "select AVG(Cast(ri.Value as Float)) as Val, d.ItemText, d.OrderNumber from dbo.WebSurveyDimensions as d " +
                "join dbo.WebSurveyItemDimensions as id on d.Id = id.DimensionId " +
                "join dbo.WebSurveyItems as item on id.ItemId = item.Id " +
                "join dbo.WebSurveyResponses as response on item.Id = response.ItemId " +
                "join dbo.WebSurveyRatingItems as ri on response.RatingId = ri.Id " +
                "where d.SurveyId = " + surveyId.ToString() + " and " + @" response.GroupUUID LIKE '" + uuid + @"' " +
                "group by d.ItemText, d.OrderNumber " +
                "order by d.OrderNumber";

            return await _context.ResultItems.FromSql(sql).ToListAsync<ResultItem>();


        }
        private async Task<List<ResultItem>> GetQuadrantData(int surveyId, string uuid)
        {
            string sql = "select AVG(Cast(ri.Value as Float)) as Val, d.ItemText, d.OrderNumber from dbo.WebSurveyDimensions as d " +
                "join dbo.WebSurveyItemDimensions as id on d.Id = id.DimensionId " +
                "join dbo.WebSurveyItems as item on id.ItemId = item.Id " +
                "join dbo.WebSurveyResponses as response on item.Id = response.ItemId " +
                "join dbo.WebSurveyRatingItems as ri on response.RatingId = ri.Id " +
                "where d.SurveyId = " + surveyId.ToString() + " and " + @" response.uuid LIKE '" + uuid + @"' " +
                "group by d.ItemText, d.OrderNumber " +
                "order by d.OrderNumber";

            return await _context.ResultItems.FromSql(sql).ToListAsync<ResultItem>();


        }

    }
}