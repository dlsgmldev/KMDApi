using System;
using System.Collections.Generic;
using System.Linq;
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
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("QuBisaPolicy")]
    public class InsightController : ControllerBase
    {
        private readonly DefaultContext _context;
        private readonly FileService _fileService;
        private DataOptions _options;

        public InsightController(DefaultContext context, Microsoft.Extensions.Options.IOptions<DataOptions> options, FileService fileService)
        {
            _context = context;
            _options = options.Value;
            _fileService = fileService;
        }


        /**
         * @api {get} /Insight/{publish} GET list insight
         * @apiVersion 1.0.0
         * @apiName GetListInsight
         * @apiGroup Survey
         * @apiPermission Apiuser
         * 
         * @apiParam {Number} publish         0 untuk draft, 1 untuk publish
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
        /*
        [Authorize(Policy = "ApiUser")]
        [HttpGet("{publish}")]
        public async Task<ActionResult<List<WorkshopParticipantWithClient>>> GetListInsight(int projectId)
        {

        }
        */
    }
}
