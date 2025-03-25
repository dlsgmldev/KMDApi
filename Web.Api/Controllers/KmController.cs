using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KDMApi.DataContexts;
using KDMApi.Models;
using KDMApi.Models.Crm;
using KDMApi.Models.Km;
using Microsoft.AspNetCore.Authorization;
using KDMApi.Services;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.AspNetCore.Cors;
using KDMApi.Models.Insight;
using System.Drawing.Imaging;
using System.IO;
using KDMApi.Models.Helper;
using System.Text;

namespace KDMApi.Controllers
{
    [Route("v1/[controller]")]
    [ApiController]
    [EnableCors("QuBisaPolicy")]
    public class KmController : ControllerBase
    {
        private static string separator = "<!>";

        private readonly DefaultContext _context;
        private readonly ActivityLogService _activityLogService;
        private DataOptions _options;
        private readonly FileService _fileService;
        public KmController(DefaultContext context, ActivityLogService activityLogService, Microsoft.Extensions.Options.IOptions<DataOptions> options, FileService fileService)
        {
            _context = context;
            _activityLogService = activityLogService;
            _options = options.Value;
            _fileService = fileService;
        }

        /**
         * @api {get} /km/tree Get tree year first
         * @apiVersion 1.0.0
         * @apiName GetTree
         * @apiGroup KM
         * @apiPermission ApiUser
         * @apiDescription Mendapatkan data untuk tree view, dengan struktur Tribes, Tahun, Perusahaan, Project
         * 
         * @apiSuccessExample Success-Response:
         *   [
         *     {
         *       "id": 1,
         *       "text": "Strategy and Execution",
         *       "children": [
         *         {
         *           "id": 2,
         *           "text": "2020",
         *           "children": [
         *             {
         *               "id": 2,
         *               "text": "PT Bank Mandiri Tbk.",
         *               "children": [
         *                 {
         *                   "id": 4,
         *                   "text": "BSC Corporate",
         *                   "children": []
         *                 }
         *               ]
         *             },
         *             {
         *               "id": 3,
         *               "text": "Back Central Asia",
         *               "children": [
         *                 {
         *                   "id": 5,
         *                   "text": "BSC Corporate and IPM",
         *                   "children": []
         *                 }
         *               ]
         *             },
         *             {
         *               "id": 11,
         *               "text": "PT Maju Mundur",
         *               "children": [
         *                 {
         *                   "id": 6,
         *                   "text": "BSC Corporate, Cascading, and IPM",
         *                   "children": []
         *                 }
         *               ]
         *             }
         *           ]
         *         }
         *       ]
         *     },
         *     {
         *       "id": 2,
         *       "text": "Organizational Excellence",
         *       "children": []
         *     }
         *   ]
         * 
         * @apiError NotAuthorized Token salah.
         * 
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("tree")]
        public async Task<ActionResult<List<TreeNode>>> GetTree()
        {
            List<TreeNode> response = new List<TreeNode>();

            var tribes = await _context.CoreTribes.Where(a => a.IsDeleted == false).ToListAsync<CoreTribe>();
            foreach(var t in tribes)
            {
                response = AddToList(t.Id, t.Tribe, response);

                IQueryable<ProjectListInfo> queryProject;
                queryProject = from project in _context.KmProjects
                               join year in _context.KmYears
                               on project.YearId equals year.Id
                               join client in _context.CrmClients
                               on project.ClientId equals client.Id
                               join tribe in _context.CoreTribes
                               on project.TribeId equals tribe.Id
                               where project.IsDeleted == false && tribe.Id == t.Id
                               orderby tribe.OrderNumber, year.Year, project.Name
                               select new ProjectListInfo()
                               {
                                   Id = project.Id,
                                   Name = project.Name,
                                   Type = 1,
                                   YearId = year.Id,
                                   Year = year.Year.ToString(),
                                   TribeId = tribe.Id,
                                   Tribe = tribe.Tribe,
                                   ClientId = client.Id,
                                   Client = client.Company,
                                   Status = project.Status,
                                   ClientOwner = "",
                                   WorkshopTypeId = project.WorkshopTypeId
                               };

                List<ProjectListInfo> projects = await queryProject.ToListAsync();
                foreach (ProjectListInfo project in projects)
                {
                    //if (!IsInList(project.TribeId, response))
                    //{
                    //   response = AddToList(project.TribeId, project.Tribe, response);
                    //}
                    TreeNode curTribe = GetNodeById(project.TribeId, response);
                    if (curTribe != null)
                    {
                        if (!IsInList(project.YearId, curTribe.children))
                        {
                            curTribe.children = AddToList(project.YearId, project.Year, curTribe.children);
                        }
                        TreeNode curYear = GetNodeById(project.YearId, curTribe.children);
                        if (curYear != null)
                        {
                            if (!IsInList(project.ClientId, curYear.children))
                            {
                                curYear.children = AddToList(project.ClientId, project.Client, curYear.children);
                            }
                            TreeNode curClient = GetNodeById(project.ClientId, curYear.children);
                            if (curClient != null)
                            {
                                if (!IsInList(project.Id, curClient.children))
                                {
                                    curClient.children = AddToList(project.Id, project.Name, curClient.children);
                                }
                            }


                        }


                    }
                }


            }

            return response;
        }

        /**
         * @api {get} /km/treeclient Get tree client first
         * @apiVersion 1.0.0
         * @apiName GetTreeYearFirst
         * @apiGroup KM
         * @apiPermission ApiUser
         * @apiDescription Mendapatkan data untuk tree view, dengan struktur Tribes, Perusahaan, Tahun
         * 
         * @apiSuccessExample Success-Response:
         *   [
         *       {
         *           "id": 1,
         *           "text": "Strategy & Execution Solutions",
         *           "children": [
         *               {
         *                   "id": 141,
         *                   "text": "3M INDONESIA, PT (MINNESOTA MINING MANUFACTURING)",
         *                   "children": [
         *                       {
         *                           "id": 1,
         *                           "text": "2019",
         *                           "children": []
         *                       }
         *                   ]
         *               },
         *               {
         *                   "id": 177,
         *                   "text": "ABM INVESTAMA Tbk, PT",
         *                   "children": [
         *                       {
         *                           "id": 14,
         *                           "text": "2018",
         *                           "children": []
         *                       }
         *                   ]
         *               }
         *           ]
         *       },
         *       {
         *           "id": 2,
         *           "text": "Organization Excellence Solutions",
         *           "children": [
         *               {
         *                   "id": 1873,
         *                   "text": "BURSA EFEK INDONESIA (IDX)",
         *                   "children": [
         *                       {
         *                           "id": 1,
         *                           "text": "2019",
         *                           "children": []
         *                       }
         *                   ]
         *               },
         *               {
         *                   "id": 6261,
         *                   "text": "GRAND INDONESIA, PT",
         *                   "children": [
         *                       {
         *                           "id": 1,
         *                           "text": "2019",
         *                           "children": []
         *                       }
         *                   ]
         *               }
         *           ]
         *       },
         *       {
         *           "id": 3,
         *           "text": "Assessment Center Solutions",
         *           "children": []
         *       }
         *   ]
         * 
         * @apiError NotAuthorized Token salah.
         * 
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("treeclient")]
        public async Task<ActionResult<List<TreeNode>>> GetClientFirst()
        {
            List<TreeNode> response = new List<TreeNode>();

            var tribes = await _context.CoreTribes.Where(a => a.IsDeleted == false).ToListAsync<CoreTribe>();
            foreach (var t in tribes)
            {
                response = AddToList(t.Id, t.Tribe, response);

                if (!t.Shortname.ToLower().Equals("retail"))
                {
                    IQueryable<ProjectListInfo> queryProject;
                    queryProject = from project in _context.KmProjects
                                   join year in _context.KmYears
                                   on project.YearId equals year.Id
                                   join client in _context.CrmClients
                                   on project.ClientId equals client.Id
                                   join tribe in _context.CoreTribes
                                   on project.TribeId equals tribe.Id
                                   where project.IsDeleted == false && tribe.Id == t.Id
                                   orderby tribe.OrderNumber, client.Company, year.Year
                                   select new ProjectListInfo()
                                   {
                                       Id = project.Id,
                                       Name = project.Name,
                                       Type = 1,
                                       YearId = year.Id,
                                       Year = year.Year.ToString(),
                                       TribeId = tribe.Id,
                                       Tribe = tribe.Tribe,
                                       ClientId = client.Id,
                                       Client = client.Company,
                                       Status = project.Status,
                                       ClientOwner = "",
                                       WorkshopTypeId = project.WorkshopTypeId
                                   };

                    List<ProjectListInfo> projects = await queryProject.ToListAsync();
                    foreach (ProjectListInfo project in projects)
                    {
                        //if (!IsInList(project.TribeId, response))
                        //{
                        //   response = AddToList(project.TribeId, project.Tribe, response);
                        //}
                        TreeNode curTribe = GetNodeById(project.TribeId, response);
                        if (curTribe != null)
                        {
                            if (!IsInList(project.ClientId, curTribe.children))
                            {
                                curTribe.children = AddToList(project.ClientId, project.Client, curTribe.children);
                            }

                            TreeNode curClient = GetNodeById(project.ClientId, curTribe.children);
                            if (curClient != null)
                            {
                                if (!IsInList(project.YearId, curClient.children))
                                {
                                    curClient.children = AddToList(project.YearId, project.Year, curClient.children);
                                }
                            }

                            // Additional Clients
                            var q = from aclient in _context.KmProjectAdditionalClients
                                    join client in _context.CrmClients
                                    on aclient.ClientId equals client.Id
                                    join p in _context.KmProjects
                                    on aclient.ProjectId equals p.Id
                                    join year in _context.KmYears
                                    on p.YearId equals year.Id
                                    where aclient.ProjectId == project.Id && !aclient.IsDeleted
                                    select new
                                    {
                                        ClientId = client.Id,
                                        ClientName = client.Company,
                                        YearId = year.Id,
                                        Year = year.Year.ToString()
                                    };

                            var objs = await q.ToListAsync();
                            foreach (var obj in objs)
                            {
                                if (!IsInList(obj.ClientId, curTribe.children))
                                {
                                    curTribe.children = AddToList(obj.ClientId, obj.ClientName, curTribe.children);
                                }

                                TreeNode curAddClient = GetNodeById(obj.ClientId, curTribe.children);
                                if (curAddClient != null)
                                {
                                    if (!IsInList(project.YearId, curAddClient.children))
                                    {
                                        curAddClient.children = AddToList(obj.YearId, obj.Year, curAddClient.children);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    CrmClient ps = _context.CrmClients.Where(a => a.Company.StartsWith("Public Seminar")).FirstOrDefault();

                    if(ps != null)
                    {
                        IQueryable<ProjectListInfo> queryProject;
                        queryProject = from project in _context.KmProjects
                                       join year in _context.KmYears
                                       on project.YearId equals year.Id
                                       join tribe in _context.CoreTribes
                                       on project.TribeId equals tribe.Id
                                       where project.IsDeleted == false && tribe.Id == t.Id
                                       orderby tribe.OrderNumber, year.Year
                                       select new ProjectListInfo()
                                       {
                                           Id = project.Id,
                                           Name = project.Name,
                                           Type = 1,
                                           YearId = year.Id,
                                           Year = year.Year.ToString(),
                                           TribeId = tribe.Id,
                                           Tribe = tribe.Tribe,
                                           ClientId = 0,
                                           Client = "",
                                           Status = project.Status,
                                           ClientOwner = "",
                                           WorkshopTypeId = project.WorkshopTypeId
                                       };

                        List<ProjectListInfo> projects = await queryProject.ToListAsync();
                        foreach (ProjectListInfo project in projects)
                        {
                            TreeNode curTribe = GetNodeById(project.TribeId, response);
                            if (curTribe != null)
                            {
                                if (!IsInList(ps.Id, curTribe.children))
                                {
                                    curTribe.children = AddToList(ps.Id, ps.Company, curTribe.children);
                                }

                                TreeNode curClient = GetNodeById(ps.Id, curTribe.children);
                                if (curClient != null)
                                {
                                    if (!IsInList(project.YearId, curClient.children))
                                    {
                                        curClient.children = AddToList(project.YearId, project.Year, curClient.children);
                                    }
                                }

                            }
                        }

                    }

                }


            }

            return response;
        }

        // GET: v1/projects/5
        /**
         * @api {get} /km/projects/{projectId}/{folderId} Get project 
         * @apiVersion 1.0.0
         * @apiName GetProject
         * @apiGroup KM
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} projectId        Id dari project yang bersangkutan, atau 0 untuk OneGML
         * @apiParam {Number} folderId         Id dari folder yang mau diambil isinya, atau 0 untuk root folder
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "project": {
         *           "id": 173,
         *           "name": "Project BSC",
         *           "status": "Closed",
         *           "venue": "",
         *           "startDate": "2019-09-10T03:17:29.814",
         *           "endDate": "2019-10-10T03:17:29.814",
         *           "keyWord": "BSC",
         *           "yearId": 1,
         *           "clientId": 141,
         *           "tribeId": 1,
         *           "workshopTypeId": 0,
         *           "createdDate": "2020-09-10T11:54:18.9674168",
         *           "createdBy": 1,
         *           "lastUpdated": "2020-09-10T11:54:18.9674168",
         *           "lastUpdatedBy": 1,
         *           "isDeleted": false,
         *           "deletedBy": 0,
         *           "deletedDate": null
         *       },
         *       "trainers": [
         *           {
         *               "id": 1,
         *               "text": "Rifky"
         *           }
         *       ],
         *       "projectAdvisor": {
         *           "id": 2,
         *           "text": "Rafdi"
         *       },
         *       "projectLeader": {
         *           "id": 3,
         *           "text": "Leviana Wijaya"
         *       },
         *       "projectMembers": [
         *           {
         *               "id": 4,
         *               "text": "Grace Louise Harsa"
         *           },
         *           {
         *               "id": 5,
         *               "text": "Lydia Lie"
         *           }
         *       ],
         *       "clientProjectOwner": {
         *           "id": 108,
         *           "text": "Ahmad Riyadi, Mr"
         *       },
         *       "clientProjectLeader": {
         *           "id": 109,
         *           "text": "Kokko Cattaka, Mr"
         *       },
         *       "clientProjectMembers": [
         *           {
         *               "id": 110,
         *               "text": "Margaretha Nike, Ms"
         *           },
         *           {
         *               "id": 111,
         *               "text": "Patrisia Lesmana, Ms"
         *           }
         *       ],
         *       "products": [
         *           {
         *               "id": 1,
         *               "text": "Vision, Mission & Destination Statement"
         *           },
         *           {
         *               "id": 2,
         *               "text": "Strategy Formulation"
         *           },
         *           {
         *               "id": 3,
         *               "text": "Balanced Scorecard Development"
         *           }
         *       ],
         *       "participants": [],
         *       "tribe": {
         *           "id": 1,
         *           "text": "Strategy & Execution Solutions"
         *       },
         *       "client": {
         *           "id": 141,
         *           "text": "3M INDONESIA, PT (MINNESOTA MINING MANUFACTURING)"
         *       },
         *       "year": 2019,
         *       "content": {
         *           "id": 0,
         *           "name": "Root",
         *           "location": "PETROKIMIA GRESIK, PT / 2019 / AWARENESS CASCADING AND ALIGNMENT KPI FOR PT PETROKIMIA GRESIK",
         *           "Owner": "",
         *           "OwnerId": 0
         *           "date": "1970-01-01T00:00:00",
         *           "folders": [
         *               {
         *                   "id": 344,
         *                   "name": "Materi Workshop",
         *                   "location": "PETROKIMIA GRESIK, PT / 2019 / AWARENESS CASCADING AND ALIGNMENT KPI FOR PT PETROKIMIA GRESIK",
         *                   "fileType": "Folder",
         *                   "Owner": "",
         *                   "OwnerId": 0
         *                   "date": "2020-03-31T16:33:58.7136379"
         *               }
         *           ],
         *           "files": []
         *       },
         *       "breadcrump": [
         *           {
         *               "id": 1,
         *               "text": "Strategy & Execution Solutions"
         *           },
         *           {
         *               "id": 10805,
         *               "text": "PETROKIMIA GRESIK, PT"
         *           },
         *           {
         *               "id": 1,
         *               "text": "2019"
         *           },
         *           {
         *               "id": 261,
         *               "text": "test project selasa"
         *           },
         *           {
         *               "id": 344,
         *               "text": "Materi Workshop"
         *           }
         *       ]
         *       "errors": [
         *           {
         *               "code": "0",
         *               "description": ""
         *           }
         *       ]
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         * 
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("projects/{projectId}/{folderId}")]
        public async Task<ActionResult<GetProjectResponse>> GetProject(int projectId, int folderId)
        {
            GetProjectResponse response = new GetProjectResponse();

            if (projectId == 0)
            {
                response.Content = await GetFolderContent(folderId, 0, 1);
                List<GenericInfo> pre1 = new List<GenericInfo>();
                pre1.Add(new GenericInfo()
                {
                    Id = 0,
                    Text = "OneGML"
                });
                response.Breadcrump = GetBreadcrump(folderId, pre1);

            }
            else
            {
                KmProject project = _context.KmProjects.Where(a => a.Id == projectId && a.IsDeleted == false).FirstOrDefault();
                if (project == null || project.Id == 0)
                {
                    return NotFound();
                }


                response.Project = project;
                response.Trainers = GetTeams("facilitator", project.Id, true);
                response.ProjectAdvisor = GetTeam("pa", project.Id, true);
                response.ProjectLeader = GetTeam("pl", project.Id, true);
                response.ProjectMembers = GetTeams("pc", project.Id, true);
                response.ClientProjectOwner = GetTeam("cpo", project.Id, false);
                response.ClientProjectLeader = GetTeam("cpl", project.Id, false);
                response.ClientProjectMembers = GetTeams("cpm", project.Id, false);
                response.Participants = GetTeams("participant", project.Id, false);
                response.Products = GetProducts(project.Id);
                response.Tribe = GetTribe(project.TribeId);
                response.Client = GetClient(project.ClientId);
                response.Year = GetYear(project.YearId);
                response.Content = await GetFolderContent (folderId, project.Id, 0);

                List<GenericInfo> pre = new List<GenericInfo>();
                pre.Add(new GenericInfo()
                {
                    Id = project.TribeId,
                    Text = response.Tribe.Text
                });
                pre.Add(new GenericInfo()
                {
                    Id = project.ClientId,
                    Text = response.Client.Text
                });
                pre.Add(new GenericInfo()
                {
                    Id = project.YearId,
                    Text = response.Year.ToString()
                }); pre.Add(new GenericInfo()
                {
                    Id = project.Id,
                    Text = project.Name
                });
                response.Breadcrump = GetBreadcrump(folderId, pre);
            }

            return response;
        }

        /**
         * @api {post} /km/projects POST project
         * @apiVersion 1.0.0
         * @apiName PostProject
         * @apiGroup KM
         * @apiPermission ApiUser
         * @apiDescription workshopTypeId = 0 untuk project, 1 = In-house workshops, dan 2 = Public workshops, Venue = "" untuk project, Status itu "Closed" atau "Open"
         * 
         * @apiParamExample {json} Request-Example:
         * {
         *   "name": "Project BSC",
         *   "status": "Closed",
         *   "venue": "",
         *   "startDate": "2019-09-10T03:17:29.814Z",
         *   "endDate": "2019-10-10T03:17:29.814Z",
         *   "keyWords": [
         *     "BSC"
         *   ],
         *   "clientId": 141,
         *   "tribeId": 1,
         *   "workshopTypeId": 0,
         *   "userId": 1,
         *   "trainerIds": [
         *     1
         *   ],
         *   "projectAdvisorId": 2,
         *   "projectLeaderId": 3,
         *   "projectMemberIds": [
         *     4,5
         *   ],
         *   "clientProjectOwnerId": 108,
         *   "clientProjectLeaderId": 109,
         *   "clientProjectMemberIds": [
         *     110,111
         *   ],
         *   "productIds": [
         *     1,2,3
         *   ]
         * }
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "project": {
         *           "id": 173,
         *           "name": "Project BSC",
         *           "status": "Closed",
         *           "venue": "",
         *           "startDate": "2019-09-10T03:17:29.814Z",
         *           "endDate": "2019-10-10T03:17:29.814Z",
         *           "keyWord": "BSC",
         *           "yearId": 1,
         *           "clientId": 141,
         *           "tribeId": 1,
         *           "workshopTypeId": 0,
         *           "createdDate": "2020-09-10T11:54:18.9674168+07:00",
         *           "createdBy": 1,
         *           "lastUpdated": "2020-09-10T11:54:18.9674168+07:00",
         *           "lastUpdatedBy": 1,
         *           "isDeleted": false,
         *           "deletedBy": 0,
         *           "deletedDate": null
         *       },
         *       "trainers": [
         *           {
         *               "id": 1,
         *               "text": "Rifky"
         *           }
         *       ],
         *       "projectAdvisor": {
         *           "id": 2,
         *           "text": "Rafdi"
         *       },
         *       "projectLeader": {
         *           "id": 3,
         *           "text": "Leviana Wijaya"
         *       },
         *       "projectMembers": [
         *           {
         *               "id": 4,
         *               "text": "Grace Louise Harsa"
         *           },
         *           {
         *               "id": 5,
         *               "text": "Lydia Lie"
         *           }
         *       ],
         *       "clientProjectOwner": {
         *           "id": 108,
         *           "text": "Ahmad Riyadi, Mr"
         *       },
         *       "clientProjectLeader": {
         *           "id": 109,
         *           "text": "Kokko Cattaka, Mr"
         *       },
         *       "clientProjectMembers": [
         *           {
         *               "id": 110,
         *               "text": "Margaretha Nike, Ms"
         *           },
         *           {
         *               "id": 111,
         *               "text": "Patrisia Lesmana, Ms"
         *           }
         *       ],
         *       "products": [
         *           {
         *               "id": 1,
         *               "text": "Vision, Mission & Destination Statement"
         *           },
         *           {
         *               "id": 2,
         *               "text": "Strategy Formulation"
         *           },
         *           {
         *               "id": 3,
         *               "text": "Balanced Scorecard Development"
         *           }
         *       ],
         *       "participants": [],
         *       "tribe": {
         *           "id": 1,
         *           "text": "Strategy & Execution Solutions"
         *       },
         *       "client": {
         *           "id": 141,
         *           "text": "3M INDONESIA, PT (MINNESOTA MINING MANUFACTURING)"
         *       },
         *       "year": 2019,
         *       "content": null,
         *       "errors": [
         *           {
         *               "code": "0",
         *               "description": ""
         *           }
         *       ]
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         * 
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost("projects")]
        public async Task<ActionResult<GetProjectResponse>> PostProject(AddProjectRequest request)
        {
            GetProjectResponse response = new GetProjectResponse();
            DateTime now = DateTime.Now;

            KmProject project = new KmProject()
            {
                Name = request.Name,
                Status = request.Status,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                KeyWord = String.Join(",", request.KeyWords),
                ClientId = request.ClientId,
                TribeId = request.TribeId,
                CreatedDate = now,
                CreatedBy = request.UserId,
                LastUpdated = now,
                LastUpdatedBy = request.UserId,
                IsDeleted = false,
                DeletedBy = 0
            };
            if(request.WorkshopTypeId > 0)
            {
                project.WorkshopTypeId = request.WorkshopTypeId;
                project.Venue = request.Venue;
            }
            else
            {
                project.WorkshopTypeId = 0;
                project.Venue = "";
            }
            int year = project.StartDate.Year;

            KmYear projectYear = _context.KmYears.Where(a => a.Year == year && a.IsDeleted == false).FirstOrDefault();
            if(projectYear == null || projectYear.Id == 0)
            {
                KmYear newYear = new KmYear()
                {
                    Year = year,
                    CreatedDate = now,
                    CreatedBy = request.UserId,
                    LastUpdated = now,
                    LastUpdatedBy = request.UserId,
                    IsDeleted = false,
                    DeletedBy = 0
                };
                _context.KmYears.Add(newYear);
                await _context.SaveChangesAsync();
                projectYear = newYear;
            }
            project.YearId = projectYear.Id;

            try
            {
                _context.KmProjects.Add(project);
                await _context.SaveChangesAsync();

                if (request.WorkshopTypeId > 0)
                {
                    if (request.TrainerIds != null && request.TrainerIds.Count() > 0)
                    {
                        await AddInternalTeam("facilitator", request.TrainerIds, project.Id);
                    }
                }
                else
                {
                    if (request.ProjectAdvisorId > 0)
                    {
                        await AddInternalTeam("pa", new List<int>(new [] { request.ProjectAdvisorId }), project.Id);
                    }

                    if (request.ProjectLeaderId > 0)
                    {
                        await AddInternalTeam("pl", new List<int>(new[] { request.ProjectLeaderId }), project.Id);
                    }

                    await AddInternalTeam("pc", request.ProjectMemberIds, project.Id);
                }
                

                if(request.ClientProjectOwnerId > 0)
                {
                    await AddExternalTeam("cpo", request.ClientProjectOwnerId, project.ClientId, project.Id);
                }

                if(request.ClientProjectLeaderId > 0)
                {
                    await AddExternalTeam("cpl", request.ClientProjectLeaderId, project.ClientId, project.Id);
                }

                foreach(int id in request.ClientProjectMemberIds)
                {
                    if(id > 0)
                    {
                        await AddExternalTeam("cpm", id, project.ClientId, project.Id);
                    }
                }

                foreach(int pid in request.ProductIds)
                {
                    await AddProjectProduct(pid, project.Id);
                }

                response.Project = project;
                response.Trainers = GetTeams("facilitator", project.Id, true);
                response.ProjectAdvisor = GetTeam("pa", project.Id, true);
                response.ProjectLeader = GetTeam("pl", project.Id, true);
                response.ProjectMembers = GetTeams("pc", project.Id, true);
                response.ClientProjectOwner = GetTeam("cpo", project.Id, false);
                response.ClientProjectLeader = GetTeam("cpl", project.Id, false);
                response.ClientProjectMembers = GetTeams("cpm", project.Id, false);
                response.Participants = GetTeams("participant", project.Id, false);
                response.Products = GetProducts(project.Id);
                response.Tribe = GetTribe(project.TribeId);
                response.Client = GetClient(project.ClientId);
                response.Year = projectYear.Year;
            }
            catch
            {
                return BadRequest(new { error = "Error updating database." });
            }
            
            return response;
        }


        /**
         * @api {post} /km/participants POST participants
         * @apiVersion 1.0.0
         * @apiName AddWorkshopParticipants
         * @apiGroup KM
         * @apiPermission ApiUser
         * @apiDescription workshopTypeId = 0 untuk project, 1 = In-house workshops, dan 2 = Public workshops, Venue = "" untuk project, Status itu "Closed" atau "Open"
         * 
         * @apiParamExample {json} Request-Example:
         * {
         *   "projectId": 173,
         *   "userId": 1,
         *   "add": [
         *     {
         *       "clientId": 141,
         *       "participants": [
         *         {
         *           "id": 0,
         *           "name": "Andi Budiman",
         *           "salutation": "Mr.",
         *           "email": "andi@budiman.co.id",
         *           "phone": "08198989189",
         *           "department": "Marketing",
         *           "position": "Manager",
         *           "valid": true
         *         }
         *       ]
         *     }
         *   ]
         * }
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *     "code": "ok",
         *     "description": ""
         * }
         * 
         * @apiError NotAuthorized Token salah.
         * 
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost("participants")]
        public async Task<ActionResult<Error>> AddWorkshopParticipants(AddParticipantRequest request)
        {
            if (request.ProjectId <= 0 || !ProjectExists(request.ProjectId))
            {
                return NotFound(new Error("projectId", "Project doesn't exist."));
            }

            DateTime now = DateTime.Now;

            KmProject project = _context.KmProjects.Find(request.ProjectId);

            bool firstClient = true;

            foreach (WorkshopParticipant p in request.Add)
            {
                foreach(ContactInfo info in p.Participants)
                {
                    CrmContact contact = FindContactByEmail(info.Email);
                    if(contact != null && contact.Id > 0)
                    {
                        contact.Name = info.Name;
                        contact.Email1 = info.Email.Trim();
                        contact.Email2 = "";
                        contact.Email3 = "";
                        contact.Email4 = "";
                        contact.Phone1 = info.Phone.Trim();
                        contact.Phone2 = "";
                        contact.Phone3 = "";
                        contact.Salutation = "-";
                        contact.Department = info.Department;
                        contact.Position = info.Position;
                        contact.Valid = true;
                        contact.Source = "Workshop";
                        contact.CrmClientId = p.ClientId;
                        contact.LastUpdated = now;
                        contact.LastUpdatedBy = request.UserId;
                        contact.IsDeleted = false;
                        contact.DeletedBy = 0;

                        _context.Entry(contact).State = EntityState.Modified;
                        await _context.SaveChangesAsync();

                        await AddExternalTeam("participant", contact.Id, p.ClientId, request.ProjectId);
                    }
                    else
                    {
                        CrmContact crmContact = new CrmContact();
                        crmContact.Name = info.Name;
                        crmContact.Email1 = info.Email.Trim();
                        crmContact.Email2 = "";
                        crmContact.Email3 = "";
                        crmContact.Email4 = "";
                        crmContact.Phone1 = info.Phone.Trim();
                        crmContact.Phone2 = "";
                        crmContact.Phone3 = "";
                        crmContact.Salutation = "-";
                        crmContact.Department = info.Department;
                        crmContact.Position = info.Position;
                        crmContact.Valid = true;
                        crmContact.Source = "Workshop";
                        crmContact.CrmClientId = p.ClientId;
                        crmContact.CreatedDate = now;
                        crmContact.CreatedBy = request.UserId;
                        crmContact.LastUpdated = now;
                        crmContact.LastUpdatedBy = request.UserId;
                        crmContact.IsDeleted = false;
                        crmContact.DeletedBy = 0;

                        _context.CrmContacts.Add(crmContact);
                        await _context.SaveChangesAsync();

                        await AddExternalTeam("participant", crmContact.Id, p.ClientId, request.ProjectId);
                    }
                }

                // Sewaktu post workshop, project.ClientId = 0
                if(firstClient)
                {
                    if (project.ClientId != p.ClientId && p.ClientId != 0)
                    {
                        project.ClientId = p.ClientId;
                        _context.Entry(project).State = EntityState.Modified;
                    }
                    firstClient = false;
                }
                else
                {
                    // Satu workshop bisa punya beberapa client
                    KmProjectAdditionalClient client = new KmProjectAdditionalClient()
                    {
                        ProjectId = project.Id,
                        ClientId = p.ClientId,
                        CreatedDate = now,
                        CreatedBy = request.UserId,
                        LastUpdated = now,
                        LastUpdatedBy = request.UserId,
                        IsDeleted = false,
                        DeletedBy = 0,
                        DeletedDate = new DateTime(1970, 1, 1)
                    };
                    _context.KmProjectAdditionalClients.Add(client);
                }
            }

            await _context.SaveChangesAsync();

            return new Error("ok", "");
        }

        /**
         * @api {put} /km/participants PUT participants
         * @apiVersion 1.0.0
         * @apiName PutParticipant
         * @apiGroup KM
         * @apiPermission ApiUser
         * 
         * @apiParamExample {json} Request-Example:
         * {
         *   "projectId": 3,
         *   "userId": 1,
         *   "add": [
         *     {
         *       "clientId": 286,
         *       "participants": [
         *         {
         *           "id": 0,
         *           "name": "Orang Baru Juga",
         *           "salutation": "Mr",
         *           "email": "baru@orang.com",
         *           "phone": "089189898",
         *           "department": "Divisi Baru",
         *           "position": "Posisi Baru",
         *           "valid": true
         *         }
         *       ]
         *     }
         *   ]
         * }
         * 
         * @apiSuccessExample Success-Response:
         *  NoContent
         * 
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPut("participants")]
        public async Task<ActionResult<Error>> PutParticipant(AddParticipantRequest request)
        {
            DateTime now = DateTime.Now;

            try
            {
                KmProject project = await _context.KmProjects.FindAsync(request.ProjectId);
                if (project == null)
                {
                    return NotFound();
                }

                KmProjectTeamRole role = GetRole("participant");

                // Delete existing participants
                List<KmProjectExternalTeam> ts = _context.KmProjectExternalTeams.Where(a => a.RoleId == role.Id && a.ProjectId == request.ProjectId).ToList();
                foreach (KmProjectExternalTeam t in ts)
                {
                    _context.KmProjectExternalTeams.Remove(t);
                }

                // Delete existing additional clients
                List<KmProjectAdditionalClient> aclients = _context.KmProjectAdditionalClients.Where(a => a.ProjectId == request.ProjectId).ToList();
                foreach(KmProjectAdditionalClient a in aclients)
                {
                    _context.KmProjectAdditionalClients.Remove(a);
                }
                await _context.SaveChangesAsync();

                bool firstClient = true;

                foreach (WorkshopParticipant p in request.Add)
                {
                    foreach (ContactInfo info in p.Participants)
                    {
                        CrmContact contact = FindContactByEmail(info.Email);
                        if (contact != null && contact.Id > 0)
                        {
                            contact.Name = info.Name;
                            contact.Email1 = info.Email.Trim();
                            contact.Email2 = "";
                            contact.Email3 = "";
                            contact.Email4 = "";
                            contact.Phone1 = info.Phone.Trim();
                            contact.Phone2 = "";
                            contact.Phone3 = "";
                            contact.Salutation = "-";
                            contact.Department = info.Department;
                            contact.Position = info.Position;
                            contact.Valid = true;
                            contact.Source = "Workshop";
                            contact.CrmClientId = p.ClientId;
                            contact.LastUpdated = now;
                            contact.LastUpdatedBy = request.UserId;
                            contact.IsDeleted = false;
                            contact.DeletedBy = 0;

                            _context.Entry(contact).State = EntityState.Modified;
                            await _context.SaveChangesAsync();

                            await AddExternalTeam("participant", contact.Id, p.ClientId, request.ProjectId);
                        }
                        else
                        {
                            CrmContact crmContact = new CrmContact();
                            crmContact.Name = info.Name;
                            crmContact.Email1 = info.Email.Trim();
                            crmContact.Email2 = "";
                            crmContact.Email3 = "";
                            crmContact.Email4 = "";
                            crmContact.Phone1 = info.Phone.Trim();
                            crmContact.Phone2 = "";
                            crmContact.Phone3 = "";
                            crmContact.Salutation = "-";
                            crmContact.Department = info.Department;
                            crmContact.Position = info.Position;
                            crmContact.Valid = true;
                            crmContact.Source = "Workshop";
                            crmContact.CrmClientId = p.ClientId;
                            crmContact.CreatedDate = now;
                            crmContact.CreatedBy = request.UserId;
                            crmContact.LastUpdated = now;
                            crmContact.LastUpdatedBy = request.UserId;
                            crmContact.IsDeleted = false;
                            crmContact.DeletedBy = 0;

                            _context.CrmContacts.Add(crmContact);
                            await _context.SaveChangesAsync();

                            await AddExternalTeam("participant", crmContact.Id, p.ClientId, request.ProjectId);
                        }
                    }

                    // Sewaktu post workshop, project.ClientId = 0
                    if (firstClient)
                    {
                        if (project.ClientId != p.ClientId && p.ClientId != 0)
                        {
                            project.ClientId = p.ClientId;
                            _context.Entry(project).State = EntityState.Modified;
                        }
                        firstClient = false;
                    }
                    else
                    {
                        // Satu workshop bisa punya beberapa client
                        KmProjectAdditionalClient client = new KmProjectAdditionalClient()
                        {
                            ProjectId = project.Id,
                            ClientId = p.ClientId,
                            CreatedDate = now,
                            CreatedBy = request.UserId,
                            LastUpdated = now,
                            LastUpdatedBy = request.UserId,
                            IsDeleted = false,
                            DeletedBy = 0,
                            DeletedDate = new DateTime(1970, 1, 1)
                        };
                        _context.KmProjectAdditionalClients.Add(client);
                    }

                }
            }
            catch
            {
                return BadRequest();
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /**
         * @api {get} /km/participants/{projectId} GET participants
         * @apiVersion 1.0.0
         * @apiName GetWorkshopParticipants
         * @apiGroup KM
         * @apiPermission ApiUser
         * 
         * @apiSuccessExample Success-Response:
         *   [
         *       {
         *           "client": {
         *               "id": 286,
         *               "text": "PANTANG MENYERAH, PT"
         *           },
         *           "participants": [
         *               {
         *                   "id": 38383,
         *                   "name": "Orang Baru",
         *                   "salutation": "-",
         *                   "email": "baru@orang.com",
         *                   "phone": "089189898",
         *                   "department": "Divisi Baru",
         *                   "position": "Posisi Baru",
         *                   "valid": true
         *               }
         *           ]
         *       }
         *   ]
         * 
         * @apiError NotAuthorized Token salah.
         * 
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("participants/{projectId}")]
        public async Task<ActionResult<List<WorkshopParticipantWithClient>>> GetWorkshopParticipants(int projectId)
        {
            List<WorkshopParticipantWithClient> response = new List<WorkshopParticipantWithClient>();
            KmProjectTeamRole role = GetRole("participant");
            if (role != null)
            {
                var q1 = from project in _context.KmProjects
                            join client in _context.CrmClients
                            on project.ClientId equals client.Id
                            where project.Id == projectId
                            select new
                            {
                                ClientId = client.Id,
                                ClientName = client.Company,
                            };
                var objs = await q1.ToListAsync();
                foreach (var obj in objs)
                {
                    WorkshopParticipantWithClient c = new WorkshopParticipantWithClient();
                    c.Client.Id = obj.ClientId;
                    c.Client.Text = obj.ClientName;

                    response.Add(c);
                }

                // Additional clients
                var q3 = from aclient in _context.KmProjectAdditionalClients
                         join client in _context.CrmClients
                         on aclient.ClientId equals client.Id
                         where aclient.ProjectId == projectId && !aclient.IsDeleted
                         select new
                         {
                             ClientId = client.Id,
                             ClientName = client.Company,
                         };

                var objs2 = await q3.ToListAsync();
                foreach (var obj in objs2)
                {
                    WorkshopParticipantWithClient c = new WorkshopParticipantWithClient();
                    c.Client.Id = obj.ClientId;
                    c.Client.Text = obj.ClientName;

                    response.Add(c);
                }

                var q2 = from member in _context.KmProjectExternalTeams
                         join contact in _context.CrmContacts
                         on member.ContactId equals contact.Id
                         join client in _context.CrmClients
                         on contact.CrmClientId equals client.Id
                         where member.ProjectId == projectId && member.RoleId == role.Id && !contact.IsDeleted
                         select new 
                         {
                             Id = contact.Id,
                             Name = contact.Name,
                             Salutation = contact.Salutation,
                             Email = contact.Email1,
                             Phone = contact.Phone1,
                             Department = contact.Department,
                             Position = contact.Position,
                             Valid = contact.Valid,
                             ClientId = client.Id
                         };
                var ps = await q2.ToListAsync();
                foreach(var p in ps)
                {
                    foreach(WorkshopParticipantWithClient client in response)
                    {
                        if(client.Client.Id == p.ClientId)
                        {
                            ContactInfo info = new ContactInfo()
                            {
                                Id = p.Id,
                                Name = p.Name,
                                Salutation = p.Salutation,
                                Email = p.Email,
                                Phone = p.Phone,
                                Department = p.Department,
                                Position = p.Position,
                                Valid = p.Valid
                            };
                            client.Participants.Add(info);
                        }
                    }
                }
            }

            return response;
        }

        /**
         * @api {delete} /km/participants/{projectId}/{contactId} DELETE participant
         * @apiVersion 1.0.0
         * @apiName DeleteWorkshopParticipant
         * @apiGroup KM
         * @apiPermission ApiUser
         * 
         * @apiSuccessExample Success-Response:
         *   NoContent
         */
        [Authorize(Policy = "ApiUser")]
        [HttpDelete("participants/{projectId}/{contactId}")]
        public async Task<ActionResult> DeleteWorkshopParticipant(int projectId, int contactId)
        {
            KmProjectTeamRole role = GetRole("participant");

            KmProjectExternalTeam part = _context.KmProjectExternalTeams.Where(a => a.ProjectId == projectId && a.ContactId == contactId && a.RoleId == role.Id).FirstOrDefault();
            if(part == null)
            {
                return NotFound();
            }

            _context.KmProjectExternalTeams.Remove(part);
            await _context.SaveChangesAsync();

            return NoContent();
        }



        // PUT: v1/Km/5
        /**
         * @api {put} /km/projects/{id} PUT project
         * @apiVersion 1.0.0
         * @apiName PutProject
         * @apiGroup KM
         * @apiPermission ApiUser
         * @apiParam {Number} id        Id dari project yang bersangkutan, sama dengan id di request
         * 
         * @apiParamExample {json} Request-Example:
         * {
         *   "id": 173
         *   "name": "Project BSC",
         *   "status": "Closed",
         *   "venue": "",
         *   "startDate": "2019-09-10T03:17:29.814Z",
         *   "endDate": "2019-10-10T03:17:29.814Z",
         *   "keyWords": [
         *     "BSC"
         *   ],
         *   "clientId": 141,
         *   "tribeId": 1,
         *   "workshopTypeId": 0,
         *   "userId": 1,
         *   "trainerIds": [
         *     1
         *   ],
         *   "projectAdvisorId": 2,
         *   "projectLeaderId": 3,
         *   "projectMemberIds": [
         *     4,5
         *   ],
         *   "clientProjectOwnerId": 108,
         *   "clientProjectLeaderId": 109,
         *   "clientProjectMemberIds": [
         *     110,111
         *   ],
         *   "productIds": [
         *     1,2,3
         *   ]
         * }
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "project": {
         *           "id": 173,
         *           "name": "Project BSC",
         *           "status": "Closed",
         *           "venue": "",
         *           "startDate": "2019-09-10T03:17:29.814Z",
         *           "endDate": "2019-10-10T03:17:29.814Z",
         *           "keyWord": "BSC",
         *           "yearId": 1,
         *           "clientId": 141,
         *           "tribeId": 1,
         *           "workshopTypeId": 0,
         *           "createdDate": "2020-09-10T11:54:18.9674168+07:00",
         *           "createdBy": 1,
         *           "lastUpdated": "2020-09-10T11:54:18.9674168+07:00",
         *           "lastUpdatedBy": 1,
         *           "isDeleted": false,
         *           "deletedBy": 0,
         *           "deletedDate": null
         *       },
         *       "trainers": [
         *           {
         *               "id": 1,
         *               "text": "Rifky"
         *           }
         *       ],
         *       "projectAdvisor": {
         *           "id": 2,
         *           "text": "Rafdi"
         *       },
         *       "projectLeader": {
         *           "id": 3,
         *           "text": "Leviana Wijaya"
         *       },
         *       "projectMembers": [
         *           {
         *               "id": 4,
         *               "text": "Grace Louise Harsa"
         *           },
         *           {
         *               "id": 5,
         *               "text": "Lydia Lie"
         *           }
         *       ],
         *       "clientProjectOwner": {
         *           "id": 108,
         *           "text": "Ahmad Riyadi, Mr"
         *       },
         *       "clientProjectLeader": {
         *           "id": 109,
         *           "text": "Kokko Cattaka, Mr"
         *       },
         *       "clientProjectMembers": [
         *           {
         *               "id": 110,
         *               "text": "Margaretha Nike, Ms"
         *           },
         *           {
         *               "id": 111,
         *               "text": "Patrisia Lesmana, Ms"
         *           }
         *       ],
         *       "products": [
         *           {
         *               "id": 1,
         *               "text": "Vision, Mission & Destination Statement"
         *           },
         *           {
         *               "id": 2,
         *               "text": "Strategy Formulation"
         *           },
         *           {
         *               "id": 3,
         *               "text": "Balanced Scorecard Development"
         *           }
         *       ],
         *       "participants": [],
         *       "tribe": {
         *           "id": 1,
         *           "text": "Strategy & Execution Solutions"
         *       },
         *       "client": {
         *           "id": 141,
         *           "text": "3M INDONESIA, PT (MINNESOTA MINING MANUFACTURING)"
         *       },
         *       "year": 2019,
         *       "content": null,
         *       "errors": [
         *           {
         *               "code": "0",
         *               "description": ""
         *           }
         *       ]
         *   }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPut("projects/{id}")]
        public async Task<ActionResult<GetProjectResponse>> PutProject(int id, EditProjectRequest request)
        {
            if (id != request.Id)
            {
                return BadRequest();
            }

            var response = new GetProjectResponse();
            DateTime now = DateTime.Now;

            try
            {
                KmProject project = await _context.KmProjects.FindAsync(request.Id);
                if(project == null)
                {
                    return NotFound();
                }

                request.ClientId = request.ClientId == 0 ? project.ClientId : request.ClientId;

                if(request.TrainerIds != null)
                {
                    await UpdateInternalTeam("facilitator", request.TrainerIds, request.Id);
                }

                if(request.ProjectAdvisorId != 0)
                {
                    await UpdateInternalTeam("pa", new List<int>(new[] { request.ProjectAdvisorId }), request.Id);
                }

                if(request.ProjectLeaderId != 0)
                {
                    await UpdateInternalTeam("pl", new List<int>(new[] { request.ProjectLeaderId }), request.Id);
                }

                List<int> pcs = GetTeamsIdOnly("pc", project.Id, true);
                foreach (var pc in request.ProjectMemberIds)
                {
                    if(!pcs.Contains(pc) && pc != 0)
                    {
                        await AddInternalTeam("pc", new List<int>(new[] { pc }), project.Id);
                    }
                }
                foreach(var pc in pcs)
                {
                    if(!request.ProjectMemberIds.Contains(pc))
                    {
                        await RemoveInternalTeam("pc", pc, project.Id);
                    }
                }

                await UpdateExternalTeam("cpo", request.ClientProjectOwnerId, request.ClientId, request.Id);
                await UpdateExternalTeam("cpl", request.ClientProjectLeaderId, request.ClientId, request.Id);
                List<int> epcs = GetTeamsIdOnly("cpm", project.Id, false);
                foreach (var pm in request.ClientProjectMemberIds)
                {
                    if (!epcs.Contains(pm) && pm != 0)
                    {
                        await AddExternalTeam("cpm", pm, project.ClientId, project.Id);
                    }
                }
                foreach (var pm in epcs)
                {
                    if (!request.ClientProjectMemberIds.Contains(pm))
                    {
                        await RemoveExternalTeam("cpm", pm, project.Id);
                    }
                }

                List<int> products = GetProductIds(request.Id);
                foreach (int pid in request.ProductIds)
                {
                    if(!products.Contains(pid) && pid != 0)
                    {
                        await AddProjectProduct(pid, request.Id);
                    }
                }
                foreach(int cid in products)
                {
                    if(!request.ProductIds.Contains(cid))
                    {
                        await RemoveProjectProduct(cid, request.Id);
                    }
                }


                project.Name = request.Name;
                project.Status = request.Status;
                project.StartDate = request.StartDate;
                project.EndDate = request.EndDate;
                project.KeyWord = String.Join(",", request.KeyWords);
                project.ClientId = request.ClientId == 0 ? project.ClientId : request.ClientId;
                project.TribeId = request.TribeId;
                project.LastUpdated = now;
                project.LastUpdatedBy = request.UserId;
                if (request.WorkshopTypeId > 0)
                {
                    project.WorkshopTypeId = request.WorkshopTypeId;
                    project.Venue = request.Venue;
                }
                else
                {
                    project.WorkshopTypeId = 0;
                    project.Venue = "";
                }
                int year = project.StartDate.Year;

                KmYear projectYear = _context.KmYears.Where(a => a.Year == year && a.IsDeleted == false).FirstOrDefault();
                if (projectYear == null || projectYear.Id == 0)
                {
                    KmYear newYear = new KmYear()
                    {
                        Year = year,
                        CreatedDate = now,
                        CreatedBy = request.UserId,
                        LastUpdated = now,
                        LastUpdatedBy = request.UserId,
                        IsDeleted = false,
                        DeletedBy = 0
                    };
                    _context.KmYears.Add(newYear);
                    await _context.SaveChangesAsync();
                    projectYear = newYear;
                }
                project.YearId = projectYear.Id;

                _context.Entry(project).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                response.Project = project;
                response.Trainers = GetTeams("facilitator", project.Id, true);
                response.ProjectAdvisor = GetTeam("pa", project.Id, true);
                response.ProjectLeader = GetTeam("pl", project.Id, true);
                response.ProjectMembers = GetTeams("pc", project.Id, true);
                response.ClientProjectOwner = GetTeam("cpo", project.Id, false);
                response.ClientProjectLeader = GetTeam("cpl", project.Id, false);
                response.ClientProjectMembers = GetTeams("cpm", project.Id, false);
                response.Participants = GetTeams("participant", project.Id, false);
                response.Products = GetProducts(project.Id);
                response.Tribe = GetTribe(project.TribeId);
                response.Client = GetClient(project.ClientId);
                response.Year = projectYear.Year;
            }
            catch
            {
                return BadRequest(new Error("db_error", "Error updating database"));
            }
            return response;
        }

        /**
         * @api {get} /km/filter/{tribeFilter}/{clientFilter}/{yearFilter}/{page}/{perPage}/{search} GET filter project
         * @apiVersion 1.0.0
         * @apiName FilterProject
         * @apiGroup KM
         * @apiPermission ApiUser
         * 
         * @apiParam {String} tribeFilter           0 untuk tidak menggunakan filter, atau comma-separated values dari tribeId, misal 1,3.
         * @apiParam {String} clientFilter          0 untuk tidak menggunakan filter, atau comma-separated values dari clientId, misal 2,3.
         * @apiParam {String} yearFilter            0 untuk tidak menggunakan filter, atau comma-separated values dari tahun, misal 2018,2019.
         * @apiParam {Number} page                  Halaman yang ditampilkan.
         * @apiParam {Number} perPage               Jumlah data per halaman.
         * @apiParam {String} search                Tanda bintang (*) untuk tidak menggunakan search, atau kata yang mau d-search.
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "projects": [
         *           {
         *               "id": 1,
         *               "name": "Building Strategy-into-Performance Execution Excellence (SPEx2) with Balanced Scorecard",
         *               "status": "Closed",
         *               "venue": "",
         *               "startDate": "2019-05-14T00:00:00",
         *               "endDate": "2019-05-14T00:00:00",
         *               "keyWordStr": "Kepemimpinan,Manajemen",
         *               "keyWord": [
         *                   "Kepemimpinan",
         *                   "Manajemen"
         *               ],
         *               "workshopTypeId": 1,
         *               "year": 2019,
         *               "yearId": 1,
         *               "trainers": [],
         *               "projectAdvisor": {
         *                       "id": 1,
         *                       "text": "Rifky"
         *                   },
         *               "projectLeader": {
         *                       "id": 2,
         *                       "text": "Rafdi"
         *                   },
         *               "projectMembers": [],
         *               "clientProjectOwner": null,
         *               "clientProjectLeader": null,
         *               "clientProjectMembers": [],
         *               "products": [
         *                   {
         *                       "id": 2,
         *                       "text": "Strategy Formulation"
         *                   },
         *                   {
         *                       "id": 3,
         *                       "text": "Balanced Scorecard Development"
         *                   }
         *               ],
         *               "tribe": {
         *                   "id": 1,
         *                   "text": "Strategy & Execution Solutions"
         *               },
         *               "client": {
         *                   "id": 1738,
         *                   "text": "BPJS KETENAGAKERJAAN"
         *               },
         *               "clientId": 1738,
         *               "tribeId": 1
         *           },
         *           {
         *               "id": 2,
         *               "name": "BSC Cascading and Alignment to Division",
         *               "status": "Closed",
         *               "venue": "",
         *               "startDate": "2019-10-14T00:00:00",
         *               "endDate": "2019-10-15T00:00:00",
         *               "keyWord": "",
         *               "workshopTypeId": 1,
         *               "year": 2019,
         *               "yearId": 1,
         *               "trainers": [],
         *               "projectAdvisor": null,
         *               "projectLeader": null,
         *               "projectMembers": [],
         *               "clientProjectOwner": null,
         *               "clientProjectLeader": null,
         *               "clientProjectMembers": [],
         *               "products": [
         *                   {
         *                       "id": 3,
         *                       "text": "Balanced Scorecard Development"
         *                   }
         *               ],
         *               "tribe": {
         *                   "id": 1,
         *                   "text": "Strategy & Execution Solutions"
         *               },
         *               "client": {
         *                   "id": 841,
         *                   "text": "ASTRA LAND INDONESIA, PT"
         *               },
         *               "clientId": 841,
         *               "tribeId": 1
         *           }
         *       ],
         *       "info": {
         *           "page": 1,
         *           "perPage": 2,
         *           "total": 175
         *       }
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("filter/{tribeFilter}/{clientFilter}/{yearFilter}/{page}/{perPage}/{search}")]
        public async Task<ActionResult<FilterResponse>> FilterProject(string tribeFilter, string clientFilter, string yearFilter, int page, int perPage, string search)
        {
            FilterResponse response = new FilterResponse();

            List<int> tribeIds = SplitString(tribeFilter);
            List<int> clientIds = SplitString(clientFilter);
            List<int> years = SplitString(yearFilter);

            page = page <= 0 ? 1 : page;
            perPage = perPage <= 0 ? 5 : perPage;

            IQueryable<FilterProjectInfo> query;

            if (tribeIds.Count == 0)
            {
                if(clientIds.Count == 0)
                {
                    if(years.Count == 0)
                    {
                        if(search.Equals("*"))
                        {
                            query = from project in _context.KmProjects
                                    join tribe in _context.CoreTribes
                                    on project.TribeId equals tribe.Id
                                    join year in _context.KmYears
                                    on project.YearId equals year.Id
                                    join client in _context.CrmClients
                                    on project.ClientId equals client.Id
                                    where project.IsDeleted == false 
                                    select new FilterProjectInfo()
                                    {
                                        Id = project.Id,
                                        Name = project.Name,
                                        Status = project.Status,
                                        Venue = project.Venue,
                                        StartDate = project.StartDate,
                                        EndDate = project.EndDate,
                                        KeyWordStr = project.KeyWord,
                                        workshopTypeId = project.WorkshopTypeId,
                                        TribeId = project.TribeId,
                                        YearId = project.YearId,
                                        ClientId = project.ClientId
                                    };
                        }
                        else
                        {
                            query = from project in _context.KmProjects
                                    join tribe in _context.CoreTribes
                                    on project.TribeId equals tribe.Id
                                    join year in _context.KmYears
                                    on project.YearId equals year.Id
                                    join client in _context.CrmClients
                                    on project.ClientId equals client.Id
                                    where project.IsDeleted == false &&
                                    project.Name.Contains(search)
                                    select new FilterProjectInfo()
                                    {
                                        Id = project.Id,
                                        Name = project.Name,
                                        Status = project.Status,
                                        Venue = project.Venue,
                                        StartDate = project.StartDate,
                                        EndDate = project.EndDate,
                                        KeyWordStr = project.KeyWord,
                                        workshopTypeId = project.WorkshopTypeId,
                                        TribeId = project.TribeId,
                                        YearId = project.YearId,
                                        ClientId = project.ClientId
                                    };
                        }
                    }
                    else
                    {
                        if (search.Equals("*"))
                        {
                            query = from project in _context.KmProjects
                                    join tribe in _context.CoreTribes
                                    on project.TribeId equals tribe.Id
                                    join year in _context.KmYears
                                    on project.YearId equals year.Id
                                    join client in _context.CrmClients
                                    on project.ClientId equals client.Id
                                    where project.IsDeleted == false &&
                                    years.Contains(year.Year)
                                    select new FilterProjectInfo()
                                    {
                                        Id = project.Id,
                                        Name = project.Name,
                                        Status = project.Status,
                                        Venue = project.Venue,
                                        StartDate = project.StartDate,
                                        EndDate = project.EndDate,
                                        KeyWordStr = project.KeyWord,
                                        workshopTypeId = project.WorkshopTypeId,
                                        TribeId = project.TribeId,
                                        YearId = project.YearId,
                                        ClientId = project.ClientId
                                    };
                        }
                        else
                        {
                            query = from project in _context.KmProjects
                                    join tribe in _context.CoreTribes
                                    on project.TribeId equals tribe.Id
                                    join year in _context.KmYears
                                    on project.YearId equals year.Id
                                    join client in _context.CrmClients
                                    on project.ClientId equals client.Id
                                    where project.IsDeleted == false &&
                                    years.Contains(year.Year) &&
                                    project.Name.Contains(search)
                                    select new FilterProjectInfo()
                                    {
                                        Id = project.Id,
                                        Name = project.Name,
                                        Status = project.Status,
                                        Venue = project.Venue,
                                        StartDate = project.StartDate,
                                        EndDate = project.EndDate,
                                        KeyWordStr = project.KeyWord,
                                        workshopTypeId = project.WorkshopTypeId,
                                        TribeId = project.TribeId,
                                        YearId = project.YearId,
                                        ClientId = project.ClientId
                                    };
                        }
                    }
                }
                else
                {
                    if (years.Count == 0)
                    {
                        if (search.Equals("*"))
                        {
                            query = from project in _context.KmProjects
                                    join tribe in _context.CoreTribes
                                    on project.TribeId equals tribe.Id
                                    join year in _context.KmYears
                                    on project.YearId equals year.Id
                                    join client in _context.CrmClients
                                    on project.ClientId equals client.Id
                                    where project.IsDeleted == false &&
                                    clientIds.Contains(client.Id)
                                    select new FilterProjectInfo()
                                    {
                                        Id = project.Id,
                                        Name = project.Name,
                                        Status = project.Status,
                                        Venue = project.Venue,
                                        StartDate = project.StartDate,
                                        EndDate = project.EndDate,
                                        KeyWordStr = project.KeyWord,
                                        workshopTypeId = project.WorkshopTypeId,
                                        TribeId = project.TribeId,
                                        YearId = project.YearId,
                                        ClientId = project.ClientId
                                    };
                        }
                        else
                        {
                            query = from project in _context.KmProjects
                                    join tribe in _context.CoreTribes
                                    on project.TribeId equals tribe.Id
                                    join year in _context.KmYears
                                    on project.YearId equals year.Id
                                    join client in _context.CrmClients
                                    on project.ClientId equals client.Id
                                    where project.IsDeleted == false &&
                                    clientIds.Contains(client.Id) &&
                                    project.Name.Contains(search)
                                    select new FilterProjectInfo()
                                    {
                                        Id = project.Id,
                                        Name = project.Name,
                                        Status = project.Status,
                                        Venue = project.Venue,
                                        StartDate = project.StartDate,
                                        EndDate = project.EndDate,
                                        KeyWordStr = project.KeyWord,
                                        workshopTypeId = project.WorkshopTypeId,
                                        TribeId = project.TribeId,
                                        YearId = project.YearId,
                                        ClientId = project.ClientId
                                    };
                        }
                    }
                    else
                    {
                        if (search.Equals("*"))
                        {
                            query = from project in _context.KmProjects
                                    join tribe in _context.CoreTribes
                                    on project.TribeId equals tribe.Id
                                    join year in _context.KmYears
                                    on project.YearId equals year.Id
                                    join client in _context.CrmClients
                                    on project.ClientId equals client.Id
                                    where project.IsDeleted == false &&
                                    years.Contains(year.Year) &&
                                    clientIds.Contains(client.Id)
                                    select new FilterProjectInfo()
                                    {
                                        Id = project.Id,
                                        Name = project.Name,
                                        Status = project.Status,
                                        Venue = project.Venue,
                                        StartDate = project.StartDate,
                                        EndDate = project.EndDate,
                                        KeyWordStr = project.KeyWord,
                                        workshopTypeId = project.WorkshopTypeId,
                                        TribeId = project.TribeId,
                                        YearId = project.YearId,
                                        ClientId = project.ClientId
                                    };
                        }
                        else
                        {
                            query = from project in _context.KmProjects
                                    join tribe in _context.CoreTribes
                                    on project.TribeId equals tribe.Id
                                    join year in _context.KmYears
                                    on project.YearId equals year.Id
                                    join client in _context.CrmClients
                                    on project.ClientId equals client.Id
                                    where project.IsDeleted == false &&
                                    years.Contains(year.Year) &&
                                    clientIds.Contains(client.Id) &&
                                    project.Name.Contains(search)
                                    select new FilterProjectInfo()
                                    {
                                        Id = project.Id,
                                        Name = project.Name,
                                        Status = project.Status,
                                        Venue = project.Venue,
                                        StartDate = project.StartDate,
                                        EndDate = project.EndDate,
                                        KeyWordStr = project.KeyWord,
                                        workshopTypeId = project.WorkshopTypeId,
                                        TribeId = project.TribeId,
                                        YearId = project.YearId,
                                        ClientId = project.ClientId
                                    };
                        }
                    }
                }
            }
            else
            {
                if (clientIds.Count == 0)
                {
                    if (years.Count == 0)
                    {
                        if (search.Equals("*"))
                        {
                            query = from project in _context.KmProjects
                                    join tribe in _context.CoreTribes
                                    on project.TribeId equals tribe.Id
                                    join year in _context.KmYears
                                    on project.YearId equals year.Id
                                    join client in _context.CrmClients
                                    on project.ClientId equals client.Id
                                    where project.IsDeleted == false &&
                                    tribeIds.Contains(tribe.Id)
                                    select new FilterProjectInfo()
                                    {
                                        Id = project.Id,
                                        Name = project.Name,
                                        Status = project.Status,
                                        Venue = project.Venue,
                                        StartDate = project.StartDate,
                                        EndDate = project.EndDate,
                                        KeyWordStr = project.KeyWord,
                                        workshopTypeId = project.WorkshopTypeId,
                                        TribeId = project.TribeId,
                                        YearId = project.YearId,
                                        ClientId = project.ClientId
                                    };
                        }
                        else
                        {
                            query = from project in _context.KmProjects
                                    join tribe in _context.CoreTribes
                                    on project.TribeId equals tribe.Id
                                    join year in _context.KmYears
                                    on project.YearId equals year.Id
                                    join client in _context.CrmClients
                                    on project.ClientId equals client.Id
                                    where project.IsDeleted == false &&
                                    tribeIds.Contains(tribe.Id) &&
                                    project.Name.Contains(search)
                                    select new FilterProjectInfo()
                                    {
                                        Id = project.Id,
                                        Name = project.Name,
                                        Status = project.Status,
                                        Venue = project.Venue,
                                        StartDate = project.StartDate,
                                        EndDate = project.EndDate,
                                        KeyWordStr = project.KeyWord,
                                        workshopTypeId = project.WorkshopTypeId,
                                        TribeId = project.TribeId,
                                        YearId = project.YearId,
                                        ClientId = project.ClientId
                                    };
                        }
                    }
                    else
                    {
                        if (search.Equals("*"))
                        {
                            query = from project in _context.KmProjects
                                    join tribe in _context.CoreTribes
                                    on project.TribeId equals tribe.Id
                                    join year in _context.KmYears
                                    on project.YearId equals year.Id
                                    join client in _context.CrmClients
                                    on project.ClientId equals client.Id
                                    where project.IsDeleted == false &&
                                    years.Contains(year.Year) &&
                                    tribeIds.Contains(tribe.Id)
                                    select new FilterProjectInfo()
                                    {
                                        Id = project.Id,
                                        Name = project.Name,
                                        Status = project.Status,
                                        Venue = project.Venue,
                                        StartDate = project.StartDate,
                                        EndDate = project.EndDate,
                                        KeyWordStr = project.KeyWord,
                                        workshopTypeId = project.WorkshopTypeId,
                                        TribeId = project.TribeId,
                                        YearId = project.YearId,
                                        ClientId = project.ClientId
                                    };
                        }
                        else
                        {
                            query = from project in _context.KmProjects
                                    join tribe in _context.CoreTribes
                                    on project.TribeId equals tribe.Id
                                    join year in _context.KmYears
                                    on project.YearId equals year.Id
                                    join client in _context.CrmClients
                                    on project.ClientId equals client.Id
                                    where project.IsDeleted == false &&
                                    years.Contains(year.Year) &&
                                    tribeIds.Contains(tribe.Id) &&
                                    project.Name.Contains(search)
                                    select new FilterProjectInfo()
                                    {
                                        Id = project.Id,
                                        Name = project.Name,
                                        Status = project.Status,
                                        Venue = project.Venue,
                                        StartDate = project.StartDate,
                                        EndDate = project.EndDate,
                                        KeyWordStr = project.KeyWord,
                                        workshopTypeId = project.WorkshopTypeId,
                                        TribeId = project.TribeId,
                                        YearId = project.YearId,
                                        ClientId = project.ClientId
                                    };
                        }
                    }
                }
                else
                {
                    if (years.Count == 0)
                    {
                        if (search.Equals("*"))
                        {
                            query = from project in _context.KmProjects
                                    join tribe in _context.CoreTribes
                                    on project.TribeId equals tribe.Id
                                    join year in _context.KmYears
                                    on project.YearId equals year.Id
                                    join client in _context.CrmClients
                                    on project.ClientId equals client.Id
                                    where project.IsDeleted == false &&
                                    clientIds.Contains(client.Id) &&
                                    tribeIds.Contains(tribe.Id)
                                    select new FilterProjectInfo()
                                    {
                                        Id = project.Id,
                                        Name = project.Name,
                                        Status = project.Status,
                                        Venue = project.Venue,
                                        StartDate = project.StartDate,
                                        EndDate = project.EndDate,
                                        KeyWordStr = project.KeyWord,
                                        workshopTypeId = project.WorkshopTypeId,
                                        TribeId = project.TribeId,
                                        YearId = project.YearId,
                                        ClientId = project.ClientId
                                    };
                        }
                        else
                        {
                            query = from project in _context.KmProjects
                                    join tribe in _context.CoreTribes
                                    on project.TribeId equals tribe.Id
                                    join year in _context.KmYears
                                    on project.YearId equals year.Id
                                    join client in _context.CrmClients
                                    on project.ClientId equals client.Id
                                    where project.IsDeleted == false &&
                                    clientIds.Contains(client.Id) &&
                                    tribeIds.Contains(tribe.Id) &&
                                    project.Name.Contains(search)
                                    select new FilterProjectInfo()
                                    {
                                        Id = project.Id,
                                        Name = project.Name,
                                        Status = project.Status,
                                        Venue = project.Venue,
                                        StartDate = project.StartDate,
                                        EndDate = project.EndDate,
                                        KeyWordStr = project.KeyWord,
                                        workshopTypeId = project.WorkshopTypeId,
                                        TribeId = project.TribeId,
                                        YearId = project.YearId,
                                        ClientId = project.ClientId
                                    };
                        }
                    }
                    else
                    {
                        if (search.Equals("*"))
                        {
                            query = from project in _context.KmProjects
                                    join tribe in _context.CoreTribes
                                    on project.TribeId equals tribe.Id
                                    join year in _context.KmYears
                                    on project.YearId equals year.Id
                                    join client in _context.CrmClients
                                    on project.ClientId equals client.Id
                                    where project.IsDeleted == false &&
                                    years.Contains(year.Year) &&
                                    clientIds.Contains(client.Id) &&
                                    tribeIds.Contains(tribe.Id)
                                    select new FilterProjectInfo()
                                    {
                                        Id = project.Id,
                                        Name = project.Name,
                                        Status = project.Status,
                                        Venue = project.Venue,
                                        StartDate = project.StartDate,
                                        EndDate = project.EndDate,
                                        KeyWordStr = project.KeyWord,
                                        workshopTypeId = project.WorkshopTypeId,
                                        TribeId = project.TribeId,
                                        YearId = project.YearId,
                                        ClientId = project.ClientId
                                    };
                        }
                        else
                        {
                            query = from project in _context.KmProjects
                                    join tribe in _context.CoreTribes
                                    on project.TribeId equals tribe.Id
                                    join year in _context.KmYears
                                    on project.YearId equals year.Id
                                    join client in _context.CrmClients
                                    on project.ClientId equals client.Id
                                    where project.IsDeleted == false &&
                                    years.Contains(year.Year) &&
                                    clientIds.Contains(client.Id) &&
                                    tribeIds.Contains(tribe.Id) &&
                                    project.Name.Contains(search)
                                    select new FilterProjectInfo()
                                    {
                                        Id = project.Id,
                                        Name = project.Name,
                                        Status = project.Status,
                                        Venue = project.Venue,
                                        StartDate = project.StartDate,
                                        EndDate = project.EndDate,
                                        KeyWordStr = project.KeyWord,
                                        workshopTypeId = project.WorkshopTypeId,
                                        TribeId = project.TribeId,
                                        YearId = project.YearId,
                                        ClientId = project.ClientId
                                    };
                        }
                    }
                }
            }

            int total = query.Count();
            response.info = new PaginationInfo(page, perPage, total);

            response.projects = await query.Skip(perPage * (page - 1)).Take(perPage).ToListAsync<FilterProjectInfo>();
            foreach(FilterProjectInfo project in response.projects)
            {
                project.KeyWord = new List<string>(project.KeyWordStr.Split(","));
                project.Trainers = GetTeams("facilitator", project.Id, true);
                project.ProjectAdvisor = GetTeam("pa", project.Id, true);
                project.ProjectLeader = GetTeam("pl", project.Id, true);
                project.ProjectMembers = GetTeams("pc", project.Id, true);
                project.ClientProjectOwner = GetTeam("cpo", project.Id, false);
                project.ClientProjectLeader = GetTeam("cpl", project.Id, false);
                project.ClientProjectMembers = GetTeams("cpm", project.Id, false);
                project.Products = GetProducts(project.Id);
                project.Tribe = GetTribe(project.TribeId);
                project.Client = GetClient(project.ClientId);
                project.Year = GetYear(project.YearId);
            }

            return response;
        }

        /**
         * @api {get} /km/filtersort/{tribeId}/{clientId}/{year}/{sort} GET filter project with sort
         * @apiVersion 1.0.0
         * @apiName FilterProjectWithSort
         * @apiGroup KM
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} tribeId           id dari tribe.
         * @apiParam {Number} clientId          0 untuk semua klien, atau clientId.
         * @apiParam {Number} year              Tahun yang diinginkan, misal 2019. Kalau 0, maka default tahun ini.
         * @apiParam {Number} sort              0 untuk ascending, 1 descending. Default ascending.
         * 
         * @apiSuccessExample Success-Response:
         *   [
         *       {
         *           "id": 17,
         *           "name": "WORK EFFECIENCY THROUGH EFFECTIVE PRODUCTIVITY WITH 5R",
         *           "type": 1,
         *           "tribeId": 1,
         *           "tribe": "Strategy & Execution Solutions",
         *           "yearId": 1,
         *           "year": "2019",
         *           "clientId": 11488,
         *           "client": "SAKA ENERGI INDONESIA, PT",
         *           "status": "Closed",
         *           "clientOwner": ""
         *       },
         *       {
         *           "id": 7,
         *           "name": "VISION & VALUE DEVELOPMENT, STRATEGY ANALYSIS AND FORMULATION",
         *           "type": 1,
         *           "tribeId": 1,
         *           "tribe": "Strategy & Execution Solutions",
         *           "yearId": 1,
         *           "year": "2019",
         *           "clientId": 1169,
         *           "client": "BANK ARTHA GRAHA INTERNASIONAL Tbk, PT ",
         *           "status": "Closed",
         *           "clientOwner": ""
         *       }
         *   ]
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("filtersort/{tribeId}/{clientId}/{year}/{sort}")]
        public async Task<ActionResult<List<ProjectListInfo>>> FilterProjectWithSort(int tribeId, int clientId, int year, int sort)
        {
            year = year == 0 ? DateTime.Now.Year : year;
                
            IQueryable<ProjectListInfo> query;

            if(clientId == 0)
            {
                if (sort != 1)
                {
                    query = from project in _context.KmProjects
                            join tribe in _context.CoreTribes
                            on project.TribeId equals tribe.Id
                            join y in _context.KmYears
                            on project.YearId equals y.Id
                            join client in _context.CrmClients
                            on project.ClientId equals client.Id
                            where project.IsDeleted == false &&
                            y.Year == year &&
                            tribe.Id == tribeId
                            orderby project.Name
                            select new ProjectListInfo()
                            {
                                Id = project.Id,
                                Name = project.Name,
                                Type = 1,
                                YearId = y.Id,
                                Year = y.Year.ToString(),
                                TribeId = tribe.Id,
                                Tribe = tribe.Tribe,
                                ClientId = client.Id,
                                Client = client.Company,
                                Status = project.Status,
                                ClientOwner = "",
                                WorkshopTypeId = project.WorkshopTypeId
                            };

                }
                else
                {
                    query = from project in _context.KmProjects
                            join tribe in _context.CoreTribes
                            on project.TribeId equals tribe.Id
                            join y in _context.KmYears
                            on project.YearId equals y.Id
                            join client in _context.CrmClients
                            on project.ClientId equals client.Id
                            where project.IsDeleted == false &&
                            y.Year == year &&
                            tribe.Id == tribeId
                            orderby project.Name descending
                            select new ProjectListInfo()
                            {
                                Id = project.Id,
                                Name = project.Name,
                                Type = 1,
                                YearId = y.Id,
                                Year = y.Year.ToString(),
                                TribeId = tribe.Id,
                                Tribe = tribe.Tribe,
                                ClientId = client.Id,
                                Client = client.Company,
                                Status = project.Status,
                                ClientOwner = "",
                                WorkshopTypeId = project.WorkshopTypeId
                            };

                }


            }
            else
            {
                CoreTribe t = _context.CoreTribes.Find(tribeId);
                if(!t.Shortname.ToLower().Equals("retail"))
                {
                    if (sort != 1)
                    {
                        query = from project in _context.KmProjects
                                join tribe in _context.CoreTribes
                                on project.TribeId equals tribe.Id
                                join y in _context.KmYears
                                on project.YearId equals y.Id
                                join client in _context.CrmClients
                                on project.ClientId equals client.Id
                                where project.IsDeleted == false &&
                                y.Year == year &&
                                client.Id == clientId &&
                                tribe.Id == tribeId
                                orderby project.Name
                                select new ProjectListInfo()
                                {
                                    Id = project.Id,
                                    Name = project.Name,
                                    Type = 1,
                                    YearId = y.Id,
                                    Year = y.Year.ToString(),
                                    TribeId = tribe.Id,
                                    Tribe = tribe.Tribe,
                                    ClientId = client.Id,
                                    Client = client.Company,
                                    Status = project.Status,
                                    ClientOwner = "",
                                    WorkshopTypeId = project.WorkshopTypeId
                                };

                    }
                    else
                    {
                        query = from project in _context.KmProjects
                                join tribe in _context.CoreTribes
                                on project.TribeId equals tribe.Id
                                join y in _context.KmYears
                                on project.YearId equals y.Id
                                join client in _context.CrmClients
                                on project.ClientId equals client.Id
                                where project.IsDeleted == false &&
                                y.Year == year &&
                                client.Id == clientId &&
                                tribe.Id == tribeId
                                orderby project.Name descending
                                select new ProjectListInfo()
                                {
                                    Id = project.Id,
                                    Name = project.Name,
                                    Type = 1,
                                    YearId = y.Id,
                                    Year = y.Year.ToString(),
                                    TribeId = tribe.Id,
                                    Tribe = tribe.Tribe,
                                    ClientId = client.Id,
                                    Client = client.Company,
                                    Status = project.Status,
                                    ClientOwner = "",
                                    WorkshopTypeId = project.WorkshopTypeId
                                };

                    }

                }
                else
                {

                    // Untuk yang retail, semua digabung di satu client
                    if (sort != 1)
                    {
                        query = from project in _context.KmProjects
                                join tribe in _context.CoreTribes
                                on project.TribeId equals tribe.Id
                                join y in _context.KmYears
                                on project.YearId equals y.Id
                                join client in _context.CrmClients
                                on project.ClientId equals client.Id
                                where project.IsDeleted == false &&
                                y.Year == year &&
                                tribe.Id == tribeId
                                orderby project.Name
                                select new ProjectListInfo()
                                {
                                    Id = project.Id,
                                    Name = project.Name,
                                    Type = 1,
                                    YearId = y.Id,
                                    Year = y.Year.ToString(),
                                    TribeId = tribe.Id,
                                    Tribe = tribe.Tribe,
                                    ClientId = client.Id,
                                    Client = client.Company,
                                    Status = project.Status,
                                    ClientOwner = "",
                                    WorkshopTypeId = project.WorkshopTypeId
                                };

                    }
                    else
                    {
                        query = from project in _context.KmProjects
                                join tribe in _context.CoreTribes
                                on project.TribeId equals tribe.Id
                                join y in _context.KmYears
                                on project.YearId equals y.Id
                                join client in _context.CrmClients
                                on project.ClientId equals client.Id
                                where project.IsDeleted == false &&
                                y.Year == year &&
                                tribe.Id == tribeId
                                orderby project.Name descending
                                select new ProjectListInfo()
                                {
                                    Id = project.Id,
                                    Name = project.Name,
                                    Type = 1,
                                    YearId = y.Id,
                                    Year = y.Year.ToString(),
                                    TribeId = tribe.Id,
                                    Tribe = tribe.Tribe,
                                    ClientId = client.Id,
                                    Client = client.Company,
                                    Status = project.Status,
                                    ClientOwner = "",
                                    WorkshopTypeId = project.WorkshopTypeId
                                };

                    }

                }
            }

            List<ProjectListInfo> response = await query.ToListAsync();

            /*
            // No longer needed, karena yang retail sekarang digabung di 1 klien
            // Additional clients

            IQueryable<ProjectListInfo> query2;

            if (clientId == 0)
            {
                if (sort != 1)
                {
                    query2 = from project in _context.KmProjects
                            join tribe in _context.CoreTribes
                            on project.TribeId equals tribe.Id
                            join y in _context.KmYears
                            on project.YearId equals y.Id
                            join aclient in _context.KmProjectAdditionalClients
                            on project.Id equals aclient.ProjectId
                            join client in _context.CrmClients
                            on aclient.ClientId equals client.Id
                            where project.IsDeleted == false &&
                            y.Year == year &&
                            tribe.Id == tribeId
                            orderby project.Name
                            select new ProjectListInfo()
                            {
                                Id = project.Id,
                                Name = project.Name,
                                Type = 1,
                                YearId = y.Id,
                                Year = y.Year.ToString(),
                                TribeId = tribe.Id,
                                Tribe = tribe.Tribe,
                                ClientId = client.Id,
                                Client = client.Company,
                                Status = project.Status,
                                ClientOwner = "",
                                WorkshopTypeId = project.WorkshopTypeId
                            };

                }
                else
                {
                    query2 = from project in _context.KmProjects
                            join tribe in _context.CoreTribes
                            on project.TribeId equals tribe.Id
                            join y in _context.KmYears
                            on project.YearId equals y.Id
                            join aclient in _context.KmProjectAdditionalClients
                            on project.Id equals aclient.ProjectId
                            join client in _context.CrmClients
                            on aclient.ClientId equals client.Id
                            where project.IsDeleted == false &&
                            y.Year == year &&
                            tribe.Id == tribeId
                            orderby project.Name descending
                            select new ProjectListInfo()
                            {
                                Id = project.Id,
                                Name = project.Name,
                                Type = 1,
                                YearId = y.Id,
                                Year = y.Year.ToString(),
                                TribeId = tribe.Id,
                                Tribe = tribe.Tribe,
                                ClientId = client.Id,
                                Client = client.Company,
                                Status = project.Status,
                                ClientOwner = "",
                                WorkshopTypeId = project.WorkshopTypeId
                            };

                }


            }
            else
            {
                if (sort != 1)
                {
                    query2 = from project in _context.KmProjects
                            join tribe in _context.CoreTribes
                            on project.TribeId equals tribe.Id
                            join y in _context.KmYears
                            on project.YearId equals y.Id
                            join aclient in _context.KmProjectAdditionalClients
                            on project.Id equals aclient.ProjectId
                            join client in _context.CrmClients
                            on aclient.ClientId equals client.Id
                            where project.IsDeleted == false &&
                            y.Year == year &&
                            client.Id == clientId &&
                            tribe.Id == tribeId
                            orderby project.Name
                            select new ProjectListInfo()
                            {
                                Id = project.Id,
                                Name = project.Name,
                                Type = 1,
                                YearId = y.Id,
                                Year = y.Year.ToString(),
                                TribeId = tribe.Id,
                                Tribe = tribe.Tribe,
                                ClientId = client.Id,
                                Client = client.Company,
                                Status = project.Status,
                                ClientOwner = "",
                                WorkshopTypeId = project.WorkshopTypeId
                            };

                }
                else
                {
                    query2 = from project in _context.KmProjects
                            join tribe in _context.CoreTribes
                            on project.TribeId equals tribe.Id
                            join y in _context.KmYears
                            on project.YearId equals y.Id
                            join aclient in _context.KmProjectAdditionalClients
                            on project.Id equals aclient.ProjectId
                            join client in _context.CrmClients
                            on aclient.ClientId equals client.Id
                            where project.IsDeleted == false &&
                            y.Year == year &&
                            client.Id == clientId &&
                            tribe.Id == tribeId
                            orderby project.Name descending
                            select new ProjectListInfo()
                            {
                                Id = project.Id,
                                Name = project.Name,
                                Type = 1,
                                YearId = y.Id,
                                Year = y.Year.ToString(),
                                TribeId = tribe.Id,
                                Tribe = tribe.Tribe,
                                ClientId = client.Id,
                                Client = client.Company,
                                Status = project.Status,
                                ClientOwner = "",
                                WorkshopTypeId = project.WorkshopTypeId
                            };

                }
            }

            response.AddRange(await query2.ToListAsync());
            */
            return response;
        }

        [Authorize(Policy = "ApiUser")]
        [HttpPost("prepare")]
        public async Task<ActionResult<Error>> PrepareView(PrepareViewRequest request)
        {
            string strs = "";
            string delimiter = ",";

            DateTime now = DateTime.Now;

            foreach (int fileId in request.FileIds)
            {
                if (FileExists(fileId))
                {
                    KmFile file = _context.KmFiles.Find(fileId);

                    await _activityLogService.AddLog("view", request.UserId, fileId, now);

                    var r = new Random();
                    int rn = r.Next(10000000, 99999999);

                    KmPrepareView view = new KmPrepareView();

                    view.UserId = request.UserId;
                    view.FileId = fileId;
                    view.RandomId = rn;
                    view.Expired = now.AddHours(1);
                    view.Fullpath = file.Fullpath;

                    _context.KmPrepareViews.Add(view);
                    await _context.SaveChangesAsync();

                    strs += strs.Equals("") ? rn.ToString() : delimiter + rn.ToString();
                }
                else
                {
                    return new Error("id", "Incorrect file id.");
                }

            }

            return new Error("ok", strs);
        }

        /**
         * @api {get} /km/tribes GET tribes
         * @apiVersion 1.0.0
         * @apiName GetTribes
         * @apiGroup KM
         * @apiPermission ApiUser
         * 
         * @apiSuccessExample Success-Response:
         *   [
         *       {
         *           "id": 1,
         *           "text": "Strategy & Execution Solutions"
         *       },
         *       {
         *           "id": 2,
         *           "text": "Organization Excellence Solutions"
         *       }
         *   ]
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("tribes")]
        public async Task<ActionResult<List<GenericInfo>>> GetTribes()
        {
            var query = from tribe in _context.CoreTribes
                        where tribe.IsDeleted == false
                        select new GenericInfo()
                        {
                            Id = tribe.Id,
                            Text = tribe.Tribe
                        };
            return await query.ToListAsync<GenericInfo>();
        }

        /**
         * @api {get} /km/tribes GET platforms
         * @apiVersion 1.0.0
         * @apiName platforms
         * @apiGroup KM
         * @apiPermission ApiUser
         * 
         * @apiSuccessExample Success-Response:
         *   [
         *       {
         *           "id": 1,
         *           "text": "Finance"
         *       },
         *       {
         *           "id": 2,
         *           "text": "Sales"
         *       },
         *       {
         *           "id": 3,
         *           "text": "Internal HR"
         *       },
         *       {
         *           "id": 4,
         *           "text": "Knowledge Management and Digitalization"
         *       }
         *   ]
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("platforms")]
        public async Task<ActionResult<List<GenericInfo>>> platforms()
        {
            var query = from platform in _context.CorePlatforms
                        where platform.IsDeleted == false
                        select new GenericInfo()
                        {
                            Id = platform.Id,
                            Text = platform.Platform
                        };
            return await query.ToListAsync<GenericInfo>();
        }

        /**
         * @api {get} /km/products GET products
         * @apiVersion 1.0.0
         * @apiName GetProducs
         * @apiGroup KM
         * @apiPermission ApiUser
         * 
         * @apiSuccessExample Success-Response:
         *   [
         *       {
         *           "id": 1,
         *           "text": "Vision, Mission & Destination Statement"
         *       },
         *       {
         *           "id": 2,
         *           "text": "Strategy Formulation"
         *       }
         *   ]
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("products")]
        public async Task<ActionResult<List<GenericInfo>>> GetProducs()
        {
            var query = from product in _context.KmProducts
                        where product.IsDeleted == false
                        select new GenericInfo()
                        {
                            Id = product.Id,
                            Text = product.Product
                        };
            return await query.ToListAsync<GenericInfo>();
        }

        /**
         * @api {get} /km/quick/{userId}/{onegml}/{tribeId}/{clientId}/{yearId}/{limit} GET quick
         * @apiVersion 1.0.0
         * @apiName GetQuickAccess
         * @apiGroup KM
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} userid            id dari user yang login. 
         * @apiParam {Number} onegml            0 untuk file project, 1 untuk file onegml.
         * @apiParam {Number} tribeId           id dari tribe tertentu, atau 0.
         * @apiParam {Number} clientId          id dari client tertentu, atau 0.
         * @apiParam {Number} yearId            id dari tahun tertentu, atau 0.
         * @apiParam {Number} limit             Jumlah record yang mau diambil. 
         * 
         * @apiSuccessExample Success-Response:
         *   [
         *       {
         *           "fileId": 5,
         *           "name": "Leviana Wijaya",
         *           "action": "view",
         *           "filename": "01. Cover Materi Workshop SPEx2 BPJS TK 2019_05_14.pdf",
         *           "fileType": "pdf",
         *           "tribe": "Strategy & Execution Solutions",
         *           "client": "BPJS KETENAGAKERJAAN",
         *           "year": "2019",
         *           "project": "Building Strategy-into-Performance Execution Excellence (SPEx2)",
         *           "onegml": false,
         *           "location": "location": "BPJS KETENAGAKERJAAN / 2019 / Building Strategy-into-Performance Exexution"
         *           "lastAccess": "2020-04-01T12:01:40.2236619"
         *       },
         *       {
         *           "fileId": 6,
         *           "name": "Leviana Wijaya",
         *           "action": "view",
         *           "filename": "02. Pembatas Materi Workshop SPEx2 BPJS TK 2019_05_14 - Kertas Biru.pdf",
         *           "fileType": "pdf",
         *           "tribe": "Strategy & Execution Solutions",
         *           "client": "BPJS KETENAGAKERJAAN",
         *           "year": "2019",
         *           "project": "Building Strategy-into-Performance Execution Excellence (SPEx2) with Balanced Scorecard",
         *           "onegml": false,
         *           "location": "location": "BPJS KETENAGAKERJAAN / 2019 / Building Strategy-into-Performance Exexution"
         *           "lastAccess": "2020-04-01T12:01:40.2236619"
         *       }
         *   ]
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("quick/{userId}/{onegml}/{tribeId}/{clientId}/{yearId}/{limit}")]
        public async Task<ActionResult<List<FileAccessInfo>>> GetQuickAccess(int userId, int onegml, int tribeId, int clientId, int yearId, int limit)
        {
            IQueryable<FileAccessInfo> query;
            if(onegml != 0)
            {
                query = from log in _context.KmActivityLogs
                        join user in _context.Users
                        on log.UserId equals user.ID
                        join file in _context.KmFiles
                        on log.FileId equals file.Id
                        where log.UserId == userId && file.Onegml
                        orderby log.CreatedDate descending
                        select new FileAccessInfo()
                        {
                            FileId = file.Id,
                            Name = user.FirstName,
                            Action = log.Action,
                            Filename = file.Name,
                            FileType = file.FileType,
                            Tribe = "",
                            Client = "",
                            Year = "",
                            Project = "",
                            Onegml = file.Onegml,
                            LastAccess = log.CreatedDate
                        };
            }
            else if (tribeId <= 0)
            {
                query = from log in _context.KmActivityLogs
                        join user in _context.Users
                        on log.UserId equals user.ID
                        join file in _context.KmFiles
                        on log.FileId equals file.Id
                        join project in _context.KmProjects
                        on file.ProjectId equals project.Id
                        join client in _context.CrmClients
                        on project.ClientId equals client.Id
                        join year in _context.KmYears
                        on project.YearId equals year.Id
                        join tribe in _context.CoreTribes
                        on project.TribeId equals tribe.Id
                        where log.UserId == userId && !file.Onegml
                        orderby log.CreatedDate descending
                        select new FileAccessInfo()
                        {
                            FileId = file.Id,
                            Name = user.FirstName,
                            Action = log.Action,
                            Filename = file.Name,
                            FileType = file.FileType,
                            Tribe = tribe.Tribe,
                            Client = client.Company,
                            Year = year.Year.ToString(),
                            Project = project.Name,
                            Onegml = file.Onegml,
                            LastAccess = log.CreatedDate
                        };
            }
            else
            {
                if (clientId <= 0)
                {
                    query = from log in _context.KmActivityLogs
                            join user in _context.Users
                            on log.UserId equals user.ID
                            join file in _context.KmFiles
                            on log.FileId equals file.Id
                            join project in _context.KmProjects
                            on file.ProjectId equals project.Id
                            join client in _context.CrmClients
                            on project.ClientId equals client.Id
                            join year in _context.KmYears
                            on project.YearId equals year.Id
                            join tribe in _context.CoreTribes
                            on project.TribeId equals tribe.Id
                            where log.UserId == userId && tribe.Id == tribeId && !file.Onegml
                            orderby log.CreatedDate descending
                            select new FileAccessInfo()
                            {
                                FileId = file.Id,
                                Name = user.FirstName,
                                Action = log.Action,
                                Filename = file.Name,
                                FileType = file.FileType,
                                Tribe = tribe.Tribe,
                                Client = client.Company,
                                Year = year.Year.ToString(),
                                Project = project.Name,
                                Onegml = file.Onegml,
                                LastAccess = log.CreatedDate
                            };
                }
                else
                {
                    if (yearId <= 0)
                    {
                        query = from log in _context.KmActivityLogs
                                join user in _context.Users
                                on log.UserId equals user.ID
                                join file in _context.KmFiles
                                on log.FileId equals file.Id
                                join project in _context.KmProjects
                                on file.ProjectId equals project.Id
                                join client in _context.CrmClients
                                on project.ClientId equals client.Id
                                join year in _context.KmYears
                                on project.YearId equals year.Id
                                join tribe in _context.CoreTribes
                                on project.TribeId equals tribe.Id
                                where log.UserId == userId && tribe.Id == tribeId && client.Id == clientId && !file.Onegml
                                orderby log.CreatedDate descending
                                select new FileAccessInfo()
                                {
                                    FileId = file.Id,
                                    Name = user.FirstName,
                                    Action = log.Action,
                                    Filename = file.Name,
                                    FileType = file.FileType,
                                    Tribe = tribe.Tribe,
                                    Client = client.Company,
                                    Year = year.Year.ToString(),
                                    Project = project.Name,
                                    Onegml = file.Onegml,
                                    LastAccess = log.CreatedDate
                                };
                    }
                    else
                    {
                        query = from log in _context.KmActivityLogs
                                join user in _context.Users
                                on log.UserId equals user.ID
                                join file in _context.KmFiles
                                on log.FileId equals file.Id
                                join project in _context.KmProjects
                                on file.ProjectId equals project.Id
                                join client in _context.CrmClients
                                on project.ClientId equals client.Id
                                join year in _context.KmYears
                                on project.YearId equals year.Id
                                join tribe in _context.CoreTribes
                                on project.TribeId equals tribe.Id
                                where log.UserId == userId && tribe.Id == tribeId && client.Id == clientId && year.Id == yearId && !file.Onegml
                                orderby log.CreatedDate descending
                                select new FileAccessInfo()
                                {
                                    FileId = file.Id,
                                    Name = user.FirstName,
                                    Action = log.Action,
                                    Filename = file.Name,
                                    FileType = file.FileType,
                                    Tribe = tribe.Tribe,
                                    Client = client.Company,
                                    Year = year.Year.ToString(),
                                    Project = project.Name,
                                    Onegml = file.Onegml,
                                    LastAccess = log.CreatedDate
                                };
                    }
                }
            }
            List<FileAccessInfo> response = query.Take(limit).ToList<FileAccessInfo>();
            foreach(FileAccessInfo info in response)
            {
                List<string> pre = new List<string>();
                if(onegml != 0)
                {
                    pre.Add("OneGML");

                }
                else
                {
                    pre.AddRange(new[] { info.Client, info.Year.ToString(), info.Project });
                }
                info.Location = GetFileLocation(info.FileId, pre);

            }
            return response;

        }

        // GET: v1/last/10
        /**
         * @api {get} /km/last/{onegml}/{limit} GET last
         * @apiVersion 1.0.0
         * @apiName GetLastActivities
         * @apiGroup KM
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} onegml            0 untuk file project, 1 untuk file onegml.
         * @apiParam {Number} limit             Jumlah record yang mau diambil. 
         * 
         * @apiSuccessExample Success-Response:
         *   [
         *       {
         *           "fileId": 5,
         *           "name": "Leviana Wijaya",
         *           "action": "view",
         *           "filename": "01. Cover Materi Workshop SPEx2 BPJS TK 2019_05_14.pdf",
         *           "fileType": "pdf",
         *           "tribe": "Strategy & Execution Solutions",
         *           "client": "BPJS KETENAGAKERJAAN",
         *           "year": "2019",
         *           "project": "Building Strategy-into-Performance Execution Excellence (SPEx2)",
         *           "onegml": false,
         *           "lastAccess": "2020-04-01T12:01:40.2236619"
         *       },
         *       {
         *           "fileId": 6,
         *           "name": "Leviana Wijaya",
         *           "action": "view",
         *           "filename": "02. Pembatas Materi Workshop SPEx2 BPJS TK 2019_05_14 - Kertas Biru.pdf",
         *           "fileType": "pdf",
         *           "tribe": "Strategy & Execution Solutions",
         *           "client": "BPJS KETENAGAKERJAAN",
         *           "year": "2019",
         *           "project": "Building Strategy-into-Performance Execution Excellence (SPEx2) with Balanced Scorecard",
         *           "onegml": false,
         *           "lastAccess": "2020-04-01T12:01:40.2236619"
         *       }
         *   ]
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("last/{onegml}/{limit}")]
        public async Task<ActionResult<List<FileAccessInfo>>> GetLastActivities(int onegml, int limit)
        {
            var queryLast = from log in _context.KmActivityLogs
                            join user in _context.Users
                            on log.UserId equals user.ID
                            join file in _context.KmFiles
                            on log.FileId equals file.Id
                            join project in _context.KmProjects
                            on file.ProjectId equals project.Id
                            join client in _context.CrmClients
                            on project.ClientId equals client.Id
                            join year in _context.KmYears
                            on project.YearId equals year.Id
                            join tribe in _context.CoreTribes
                            on project.TribeId equals tribe.Id
                            where file.Onegml == (onegml == 1)
                            orderby log.CreatedDate descending
                            select new FileAccessInfo()
                            {
                                FileId = file.Id,
                                Name = user.FirstName,
                                Action = log.Action,
                                Filename = file.Name,
                                FileType = file.FileType,
                                Tribe = tribe.Tribe,
                                Client = client.Company,
                                Year = year.Year.ToString(),
                                Project = project.Name,
                                Onegml = file.Onegml,
                                LastAccess = log.CreatedDate
                            };
            List<FileAccessInfo> response = await queryLast.Take(limit).ToListAsync<FileAccessInfo>();
            return response;
        }

        /**
         * @api {post} /km/folder POST folder
         * @apiVersion 1.0.0
         * @apiName UpdateFolder
         * @apiGroup KM
         * @apiPermission ApiUser
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "id": 0,
         *     "name": "Nama folder",
         *     "description": "folder untuk proyek yang sedang dikerjakan",
         *     "parentId": 0,
         *     "projectId": 1,
         *     "onegml": 0,
         *     "OwnerId": 0,
         *     "userId": 2
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   {
         *       "id": 10445,
         *       "parentId": 0,
         *       "name": "Nama folder",
         *       "filename": "",
         *       "fileType": "",
         *       "isFolder": true,
         *       "description": "folder untuk proyek yang sedang dikerjakan".
         *       "projectId": 1,
         *       "onegml": false,
         *       "ownerId": 0,
         *       "createdDate": "2020-09-19T13:13:42.3056799+07:00",
         *       "createdBy": 2,
         *       "lastUpdated": "2020-09-19T13:13:42.3056799+07:00",
         *       "lastUpdatedBy": 2,
         *       "isDeleted": false,
         *       "deletedBy": 0,
         *       "deletedDate": null
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost("folder")]
        public async Task<ActionResult<KmFile>> UpdateFolder(FolderInfo request)
        {
            var now = DateTime.Now;

            if(request.Onegml == 0 && request.ProjectId == 0)
            {
                return BadRequest(new { error = "OneGML and projectId cannot be both 0." });
            }

            if(request.Id == 0)
            {
                try
                {
                    KmFile file = new KmFile()
                    {
                        ParentId = request.ParentId,
                        Name = request.Name,
                        Filename = "",
                        FileType = "",
                        IsFolder = true,
                        RootFolder = _options.DataRootDirectory,
                        Description = request.Description == null ? "" : request.Description,
                        ProjectId = request.ProjectId,
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
                    _context.KmFiles.Add(file);
                    await _context.SaveChangesAsync();

                    return file;
                }
                catch
                {
                    return BadRequest(new { error = "Error in creating folder" });
                }
            }
            else
            {
                try
                {
                    if (FileExists(request.Id))
                    {
                        KmFile kmFile = _context.KmFiles.Where(a => a.Id == request.Id).FirstOrDefault();
                        kmFile.ParentId = request.ParentId;
                        kmFile.Name = request.Name;
                        kmFile.Filename = "";
                        kmFile.FileType = "";
                        kmFile.IsFolder = true;
                        kmFile.Description = request.Description == null ? kmFile.Description : request.Description;
                        kmFile.ProjectId = request.ProjectId;
                        kmFile.Onegml = request.Onegml == 1;
                        kmFile.OwnerId = request.OwnerId;
                        kmFile.LastUpdated = now;
                        kmFile.LastUpdatedBy = request.UserId;
                        kmFile.IsDeleted = false;
                        _context.Entry(kmFile).State = EntityState.Modified;
                        await _context.SaveChangesAsync();

                        return kmFile;
                    }
                    else
                    {
                        return BadRequest(new { error = "Error in updating folder" });
                    }
                }
                catch
                {
                    return BadRequest(new { error = "Error in updating folder" });
                }
            }
        }

        /**
         * @api {put} /km/folder/{id} PUT folder
         * @apiVersion 1.0.0
         * @apiName PutFolder
         * @apiGroup KM
         * @apiPermission ApiUser
         * @apiParam {Number} id        Id dari folder yang bersangkutan, sama dengan id di request
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "id": 407,
         *     "name": "Nama folder",
         *     "description": "folder untuk proyek yang sedang dikerjakan",
         *     "parentId": 0,
         *     "projectId": 1,
         *     "onegml": 0,
         *     "OwnerId": 0,
         *     "userId": 2
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   {
         *       "id": 407,
         *       "parentId": 0,
         *       "name": "Nama folder",
         *       "filename": "",
         *       "fileType": "",
         *       "isFolder": true,
         *       "description": "folder untuk proyek yang sedang dikerjakan",
         *       "projectId": 1,
         *       "onegml": false,
         *       "ownerId": 0,
         *       "createdDate": "2020-09-19T13:13:42.3056799+07:00",
         *       "createdBy": 2,
         *       "lastUpdated": "2020-09-19T13:13:42.3056799+07:00",
         *       "lastUpdatedBy": 2,
         *       "isDeleted": false,
         *       "deletedBy": 0,
         *       "deletedDate": null
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPut("folder/{id}")]
        public async Task<ActionResult<KmFile>> PutFolder(int id, FolderInfo request)
        {
            if (id != request.Id)
            {
                return BadRequest();
            }

            var now = DateTime.Now;

            if (FileExists(request.Id))
            {
                try
                {
                    KmFile kmFile = _context.KmFiles.Where(a => a.Id == request.Id).FirstOrDefault();
                    kmFile.ParentId = request.ParentId;
                    kmFile.Name = request.Name;
                    kmFile.Filename = "";
                    kmFile.FileType = "";
                    kmFile.IsFolder = true;
                    kmFile.Description = request.Description == null ? kmFile.Description : request.Description;
                    kmFile.ProjectId = request.ProjectId;
                    kmFile.Onegml = request.Onegml == 1;
                    kmFile.OwnerId = request.OwnerId;
                    kmFile.LastUpdated = now;
                    kmFile.LastUpdatedBy = request.UserId;
                    kmFile.IsDeleted = false;
                    _context.Entry(kmFile).State = EntityState.Modified;
                    await _context.SaveChangesAsync();

                    return kmFile;
                }
                catch
                {
                    return BadRequest(new { error = "Error in updating folder" });
                }
            }

            return NotFound();
        }

        /**
         * @api {get} /km/parents/{folderId} GET parent folders
         * @apiVersion 1.0.0
         * @apiName GetParentFolders
         * @apiGroup KM
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} folderId          id dari folder yang mau didapat parent-nya.  
         * 
         * @apiSuccessExample Success-Response:
         *   [
         *       {
         *           "id": 0,
         *           "text": "Root"
         *       },
         *       {
         *           "id": 60,
         *           "text": "Name Table"
         *       }
         *   ]
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("parents/{folderId}")]
        public async Task<ActionResult<List<GenericInfo>>> GetParentFolders(int folderId)
        {
            List<GenericInfo> pre = new List<GenericInfo>();
            pre.Add(new GenericInfo()
            {
                Id = 0,
                Text = "Root"
            });
            List<GenericInfo> list = GetBreadcrump(folderId, pre);

            if(list == null)
            {
                return NotFound(new { error = "Folder does not exist. Please check folderId." });
            }
            return list;
        }


        /**
         * @api {delete} /km/insight/delete/{insightId}/{userId} DELETE insight
         * @apiVersion 1.0.0
         * @apiName DeleteInsight
         * @apiGroup KM
         * @apiPermission ApiUser
         * 
         * @apiSuccessExample Success-Response:
         *   No content
         */
        [Authorize(Policy = "ApiUser")]
        [HttpDelete("insight/delete/{insightId}/{userId}")]
        public async Task<ActionResult<KmInsight>> DeleteInsight(int insightId, int userId)
        {
            KmInsight insight = _context.KmInsights.Find(insightId);

            if (insight == null) return NotFound();

            DateTime now = DateTime.Now;

            insight.IsDeleted = true;
            insight.DeletedBy = userId;
            insight.DeletedDate = now;
            _context.Entry(insight).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /**
         * @api {get} /km/insight/slug/{slug} Get insight by slug
         * @apiVersion 1.0.0
         * @apiName GetInsightBySlug
         * @apiGroup KM
         * @apiPermission Basic Auth
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "id": 2,
         *       "userId": 35,
         *       "title": "Altius Solusi Saat Ini",
         *       "content": "Altius adalah solusi terbaik untuk manajemen kinerja",
         *       "authors": [
         *           {
         *               "id": 1,
         *               "text": "Rifky"
         *           },
         *           {
         *               "id": 2,
         *               "text": "Rafdi"
         *           }
         *       ],
         *       "categories": [
         *           {
         *               "id": 1,
         *               "text": "Strategy"
         *           },
         *           {
         *               "id": 1,
         *               "text": "Strategy"
         *           }
         *       ],
         *       "filename": "test.png",
         *       "fileBase64": "",
         *       "metaTitle": "title Altius",
         *       "metaDescription": "Description Altius",
         *       "slug": "Slug Altius",
         *       "keyWords": [
         *           "manajemen",
         *           "kinerja"
         *       ],
         *       "thumbnail": "https://www.onegml.com/assettest//insight/1/hlkhbbhg.xwr.png",
         *       "lastUpdated": "2020-12-16T12:03:20.5164177"
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         * 
         */
        [AllowAnonymous]
        [HttpGet("insight/slug/{slug}")]
        public async Task<ActionResult<DetailInsight>> GetInsightBySlug(string slug)
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

            KmInsight insight = _context.KmInsights.Where(a => !a.IsDeleted && a.Slug.Equals(slug)).FirstOrDefault();
            if (insight == null) return NotFound();

            return await GetInsightById(insight.Id);
        }


        /**
         * @api {get} /km/insight/{insightId} Get insight by Id
         * @apiVersion 1.0.0
         * @apiName GetInsightById
         * @apiGroup KM
         * @apiPermission ApiUser
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "id": 2,
         *       "userId": 35,
         *       "title": "Altius Solusi Saat Ini",
         *       "content": "Altius adalah solusi terbaik untuk manajemen kinerja",
         *       "authors": [
         *           {
         *               "id": 1,
         *               "text": "Rifky"
         *           },
         *           {
         *               "id": 2,
         *               "text": "Rafdi"
         *           }
         *       ],
         *       "categories": [
         *           {
         *               "id": 1,
         *               "text": "Strategy"
         *           },
         *           {
         *               "id": 1,
         *               "text": "Strategy"
         *           }
         *       ],
         *       "filename": "test.png",
         *       "fileBase64": "",
         *       "metaTitle": "title Altius",
         *       "metaDescription": "Description Altius",
         *       "slug": "Slug Altius",
         *       "keyWords": [
         *           "manajemen",
         *           "kinerja"
         *       ],
         *       "thumbnail": "https://www.onegml.com/assettest//insight/1/hlkhbbhg.xwr.png",
         *       "lastUpdated": "2020-12-16T12:03:20.5164177"
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         * 
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("insight/{insightId}")]
        public async Task<ActionResult<DetailInsight>> GetInsightById(int insightId)
        {
            KmInsight insight = _context.KmInsights.Where(a => a.Id == insightId && !a.IsDeleted).FirstOrDefault();
            if (insight == null) return NotFound();

            string baseurl = GetInsightURL(insight.Id);

            GenericWebsite gw = new GenericWebsite();
            gw.label = insight.Website.ToUpper();
            gw.value = insight.Website;

            DetailInsight response = new DetailInsight()
            {
                Id = insight.Id,
                Website = gw,
                UserId = insight.LastUpdatedBy,
                Title = insight.Title,
                Content = insight.Content,
                Filename = insight.OriginalFilename,
                FileBase64 = "",
                MetaTitle = insight.MetaTitle,
                MetaDescription = insight.MetaDescription,
                Slug = insight.Slug,
                KeyWords = new List<string>(insight.KeyWord.Split(",")),
                Thumbnail = string.Join(@"/", new[] { baseurl, insight.Filename }),
                LastUpdated = insight.LastUpdated
            };

            var q1 = from ia in _context.KmInsightAuthors
                     join u in _context.Users on ia.UserId equals u.ID
                     where ia.InsightId == insight.Id
                     select new GenericInfo()
                     {
                         Id = u.ID,
                         Text = u.FirstName
                     };

            var q2 = from ic in _context.KmInsightCategories
                     join c in _context.WebTopicCategories on ic.CategoryId equals c.Id
                     where ic.InsightId == insight.Id
                     select new GenericInfo()
                     {
                         Id = c.Id,
                         Text = c.Category
                     };

            response.Authors = await q1.ToListAsync();
            response.Categories = await q2.ToListAsync();

            return response;
        }

        /**
         * @api {get} /km/insight/{authorIds}/{categoryIds}/{publish}/{sort}/{page}/{perPage}/{search} Get insight
         * @apiVersion 1.0.0
         * @apiName GetInsight
         * @apiGroup KM
         * @apiPermission ApiUser
         * 
         * @apiParam {String} authorIds       0 untuk semua, atau comma-separated values dari authorId
         * @apiParam {String} categoryIds     0 untuk semua, atau comma-separated values dari categoryId
         * @apiParam {Number} publish         0 untuk draft, 1 untuk publish
         * @apiParam {Number} sort            0 untuk dari yang terbaru, 1 untuk dari yang terlama
         * @apiParam {Number} page            Halaman yang ditampilkan. 
         * @apiParam {Number} perPage         Jumlah data per halaman.  
         * @apiParam {String} search          Kata yang mau dicari di judul. * untuk semua
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "items": [
         *           {
         *               "id": 1,
         *               "title": "Altius Solusi Saat Ini",
         *               "authors": [
         *                   {
         *                       "id": 1,
         *                       "text": "Rifky"
         *                   },
         *                   {
         *                       "id": 2,
         *                       "text": "Rafdi"
         *                   },
         *                   {
         *                       "id": 1,
         *                       "text": "Rifky"
         *                   },
         *                   {
         *                       "id": 2,
         *                       "text": "Rafdi"
         *                   }
         *               ],
         *               "categories": [
         *                   {
         *                       "id": 1,
         *                       "text": "Strategy"
         *                   },
         *                   {
         *                       "id": 1,
         *                       "text": "Strategy"
         *                   }
         *               ],
         *               "lastUpdate": "2020-12-16T10:04:18.1507304",
         *               "publish": false
         *           },
         *           {
         *               "id": 2,
         *               "title": "Altius Solusi Saat Ini",
         *               "authors": [
         *                   {
         *                       "id": 1,
         *                       "text": "Rifky"
         *                   },
         *                   {
         *                       "id": 2,
         *                       "text": "Rafdi"
         *                   }
         *               ],
         *               "categories": [
         *                   {
         *                       "id": 1,
         *                       "text": "Strategy"
         *                   },
         *                   {
         *                       "id": 1,
         *                       "text": "Strategy"
         *                   }
         *               ],
         *               "lastUpdate": "2020-12-16T10:06:28.4224422",
         *               "publish": false
         *           }
         *       ],
         *       "info": {
         *           "page": 1,
         *           "perPage": 10,
         *           "total": 2
         *       }
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         * 
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("insight/{authorIds}/{categoryIds}/{publish}/{sort}/{page}/{perPage}/{search}")]
        public async Task<ActionResult<InsightList>> GetInsight(string authorIds, string categoryIds, int publish, int sort, int page, int perPage, string search)
        {

            List<int> aids = SplitString(authorIds);
            List<int> cids = SplitString(categoryIds);
            
            if(aids == null || cids == null)
            {
                return BadRequest(new { error = "Error in parsing authorIds or categoryIds." });
            }

            bool p = publish == 1;

            Func<KmInsightAuthor, bool> AuthorPredicate = ia => {
                bool cat = authorIds.Trim().Equals("0") ? true : aids.Contains(ia.UserId);

                return cat;
            };

            Func<KmInsightCategory, bool> CategoryPredicate = ic => {
                bool cat = categoryIds.Trim().Equals("0") ? true : cids.Contains(ic.CategoryId);

                return cat;
            };


            IQueryable<KmInsight> query;
            if(sort == 1)
            {
                // dari yang terlama
                if(search.Trim().Equals("*"))
                {
                    query = from a in _context.KmInsights
                            join ia in _context.KmInsightAuthors on a.Id equals ia.InsightId
                            join ic in _context.KmInsightCategories on a.Id equals ic.InsightId
                            where !a.IsDeleted && a.Publish == p && AuthorPredicate(ia) && CategoryPredicate(ic)
                            orderby a.LastUpdated
                            select a;
                }
                else
                {
                    query = from a in _context.KmInsights
                            join ia in _context.KmInsightAuthors on a.Id equals ia.InsightId
                            join ic in _context.KmInsightCategories on a.Id equals ic.InsightId
                            where !a.IsDeleted && a.Publish == p && a.Title.Contains(search) && AuthorPredicate(ia) && CategoryPredicate(ic)
                            orderby a.LastUpdated
                            select a;

                }

            }
            else
            {
                if(search.Trim().Equals("*"))
                {
                    query = from a in _context.KmInsights
                            join ia in _context.KmInsightAuthors on a.Id equals ia.InsightId
                            join ic in _context.KmInsightCategories on a.Id equals ic.InsightId
                            where !a.IsDeleted && a.Publish == p && AuthorPredicate(ia) && CategoryPredicate(ic)
                            orderby a.LastUpdated descending
                            select a;
                }
                else
                {
                    query = from a in _context.KmInsights
                            join ia in _context.KmInsightAuthors on a.Id equals ia.InsightId
                            join ic in _context.KmInsightCategories on a.Id equals ic.InsightId
                            where !a.IsDeleted && a.Publish == p && a.Title.Contains(search) && AuthorPredicate(ia) && CategoryPredicate(ic)
                            orderby a.LastUpdated descending
                            select a;
                }
            }
            InsightList response = new InsightList();
            response.items = new List<InsightListItem>();
            response.Info = new PaginationInfo(page, perPage, query.Distinct().Count());

            response.TotalAllInsightsPublished = GetTotalInsight("");
            response.TotalAllInsightsPublishedGML = GetTotalInsight("gml");
            response.TotalAllInsightsPublishedCDHX = GetTotalInsight("cdhx");

            List<KmInsight> insights = await query.Skip(perPage * (page - 1)).Take(perPage).Distinct().ToListAsync<KmInsight>();

            foreach(KmInsight insight in insights)
            {
                var q1 = from ia in _context.KmInsightAuthors
                         join u in _context.Users on ia.UserId equals u.ID
                         where ia.InsightId == insight.Id
                         select new GenericInfo()
                         {
                             Id = u.ID,
                             Text = u.FirstName
                         };

                var q2 = from ic in _context.KmInsightCategories
                         join c in _context.WebTopicCategories on ic.CategoryId equals c.Id
                         where ic.InsightId == insight.Id
                         select new GenericInfo()
                         {
                             Id = c.Id,
                             Text = c.Category
                         };

                InsightListItem item = new InsightListItem()
                {
                    Id = insight.Id,
                    Title = insight.Title,
                    Authors = await q1.ToListAsync(),
                    Categories = await q2.ToListAsync(),
                    LastUpdate = insight.LastUpdated,
                    Publish = insight.Publish,
                    Website = insight.Website.ToUpper()
                };
                response.items.Add(item);
            }

            return response;
        }

        /**
         * @api {get} /km/insight/publish/{id}/{publish}/{userId} Publish insight  
         * @apiVersion 1.0.0
         * @apiName PublishInsight
         * @apiGroup KM
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} id            Id dari insight yang ingin di-publish
         * @apiParam {Number} publish       1 untuk publish, 0 untuk tidak mem-publish         
         * @apiParam {Number} userId        userId dari user yang login
         * @apiSuccessExample Success-Response:        
         * {
         *     "id": 1,
         *     "title": "Altius Solusi Saat Ini",
         *     "content": "Altius adalah solusi terbaik untuk manajemen kinerja",
         *     "keyWord": "manajemen,kinerja",
         *     "slug": "Slug Altius",
         *     "metaTitle": "title Altius",
         *     "metaDescription": "Description Altius",
         *     "originalFilename": "test.png",
         *     "filename": "m2zukqyz.bzz.png",
         *     "filetype": "png",
         *     "publish": true,
         *     "createdDate": "2020-12-16T10:04:18.1507304",
         *     "createdBy": 35,
         *     "lastUpdated": "2020-12-16T10:33:01.5789124+07:00",
         *     "lastUpdatedBy": 35,
         *     "isDeleted": false,
         *     "deletedBy": 0,
         *     "deletedDate": "1970-01-01T00:00:00"
         * }
         * 
         * @apiError NotFound    id salah
         * 
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("insight/publish/{id}/{publish}/{userId}")]
        public async Task<ActionResult<KmInsight>> PublishInsight(int id, int publish, int userId)
        {
            var insight = await _context.KmInsights.FindAsync(id);
            if (insight == null)
            {
                return NotFound();
            }
            DateTime now = DateTime.Now;
            insight.LastUpdated = now;
            insight.LastUpdatedBy = userId;
            insight.Publish = publish == 1;

            _context.Entry(insight).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return insight;
        }

        /**
         * @api {get} /km/insight/intro/{count} Get insight intro
         * @apiVersion 1.0.0
         * @apiName GetInsightIntro
         * @apiGroup KM
         * @apiPermission Basic auth
         * 
         * @apiParam {Number} count         Jumlah yang mau di-get, misal 4.
         * 
         * @apiSuccessExample Success-Response:        
         *   [
         *       {
         *           "id": 1,
         *           "title": "Altius Solusi Saat Ini",
         *           "extract": "Altius adalah solusi terbaik untuk manajemen kinerja",
         *           "categories": [
         *               {
         *                   "id": 1,
         *                   "text": "Strategy"
         *               }
         *           ],
         *           "authors": [
         *               {
         *                   "id": 1,
         *                   "text": "Rifky"
         *               },
         *               {
         *                   "id": 2,
         *                   "text": "Rafdi"
         *               }
         *           ],
         *           "thumbnail": "http://localhost/assets//insight/1/m2zukqyz.bzz.png"
         *       },
         *       {
         *           "id": 2,
         *           "title": "Altius Solusi Saat Ini",
         *           "extract": "Altius adalah solusi terbaik untuk manajemen kinerja",
         *           "categories": [
         *               {
         *                   "id": 1,
         *                   "text": "Strategy"
         *               }
         *           ],
         *           "authors": [
         *               {
         *                   "id": 1,
         *                   "text": "Rifky"
         *               },
         *               {
         *                   "id": 2,
         *                   "text": "Rafdi"
         *               }
         *           ],
         *           "thumbnail": "http://localhost/assets//insight/2/jp1xxyux.f4y.png"
         *       }
         *   ]
         * 
         */
        [AllowAnonymous]
        [HttpGet("insight/intro/{count}")]
        public async Task<ActionResult<List<InsightIntro>>> GetInsightIntro(int count)
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

            List<InsightIntro> response = new List<InsightIntro>();

            var query = from i in _context.KmInsights
                        where !i.IsDeleted
                        orderby i.LastUpdated descending
                        select i;
            List<KmInsight> insights = await query.Take(count).ToListAsync();

            foreach(KmInsight insight in insights)
            {
                string baseurl = GetInsightURL(insight.Id);

                int l = insight.Content.Length > 430 ? 430 : insight.Content.Length;

                InsightIntro intro = new InsightIntro()
                {
                    Id = insight.Id,
                    Title = insight.Title,
                    Extract = insight.Content.Substring(0, l),
                    Thumbnail = string.Join(@"/",  new[] { baseurl, insight.Filename }),
                    Slug = insight.Slug
                };

                var q1 = from ia in _context.KmInsightAuthors
                         join u in _context.Users on ia.UserId equals u.ID
                         where ia.InsightId == insight.Id
                         select new GenericInfo()
                         {
                             Id = u.ID,
                             Text = u.FirstName
                         };

                var q2 = from ic in _context.KmInsightCategories
                         join c in _context.WebTopicCategories on ic.CategoryId equals c.Id
                         where ic.InsightId == insight.Id
                         select new GenericInfo()
                         {
                             Id = c.Id,
                             Text = c.Category
                         };

                intro.Authors = await q1.ToListAsync();
                intro.Categories = await q2.ToListAsync();

                response.Add(intro);
            }
            return response;
        }

        /**
         * @api {get} /km/insight/intro/{count}/{publish} Get insight published intro 
         * @apiVersion 1.0.0
         * @apiName GetInsightPublishedIntro
         * @apiGroup KM
         * @apiPermission Basic auth
         * 
         * @apiParam {Number} count         Jumlah yang mau di-get, misal 4.
         * @apiParam {Number} publish       1 untuk publish, 0 untuk draft
         * 
         * @apiSuccessExample Success-Response:        
         *   [
         *       {
         *           "id": 1,
         *           "title": "Altius Solusi Saat Ini",
         *           "extract": "Altius adalah solusi terbaik untuk manajemen kinerja",
         *           "categories": [
         *               {
         *                   "id": 1,
         *                   "text": "Strategy"
         *               }
         *           ],
         *           "authors": [
         *               {
         *                   "id": 1,
         *                   "text": "Rifky"
         *               },
         *               {
         *                   "id": 2,
         *                   "text": "Rafdi"
         *               }
         *           ],
         *           "thumbnail": "http://localhost/assets//insight/1/m2zukqyz.bzz.png"
         *       },
         *       {
         *           "id": 2,
         *           "title": "Altius Solusi Saat Ini",
         *           "extract": "Altius adalah solusi terbaik untuk manajemen kinerja",
         *           "categories": [
         *               {
         *                   "id": 1,
         *                   "text": "Strategy"
         *               }
         *           ],
         *           "authors": [
         *               {
         *                   "id": 1,
         *                   "text": "Rifky"
         *               },
         *               {
         *                   "id": 2,
         *                   "text": "Rafdi"
         *               }
         *           ],
         *           "thumbnail": "http://localhost/assets//insight/2/jp1xxyux.f4y.png"
         *       }
         *   ]
         * 
         */
        [AllowAnonymous]
        [HttpGet("insight/intro/{count}/{publish}")]
        public async Task<ActionResult<List<InsightIntro>>> GetInsightPublishedIntro(int count, int publish)
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

            bool p = publish == 1;

            List<InsightIntro> response = new List<InsightIntro>();

            var query = from i in _context.KmInsights
                        where !i.IsDeleted && i.Publish == p && i.Website == "gml" || !i.IsDeleted && i.Publish == p && i.Website == "both"
                        orderby i.LastUpdated descending
                        select i;
            List<KmInsight> insights = await query.Take(count).ToListAsync();

            foreach (KmInsight insight in insights)
            {
                string baseurl = GetInsightURL(insight.Id);

                int l = insight.Content.Length > 430 ? 430 : insight.Content.Length;

                InsightIntro intro = new InsightIntro()
                {
                    Id = insight.Id,
                    Title = insight.Title,
                    Extract = insight.Content.Substring(0, l),
                    Thumbnail = string.Join(@"/", new[] { baseurl, insight.Filename }),
                    Slug = insight.Slug
                };

                var q1 = from ia in _context.KmInsightAuthors
                         join u in _context.Users on ia.UserId equals u.ID
                         where ia.InsightId == insight.Id
                         select new GenericInfo()
                         {
                             Id = u.ID,
                             Text = u.FirstName
                         };

                var q2 = from ic in _context.KmInsightCategories
                         join c in _context.WebTopicCategories on ic.CategoryId equals c.Id
                         where ic.InsightId == insight.Id
                         select new GenericInfo()
                         {
                             Id = c.Id,
                             Text = c.Category
                         };

                intro.Authors = await q1.ToListAsync();
                intro.Categories = await q2.ToListAsync();

                response.Add(intro);
            }
            return response;
        }

        [AllowAnonymous]
        [HttpGet("insight/introcdhx/{count}/{publish}")]
        public async Task<ActionResult<List<InsightIntro>>> GetInsightPublishedIntroCDHX(int count, int publish)
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

            bool p = publish == 1;

            List<InsightIntro> response = new List<InsightIntro>();

            var query = from i in _context.KmInsights
                        where !i.IsDeleted && i.Publish == p && i.Website == "cdhx" || !i.IsDeleted && i.Publish == p && i.Website == "both"
                        orderby i.LastUpdated descending
                        select i;
            List<KmInsight> insights = await query.Take(count).ToListAsync();

            foreach (KmInsight insight in insights)
            {
                string baseurl = GetInsightURL(insight.Id);

                int l = insight.Content.Length > 430 ? 430 : insight.Content.Length;

                InsightIntro intro = new InsightIntro()
                {
                    Id = insight.Id,
                    Title = insight.Title,
                    Extract = insight.Content.Substring(0, l),
                    Thumbnail = string.Join(@"/", new[] { baseurl, insight.Filename }),
                    Slug = insight.Slug
                };

                var q1 = from ia in _context.KmInsightAuthors
                         join u in _context.Users on ia.UserId equals u.ID
                         where ia.InsightId == insight.Id
                         select new GenericInfo()
                         {
                             Id = u.ID,
                             Text = u.FirstName
                         };

                var q2 = from ic in _context.KmInsightCategories
                         join c in _context.WebTopicCategories on ic.CategoryId equals c.Id
                         where ic.InsightId == insight.Id
                         select new GenericInfo()
                         {
                             Id = c.Id,
                             Text = c.Category
                         };

                intro.Authors = await q1.ToListAsync();
                intro.Categories = await q2.ToListAsync();

                response.Add(intro);
            }
            return response;
        }

        /**
         * @api {post} /km/insight POST insight
         * @apiVersion 1.0.0
         * @apiName PostInsight
         * @apiGroup KM
         * @apiPermission ApiUser
         * 
         * @apiParamExample {json} Request-Example:
         * {
         *   "id": 0,
         *   "userId": 35,
         *   "title": "Altius Solusi Saat Ini",
         *   "content": "Altius adalah solusi terbaik untuk manajemen kinerja",
         *   "authorIds": [
         *     1,2
         *   ],
         *   "categoryIds": [
         *     1
         *   ],
         *   "filename": "test.png",
         *   "fileBase64": "data:image/png;base64, ....",
         *   "metaTitle": "title Altius",
         *   "metaDescription": "Description Altius",
         *   "slug": "Slug Altius",
         *   "keyWords": [
         *     "manajemen", "kinerja"
         *   ],
         *   "publish": 1
         * }
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "id": 2,
         *       "userId": 35,
         *       "title": "Altius Solusi Saat Ini",
         *       "content": "Altius adalah solusi terbaik untuk manajemen kinerja",
         *       "authors": [
         *           {
         *               "id": 1,
         *               "text": "Rifky"
         *           },
         *           {
         *               "id": 2,
         *               "text": "Rafdi"
         *           }
         *       ],
         *       "categories": [
         *           {
         *               "id": 1,
         *               "text": "Strategy"
         *           },
         *           {
         *               "id": 1,
         *               "text": "Strategy"
         *           }
         *       ],
         *       "filename": "test.png",
         *       "fileBase64": "",
         *       "metaTitle": "title Altius",
         *       "metaDescription": "Description Altius",
         *       "slug": "Slug Altius",
         *       "keyWords": [
         *           "manajemen",
         *           "kinerja"
         *       ],
         *       "thumbnail": "https://www.onegml.com/assettest/insight/2/hlkhbbhg.xwr.png",
         *       "lastUpdated": "2020-12-16T12:03:20.5164177"
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         * 
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost("insight")]
        public async Task<ActionResult<DetailInsight>> PostInsight(PostInsight request)
        {
            DateTime now = DateTime.Now;

            if (!IsInsightSlugUnique(request.Slug, 0)) return BadRequest(new { error = "Slug must be unique." });
              
            KmInsight insight = new KmInsight()
            {
                Title = request.Title,
                Content = request.Content,
                KeyWord = string.Join(",", request.KeyWords),
                Slug = request.Slug,
                MetaTitle = request.MetaTitle,
                MetaDescription = request.MetaDescription,
                OriginalFilename = "",
                Filename = "",
                Filetype = "",
                Publish = request.Publish == 1,
                CreatedDate = now,
                CreatedBy = request.UserId,
                LastUpdated = now,
                LastUpdatedBy = request.UserId,
                IsDeleted = false,
                DeletedBy = 0,
                DeletedDate = new DateTime(1970, 1, 1),
                Website = request.Website
            };
            _context.KmInsights.Add(insight);

            try
            {
                await _context.SaveChangesAsync();

                if(request.FileBase64 != null)
                {
                    var error = SaveImage(request.FileBase64, insight.Id, request.Filename);
                    if (error.Code.Equals("ok"))
                    {
                        string[] names = error.Description.Split(separator);
                        if (names.Length >= 3)
                        {
                            insight.Filename = names[1];
                            insight.OriginalFilename = names[0];
                            insight.Filetype = names[2];
                            _context.Entry(insight).State = EntityState.Modified;
                            await _context.SaveChangesAsync();
                        }
                    }
                    else
                    {
                        return BadRequest(new { error = "Error saving file" });
                    }
                }

                foreach (int aid in request.AuthorIds)
                {
                    KmInsightAuthor ia = new KmInsightAuthor()
                    {
                        InsightId = insight.Id,
                        UserId = aid
                    };
                    _context.KmInsightAuthors.Add(ia);
                }

                foreach(int cid in request.CategoryIds)
                {
                    KmInsightCategory ic = new KmInsightCategory()
                    {
                        InsightId = insight.Id,
                        CategoryId = cid
                    };
                    _context.KmInsightCategories.Add(ic);
                }

                await _context.SaveChangesAsync();

            }
            catch
            {
                return BadRequest(new { error = "Error saving to database." });

            }

            return await GetInsightById(insight.Id);
        }

        /**
         * @api {post} /km/insight PUT insight
         * @apiVersion 1.0.0
         * @apiName PutInsight
         * @apiGroup KM
         * @apiPermission ApiUser
         * 
         * @apiParamExample {json} Request-Example:
         * {
         *   "id": 1,
         *   "userId": 35,
         *   "title": "Altius Solusi Saat Ini",
         *   "content": "Altius adalah solusi terbaik untuk manajemen kinerja",
         *   "authorIds": [
         *     1,2
         *   ],
         *   "categoryIds": [
         *     1
         *   ],
         *   "filename": "test.png",
         *   "fileBase64": "data:image/png;base64, ....",
         *   "metaTitle": "title Altius",
         *   "metaDescription": "Description Altius",
         *   "slug": "Slug Altius",
         *   "keyWords": [
         *     "manajemen", "kinerja"
         *   ],
         *   "publish": 1
         * }
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "id": 1,
         *       "userId": 35,
         *       "title": "Altius Solusi Saat Ini",
         *       "content": "Altius adalah solusi terbaik untuk manajemen kinerja",
         *       "authorIds": [
         *           1,
         *           2
         *       ],
         *       "categoryIds": [
         *           1
         *       ],
         *       "filename": "test.png",
         *       "fileBase64": "",
         *       "metaTitle": "title Altius",
         *       "metaDescription": "Description Altius",
         *       "slug": "Slug Altius",
         *       "keyWords": [
         *           "manajemen",
         *           "kinerja"
         *       ],
         *       "thumbnail": "https://www.onegml.com/assettest//insight/1/hlkhbbhg.xwr.png",
         *       "lastUpdated": "2020-12-16T12:03:20.5164177"
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         * 
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPut("insight")]
        public async Task<ActionResult<DetailInsight>> PutInsight(PostInsight request)
        {
            KmInsight insight = _context.KmInsights.Where(a => a.Id == request.Id && !a.IsDeleted).FirstOrDefault();
            if (insight == null) return NotFound();

            if (!IsInsightSlugUnique(request.Slug, request.Id)) return BadRequest(new { error = "Slug must be unique." });

            DateTime now = DateTime.Now;

            insight.Title = request.Title;
            insight.Content = request.Content;
            insight.KeyWord = string.Join(",", request.KeyWords);
            insight.Publish = request.Publish == 1;
            insight.Slug = request.Slug;
            insight.MetaTitle = request.MetaTitle;
            insight.MetaDescription = request.MetaDescription;
            insight.LastUpdated = now;
            insight.LastUpdatedBy = request.UserId;
            insight.Website = request.Website;

            _context.Entry(insight).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();

                if (request.FileBase64 != null)
                {
                    var error = SaveImage(request.FileBase64, insight.Id, request.Filename);
                    if (error.Code.Equals("ok"))
                    {
                        string[] names = error.Description.Split(separator);
                        if (names.Length >= 3)
                        {
                            insight.Filename = names[1];
                            insight.OriginalFilename = names[0];
                            insight.Filetype = names[2];
                            _context.Entry(insight).State = EntityState.Modified;
                            await _context.SaveChangesAsync();
                        }
                    }
                    else
                    {
                        return BadRequest(new { error = "Error saving file" });
                    }
                }

                List<KmInsightAuthor> caids = await _context.KmInsightAuthors.Where(a => a.InsightId == request.Id).ToListAsync();
                if(caids != null)
                {
                    foreach (KmInsightAuthor ia in caids)
                    {
                        _context.KmInsightAuthors.Remove(ia);
                    }
                }

                List<KmInsightCategory> ccids = await _context.KmInsightCategories.Where(a => a.InsightId == request.Id).ToListAsync();
                if (ccids != null)
                {
                    foreach (KmInsightCategory ic in ccids)
                    {
                        _context.KmInsightCategories.Remove(ic);
                    }
                }

                foreach (int aid in request.AuthorIds)
                {
                    KmInsightAuthor ia = new KmInsightAuthor()
                    {
                        InsightId = insight.Id,
                        UserId = aid
                    };
                    _context.KmInsightAuthors.Add(ia);
                }

                foreach (int cid in request.CategoryIds)
                {
                    KmInsightCategory ic = new KmInsightCategory()
                    {
                        InsightId = insight.Id,
                        CategoryId = cid
                    };
                    _context.KmInsightCategories.Add(ic);
                }

                await _context.SaveChangesAsync();

            }
            catch
            {
                return BadRequest(new { error = "Error saving to database." });

            }

            return await GetInsightById(insight.Id);
        }

        private Error SaveImage(string base64String, int insightId, string name)
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
            if (n != 0)
            {
                try
                {
                    base64String = base64String.Substring(n);

                    string randomName = Path.GetRandomFileName() + "." + fileExt;
                    string fileDir;
                    if (insightId == 0)
                    {
                        fileDir = GetWebDirectory();
                    }
                    else
                    {
                        fileDir = GetInsightDirectory(insightId);
                    }
                    if(_fileService.CheckAndCreateDirectory(Path.Combine(_options.AssetsRootDirectory, @"insight")))
                    {
                        if (_fileService.CheckAndCreateDirectory(fileDir))
                        {
                            var fileName = Path.Combine(fileDir, randomName);
                            _fileService.SaveByteArrayAsImage(fileName, base64String, format);
                            return new Error("ok", string.Join(separator, new[] { name, randomName, fileExt }));
                        }
                        else
                        {
                            return new Error("error", "Error in saving file.");
                        }

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

        private string GetWebDirectory()
        {
            return Path.Combine(_options.AssetsRootDirectory, @"web");
        }

        private string GetInsightDirectory(int insightId)
        {
            return Path.Combine(_options.AssetsRootDirectory, @"insight", insightId.ToString());
        }

        private string GetInsightURL(int insightId)
        {
            return string.Join(@"/", new[] { _options.AssetsBaseURL, @"insight", insightId.ToString() });
        }

        private List<GenericInfo> GetBreadcrump(int folderId, List<GenericInfo> pre)
        {
            List<GenericInfo> list = new List<GenericInfo>();

            while (folderId != 0)
            {
                KmFile file1 = _context.KmFiles.Where(a => a.Id == folderId && a.IsFolder && !a.IsDeleted).FirstOrDefault();
                if (file1 != null)
                {
                    list.Add(new GenericInfo()
                    {
                        Id = file1.Id,
                        Text = file1.Name
                    });
                    folderId = file1.ParentId;
                }
                else
                {
                    return null;
                }

            }

            pre.Reverse();
            foreach(GenericInfo info in pre)
            {
                list.Add(info);
            }

            list.Reverse();

            return list;
        }

        /**
         * @api {delete} /km/deletefolderfile/{id} DELETE folder/file
         * @apiVersion 1.0.0
         * @apiName DeleteFolderOrFile
         * @apiGroup KM
         * @apiPermission ApiUser
         * @apiDescription Ketika men-delete folder, semua file isinya akan ter-delete juga.
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "id": 60,
         *       "parentId": 0,
         *       "name": "Name Table",
         *       "filename": "",
         *       "fileType": "",
         *       "isFolder": true,
         *       "projectId": 3,
         *       "onegml": false,
         *       "createdDate": "2020-03-30T17:02:53.7777214",
         *       "createdBy": 31,
         *       "lastUpdated": "2020-03-30T17:02:53.7777214",
         *       "lastUpdatedBy": 31,
         *       "isDeleted": true,
         *       "deletedBy": 0,
         *       "deletedDate": null
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpDelete("deletefolderfile/{id}")]
        public async Task<ActionResult<KmFile>> DeleteFolderOrFile(int id)
        {
            KmFile kmFile = await _context.KmFiles.FindAsync(id);
            if(kmFile == null)
            {
                return NotFound();
            }

            if(kmFile.IsFolder)
            {
                List<KmFile> files = await _context.KmFiles.Where(a => a.ParentId == kmFile.Id && a.ProjectId == kmFile.ProjectId && !a.IsDeleted).ToListAsync<KmFile>();
                foreach(KmFile file in files)
                {
                    file.IsDeleted = true;
                    _context.Entry(file).State = EntityState.Modified;
                }
            }

            kmFile.IsDeleted = true;
            _context.Entry(kmFile).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return kmFile;
        }

        private IQueryable<GenericInfo> GetQuery(string roleshortname, int projectId, bool internalTeam)
        {
            IQueryable<GenericInfo> query;
            if (internalTeam)
            {
                query = from team in _context.KmProjectInternalTeams
                        join role in _context.KmProjectTeamRoles
                        on team.RoleId equals role.Id
                        join user in _context.Users
                        on team.UserId equals user.ID
                        where team.ProjectId == projectId && role.Shortname == roleshortname && role.IsDeleted == false
                        select new GenericInfo()
                        {
                            Id = user.ID,
                            Text = user.FirstName
                        };
            }
            else
            {
                query = from team in _context.KmProjectExternalTeams
                        join role in _context.KmProjectTeamRoles
                        on team.RoleId equals role.Id
                        join contact in _context.CrmContacts
                        on team.ContactId equals contact.Id
                        where team.ProjectId == projectId && role.Shortname == roleshortname && role.IsDeleted == false
                        select new GenericInfo()
                        {
                            Id = contact.Id,
                            Text = contact.Name
                        };
            }
            return query;
        }
        private IQueryable<int> GetQueryIdOnly(string roleshortname, int projectId, bool internalTeam)
        {
            IQueryable<int> query;
            if (internalTeam)
            {
                query = from team in _context.KmProjectInternalTeams
                        join role in _context.KmProjectTeamRoles
                        on team.RoleId equals role.Id
                        join user in _context.Users
                        on team.UserId equals user.ID
                        where team.ProjectId == projectId && role.Shortname == roleshortname && role.IsDeleted == false
                        select user.ID;
            }
            else
            {
                query = from team in _context.KmProjectExternalTeams
                        join role in _context.KmProjectTeamRoles
                        on team.RoleId equals role.Id
                        join contact in _context.CrmContacts
                        on team.ContactId equals contact.Id
                        where team.ProjectId == projectId && role.Shortname == roleshortname && role.IsDeleted == false
                        select contact.Id;
            }
            return query;
        }
        private List<GenericInfo> GetProducts(int projectId)
        {
            var query = from pp in _context.KmProjectProducts
                        join product in _context.KmProducts
                        on pp.ProductId equals product.Id
                        where pp.ProjectId == projectId && product.IsDeleted == false
                        select new GenericInfo()
                        {
                            Id = product.Id,
                            Text = product.Product
                        };
            return query.ToList<GenericInfo>();
        }

        private List<int> GetProductIds(int projectId)
        {
            var query = from pp in _context.KmProjectProducts
                        join product in _context.KmProducts
                        on pp.ProductId equals product.Id
                        where pp.ProjectId == projectId && product.IsDeleted == false
                        select product.Id;
            return query.ToList<int>();
        }
        private GenericInfo GetTeam(string roleshortname, int projectId, bool internalTeam)
        {
            var query = GetQuery(roleshortname, projectId, internalTeam);
            return query.FirstOrDefault();
        }
        private List<int> GetTeamsIdOnly(string roleshortname, int projectId, bool internalTeam)
        {
            IQueryable<int> query = GetQueryIdOnly(roleshortname, projectId, internalTeam);
            return query.ToList<int>();
        }
        private List<GenericInfo> GetTeams(string roleshortname, int projectId, bool internalTeam)
        {
            IQueryable<GenericInfo> query = GetQuery(roleshortname, projectId, internalTeam);
            return query.ToList<GenericInfo>();
        }
        private TreeNode GetNodeById(int id, List<TreeNode> nodes) 
        {
            foreach(TreeNode node in nodes)
            {
                if (node.Id == id) return node;
            }
            return null;
        }
        private List<TreeNode> AddToList(int id, string name, List<TreeNode> node)
        {
            if (node == null) node = new List<TreeNode>();
            node.Add(new TreeNode(id, name));
            return node;
        }

        private bool IsInList(int id, List<TreeNode> list)
        {
            if (list == null) return false;
            foreach (TreeNode item in list)
            {
                if (item.Id == id) return true;
            }
            return false;
        }

        private KmProjectTeamRole GetRole(string shortname)
        {
            return _context.KmProjectTeamRoles.Where(a => a.Shortname.Equals(shortname)).FirstOrDefault();
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.ID == id);
        }
        private bool FileExists(int id)
        {
            return _context.KmFiles.Any(e => e.Id == id);
        }
        private bool ContactExists(int id, int clientId)
        {
            return _context.CrmContacts.Any(e => e.Id == id && e.CrmClientId == clientId);
        }

        private bool ProductExists(int id)
        {
            return _context.KmProducts.Any(e => e.Id == id);
        }

        private bool ProjectExists(int id)
        {
            return _context.KmProjects.Any(e => e.Id == id);
        }
        private async Task<KmProjectExternalTeam> UpdateExternalTeam(string role, int teamId, int clientId, int projectId)
        {
            GenericInfo pa = GetTeam(role, projectId, false);
            if (pa == null)
            {
                if (teamId > 0)
                {
                    return await AddExternalTeam(role, teamId, clientId, projectId);
                }
            }
            else
            {
                if (teamId == 0)
                {
                    return await RemoveExternalTeam(role, pa.Id, projectId);
                }
                else
                {
                    if (teamId != pa.Id)
                    {
                        return await ReplaceExternalTeam(role, teamId, projectId);
                    }
                }
            }
            return null;
        }
        private async Task<int> UpdateInternalTeam(string role, List<int> teamIds, int projectId)
        {
            List<GenericInfo> pas = GetTeams(role, projectId, true);

            int n = 0;
            if(pas == null || pas.Count() == 0)
            {
                foreach(int teamId in teamIds)
                {
                    await AddInternalTeam(role, new List<int>(new[] { teamId }), projectId);
                }
            }
            else
            {
                foreach(int teamId in teamIds)
                {
                    GenericInfo cpa = pas.Where(a => a.Id == teamId).FirstOrDefault();
                    if (cpa == null)
                    {
                        await AddInternalTeam(role, new List<int>(new[] { teamId }), projectId);
                    }
                }
                foreach (GenericInfo pa in pas)
                {
                    // ragu makai ini
                    // int? ci = teamIds.Where(a => a == pa.Id).FirstOrDefault();
                    bool found = false;

                    foreach (int teamId in teamIds)
                    {
                        if (teamId == pa.Id)
                        {
                            found = true;
                            break;
                        }
                    }
                    if(!found)
                    {
                        await RemoveInternalTeam(role, pa.Id, projectId);
                    }
                }
            }
            return n;
        }
        private async Task<KmProjectExternalTeam> ReplaceExternalTeam(string role, int newContactId, int projectId)
        {
            var paRole = GetRole(role);
            if (paRole != null && paRole.Id > 0)
            {
                KmProjectExternalTeam team = _context.KmProjectExternalTeams.Where(a => a.RoleId == paRole.Id && a.ProjectId == projectId).FirstOrDefault();

                if (team != null)
                {
                    team.ContactId = newContactId;
                    _context.Entry(team).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                    return team;
                }
            }
            return null;
        }
        private async Task<KmProjectInternalTeam> ReplaceInternalTeam(string role, int newUserid, int projectId)
        {
            var paRole = GetRole(role);
            if (paRole != null && paRole.Id > 0)
            {
                KmProjectInternalTeam team = _context.KmProjectInternalTeams.Where(a => a.RoleId == paRole.Id && a.ProjectId == projectId).FirstOrDefault();

                if (team != null)
                {
                    team.UserId = newUserid;
                    _context.Entry(team).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                    return team;
                }
            }
            return null;
        }
        private async Task<KmProjectProduct> RemoveProjectProduct(int productId, int projectId)
        {
            KmProjectProduct pp = _context.KmProjectProducts.Where(a => a.ProductId == productId && a.ProjectId == projectId).FirstOrDefault();
            if(pp != null)
            {
                _context.KmProjectProducts.Remove(pp);
                await _context.SaveChangesAsync();
                return pp;
            }

            return null;
        }
        private async Task<KmProjectExternalTeam> RemoveExternalTeam(string role, int contactId, int projectId)
        {
            var paRole = GetRole(role);
            if (paRole != null && paRole.Id > 0)
            {
                KmProjectExternalTeam team = _context.KmProjectExternalTeams.Where(a => a.RoleId == paRole.Id && a.ContactId == contactId && a.ProjectId == projectId).FirstOrDefault();

                if (team != null)
                {
                    _context.KmProjectExternalTeams.Remove(team);
                    await _context.SaveChangesAsync();
                    return team;
                }
            }
            return null;

        }
        private async Task<KmProjectInternalTeam> RemoveInternalTeam(string role, int userId, int projectId)
        {
            var paRole = GetRole(role);
            if(paRole != null && paRole.Id > 0)
            {
                KmProjectInternalTeam team = _context.KmProjectInternalTeams.Where(a => a.RoleId == paRole.Id && a.UserId == userId && a.ProjectId == projectId).FirstOrDefault();

                if (team != null)
                {
                    _context.KmProjectInternalTeams.Remove(team);
                    await _context.SaveChangesAsync();
                    return team;
                }
            }
            return null;
        }
        private async Task<int> AddInternalTeam(string role, List<int> userIds, int projectId)
        {
            var paRole = GetRole(role);
            int n = 0;
            foreach(int userId in userIds)
            {
                if (paRole != null && paRole.Id > 0 && UserExists(userId))
                {
                    var paTeam = new KmProjectInternalTeam()
                    {
                        ProjectId = projectId,
                        RoleId = paRole.Id,
                        UserId = userId
                    };
                    _context.KmProjectInternalTeams.Add(paTeam);
                    n++;
                }

            }
            await _context.SaveChangesAsync();

            return n;
        }

        private async Task<KmProjectExternalTeam> AddExternalTeam(string role, int contactId, int clientId, int projectId)
        {
            var paRole = GetRole(role);

            if (paRole != null && paRole.Id > 0 && ContactExists(contactId, clientId))
            {
                KmProjectExternalTeam ex = _context.KmProjectExternalTeams.Where(a => a.ContactId == contactId && a.RoleId == paRole.Id && a.ProjectId == projectId).FirstOrDefault();
                if (ex != null)
                {
                    // Already exist
                    return ex;
                }

                var paTeam = new KmProjectExternalTeam()
                {
                    ProjectId = projectId,
                    RoleId = paRole.Id,
                    ContactId = contactId
                };
                _context.KmProjectExternalTeams.Add(paTeam);
                await _context.SaveChangesAsync();
                return paTeam;
            }

            return null;
        }

        private async Task<KmProjectProduct> AddProjectProduct(int productId, int projectId)
        {
            if (productId > 0 && ProductExists(productId))
            {
                KmProjectProduct pp = new KmProjectProduct()
                {
                    ProjectId = projectId,
                    ProductId = productId
                };
                _context.KmProjectProducts.Add(pp);
                await _context.SaveChangesAsync();

                return pp;
            }
            return null;
        }
        private GenericInfo GetTribe(int tribeId)
        {
            var query = from tribe in _context.CoreTribes
                        where tribe.Id == tribeId && tribe.IsDeleted == false
                        select new GenericInfo()
                        {
                            Id = tribe.Id,
                            Text = tribe.Tribe

                        };
            return query.FirstOrDefault();
        }
        private GenericInfo GetClient(int clientId)
        {
            var query = from client in _context.CrmClients
                        where client.Id == clientId && client.IsDeleted == false
                        select new GenericInfo()
                        {
                            Id = client.Id,
                            Text = client.Company

                        };
            return query.FirstOrDefault();
        }
        private int GetYear(int yearId)
        {
            var query = from year in _context.KmYears
                        where year.Id == yearId && year.IsDeleted == false
                        select year.Year;
            return query.FirstOrDefault();
        }

        private int GetTotalInsight(string website)
        {
            IQueryable<KmInsight> queryCountWebsite;
            if (website == "")
            {
                queryCountWebsite = from a in _context.KmInsights
                                    join ia in _context.KmInsightAuthors on a.Id equals ia.InsightId
                                    join ic in _context.KmInsightCategories on a.Id equals ic.InsightId
                                    where !a.IsDeleted && a.Publish == true
                                    orderby a.LastUpdated descending
                                    select a;
            }
            else
            {
                queryCountWebsite = from a in _context.KmInsights
                                    join ia in _context.KmInsightAuthors on a.Id equals ia.InsightId
                                    join ic in _context.KmInsightCategories on a.Id equals ic.InsightId
                                    where !a.IsDeleted && a.Publish == true && a.Website == website
                                    orderby a.LastUpdated descending
                                    select a;
            }

            return queryCountWebsite.Count();
        }
        private string GetOwner(int ownerId)
        {
            CorePlatform platform = _context.CorePlatforms.Find(ownerId);
            if (platform == null) return "";

            return platform.Platform;
        }
        private async Task<FolderContent> GetFolderContent(int folderId, int projectId, int OneGML)
        {
            var response = new FolderContent();

            List<string> rootFolders = _context.KmWebinarRootFolders.Select(a => a.Folder).ToList();

            if (folderId > 0)
            {
                var query1 = from files in _context.KmFiles
                             where files.Id == folderId && files.IsDeleted == false && files.Onegml == (OneGML == 1)
                             select new FileFolderInfo()
                             {
                                 Id = files.Id,
                                 Name = files.Name,
                                 Description = files.Description,
                                 Date = files.CreatedDate,
                                 OwnerId = files.OwnerId
                             };
                FileFolderInfo info = query1.FirstOrDefault();

                if (info != null && info.Id > 0)
                {
                    response.Id = info.Id;
                    response.Name = info.Name;
                    response.Date = info.Date;
                    response.Location = GetFileLocation(info.Id);
                    if(info.OwnerId != 0)
                    {
                        response.OwnerId = info.OwnerId;
                        response.Owner = GetOwner(info.OwnerId);
                    }
                }
            }
            else if(folderId == 0)
            {
                if(OneGML == 1)
                {
                    response.Location = "OneGML";
                }
                else
                {
                    response.Location = GetLocationByProjectId(projectId);
                }
            }
            else if(folderId < 0)
            {
                DateTime now = DateTime.Now;
                // Webinar
                if(folderId == -1)
                {
                    // Root of webinar folders
                    response.folders = new List<FileFolderInfo>();
                    response.files = new List<FileFolderInfo>();
                    foreach (string folder in rootFolders)
                    {
                        response.folders.AddRange(await GetWebinarFolders(folder, now));
                        response.files.AddRange(await GetWebinarFiles(folder, now));
                    }
                    return response;
                }
                else
                {
                    KmWebinarFileFolder folder = _context.KmWebinarFileFolders.Find(-folderId);
                    if(folder != null)
                    {
                        response.folders = await GetWebinarFolders(Path.Combine(new[] { folder.RootFolder, folder.FolderFileName }), now);
                        response.files = await GetWebinarFiles(Path.Combine(new[] { folder.RootFolder, folder.FolderFileName }), now);
                    }
                    return response;
                }
            } 
            var query2 = from files in _context.KmFiles
                         where files.ParentId == folderId && files.ProjectId == projectId && files.IsDeleted == false && files.IsFolder && files.Onegml == (OneGML == 1)
                         select new FileFolderInfo()
                         {
                             Id = files.Id,
                             Name = files.Name,
                             Description = files.Description,
                             Date = files.CreatedDate,
                             OwnerId = files.OwnerId
                         };
            response.folders = query2.ToList<FileFolderInfo>();
            foreach(FileFolderInfo ino in response.folders)
            {
                ino.Location = GetFileLocation(ino.Id);
                if (ino.OwnerId != 0) ino.Owner = GetOwner(ino.OwnerId);
                ino.FileType = "Folder";
            }

            if(rootFolders != null & response.Location.Equals("OneGML"))
            {
                if(folderId == 0)
                {
                    FileFolderInfo info = new FileFolderInfo()
                    {
                        Id = -1,
                        Name = "Webinar",
                        Location = "",
                        FileType = "Folder",
                        Description = "",
                        Owner = "OneGML",
                        OwnerId = 0,
                        Date = DateTime.Now
                    };
                    response.folders.Add(info);
                }
            }


            var query3 = from files in _context.KmFiles
                         where files.ParentId == folderId && files.ProjectId == projectId && files.IsDeleted == false && !files.IsFolder && files.Onegml == (OneGML == 1)
                         select new FileFolderInfo()
                         {
                             Id = files.Id,
                             Name = files.Name,
                             Description = files.Description,
                             Date = files.CreatedDate,
                             FileType = files.FileType
                         };
            response.files = query3.ToList<FileFolderInfo>();
            foreach(FileFolderInfo info1 in response.files)
            {
                info1.Location = GetFileLocation(info1.Id);
            }

            return response;
        }
        private async Task<List<FileFolderInfo>> GetWebinarFiles(string folder, DateTime now)
        {
            List<FileFolderInfo> response = new List<FileFolderInfo>();

            DirectoryInfo dirWebinar = new DirectoryInfo(folder); 
            
            foreach(var fi in dirWebinar.EnumerateFiles("*.mp4"))
            {
                KmWebinarFileFolder curFile = _context.KmWebinarFileFolders.Where(a => !a.IsFolder && a.RootFolder.Equals(fi.DirectoryName) && a.FolderFileName.Equals(fi.Name)).FirstOrDefault();
                if (curFile == null)
                {
                    curFile = new KmWebinarFileFolder()
                    {
                        IsFolder = false,
                        RootFolder = fi.DirectoryName,
                        FolderFileName = fi.Name,
                        CreatedDate = now,
                        CreatedBy = 1
                    };
                    _context.KmWebinarFileFolders.Add(curFile);
                    await _context.SaveChangesAsync();
                }
                if (curFile  != null)
                {
                    response.Add(new FileFolderInfo()
                    {
                        Id = -curFile.Id,
                        Name = fi.Name,
                        FileType = "mp4",
                        Location = "",
                        Description = "",
                        Date = fi.CreationTime,
                        OwnerId = -1
                    });
                }

            }

            return response;
        }
        


        private async Task<List<FileFolderInfo>> GetWebinarFolders(string folder, DateTime now)
        {
            List<FileFolderInfo> response = new List<FileFolderInfo>();

            DirectoryInfo dirWebinars = new DirectoryInfo(folder);

            var dirs = from dir in dirWebinars.EnumerateDirectories()
                       select new
                       {
                           ProgDir = dir,
                       };
            foreach (var dir in dirs)
            {
                KmWebinarFileFolder curDir = _context.KmWebinarFileFolders.Where(a => a.IsFolder && a.RootFolder.Equals(folder) && a.FolderFileName.Equals(dir.ProgDir.Name)).FirstOrDefault();
                if(curDir == null)
                {
                    curDir = new KmWebinarFileFolder()
                    {
                        IsFolder = true,
                        RootFolder = folder,
                        FolderFileName = dir.ProgDir.Name,
                        CreatedDate = now,
                        CreatedBy = 1
                    };
                    _context.KmWebinarFileFolders.Add(curDir);
                    await _context.SaveChangesAsync();
                }
                if(curDir != null)
                {
                    response.Add(new FileFolderInfo()
                    {
                        Id = -curDir.Id,
                        Name = dir.ProgDir.Name,
                        Description = dir.ProgDir.FullName,
                        Date = dir.ProgDir.CreationTime,
                        OwnerId = -1
                    });
                }
            }

            return response;
        }

        private List<int> SplitString(string str)
        {
            List<int> r = new List<int>();

            if (str.Equals("0"))
            {
                return r;
            }
            foreach (string s in str.Split(","))
            {
                try
                {
                    r.Add(Int32.Parse(s));
                }
                catch
                {
                    return null;
                };
            }

            return r;
        }
        private CrmContact FindContactByEmail(string email)
        {
            return _context.CrmContacts.Where(a => a.Email1.Equals(email) || a.Email2.Equals(email) || a.Email3.Equals(email) || a.Email4.Equals(email)).FirstOrDefault();
        }

        private string GetFileLocation(int fileId)
        {
            List<string> pre = new List<string>();

            KmFile file = _context.KmFiles.Find(fileId);
            if(file != null)
            {
                if (file.Onegml)
                {
                    pre.Add("OneGML");
                }
                else
                {
                    var query = from proj in _context.KmProjects
                                join client in _context.CrmClients
                                on proj.ClientId equals client.Id
                                join year in _context.KmYears
                                on proj.YearId equals year.Id
                                where proj.Id == file.ProjectId
                                select new FileAccessInfo()
                                {
                                    FileId = file.Id,
                                    Name = "",
                                    Action = "",
                                    Filename = "",
                                    FileType = "",
                                    Tribe = "",
                                    Client = client.Company,
                                    Year = year.Year.ToString(),
                                    Project = proj.Name,
                                    Onegml = file.Onegml,
                                    LastAccess = DateTime.Now
                                };
                    FileAccessInfo info = query.FirstOrDefault();
                    if(info != null)
                    {
                        pre.AddRange(new[] { info.Client, info.Year.ToString(), info.Project });
                    }
                }
            }

            return GetFileLocation(fileId, pre);

        }

        private string GetLocationByProjectId(int projectId)
        {
            var query = from proj in _context.KmProjects
                        join client in _context.CrmClients
                        on proj.ClientId equals client.Id
                        join year in _context.KmYears
                        on proj.YearId equals year.Id
                        where proj.Id == projectId
                        select new FileAccessInfo()
                        {
                            FileId = proj.Id,
                            Name = "",
                            Action = "",
                            Filename = "",
                            FileType = "",
                            Tribe = "",
                            Client = client.Company,
                            Year = year.Year.ToString(),
                            Project = proj.Name,
                            Onegml = false,
                            LastAccess = DateTime.Now
                        };
            FileAccessInfo info = query.FirstOrDefault();
            if (info != null)
            {
                return string.Join(" / ", new[] { info.Client, info.Year.ToString(), info.Project });
            }
            return "";
        }
        private string GetFileLocation(int fileId, List<string> pre)
        {
            KmFile file = _context.KmFiles.Find(fileId);
            if (file == null) return "";

            List<string> list = new List<string>();

            int folderId = file.ParentId;
            while(folderId != 0)
            {
                KmFile folder = _context.KmFiles.Find(folderId);
                if(folder == null)
                {
                    break;
                }
                list.Add(folder.Name);
                folderId = folder.ParentId;
            }

            pre.Reverse();

            foreach(string s in pre)
            {
                list.Add(s);
            }
            list.Reverse();
            return string.Join(" / ", list);
        }

        private bool IsInsightSlugUnique(string slug, int insightId)
        {
            return !(_context.KmInsights.Where(a => a.Slug.Equals(slug) && a.Id != insightId && !a.IsDeleted).Any());
        }
        
    }



}