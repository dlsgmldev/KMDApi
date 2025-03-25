using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KDMApi.DataContexts;
using KDMApi.Models;
using KDMApi.Models.Crm;
using Microsoft.AspNetCore.Authorization;
using KDMApi.Models.Km;
using Microsoft.AspNetCore.Cors;
using ClosedXML.Excel;
using System.IO;

namespace KDMApi.Controllers
{
    [Authorize]
    [Route("v1/clients")]
    [ApiController]
    [EnableCors("QuBisaPolicy")]
    public class CrmClientsController : ControllerBase
    {
        private readonly DefaultContext _context;

        public CrmClientsController(DefaultContext context)
        {
            _context = context;
        }

        // GET: v1/clients/list/0/0/0/1/10/*
        /**
         * @api {get} /clients/list/{industryFilter}/{segmentFilter}/{rmFilter}/{page}/{perPage}/{search} Filter clients
         * @apiVersion 1.0.0
         * @apiName GetCrmClients
         * @apiGroup Clients
         * @apiPermission ApiUser
         * 
         * @apiParam {String} industryFilter  0 untuk tidak menggunakan filter, atau comma-separated values dari industryId, misal 1,3.
         * @apiParam {String} segmentFilter   0 untuk tidak menggunakan filter, atau comma-separated values dari segmentId, misal 2,3.
         * @apiParam {String} rmFilter        0 untuk tidak menggunakan filter, atau comma-separated values dari relManagerId, misal 4,7.
         * @apiParam {Number} page            Halaman yang ditampilkan.
         * @apiParam {Number} perPage         Jumlah data per halaman.
         * @apiParam {String} search          Tanda bintang (*) untuk tidak menggunakan search, atau kata yang mau d-search.
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *     "clients": [
         *       {
         *         "id": 2,
         *         "company": "Bank Mandiri",
         *         "industry": "Financial Services - Bank",
         *         "address": "Jl. Gatot Subroto",
         *         "phone": null
         *       },
         *       {
         *         "id": 3,
         *         "company": "Bank Central Asia (BCA)",
         *         "industry": "Financial Services - Bank",
         *         "address": "Jl. Jend Sudirman",
         *         "phone": "021-800900"
         *       }
         *     ],
         *     "industries": [
         *       {
         *         "id": 1,
         *         "industry": "Agriculture"
         *       },
         *       {
         *         "id": 2,
         *         "industry": "Chemical"
         *       },
         *       {
         *         "id": 3,
         *         "industry": "Commerce"
         *       },
         *       {
         *         "id": 4,
         *         "industry": "Construction"
         *       },
         *       {
         *         "id": 5,
         *         "industry": "Education"
         *       },
         *     ],
         *     "relManagers": [
         *       {
         *         "id": 5,
         *         "relManagerId": 1,
         *         "name": "Leviana Wijaya",
         *         "email": "levi@gmlperformannce.co.id",
         *         "segment": "Private",
         *         "branch": "Jakarta"
         *       },
         *       {
         *         "id": 7,
         *         "relManagerId": 2,
         *         "name": "Grace",
         *         "email": "grace@gmlperformannce.co.id",
         *         "segment": "Private",
         *         "branch": "Jakarta"
         *       },
         *       {
         *         "id": 6,
         *         "relManagerId": 3,
         *         "name": "Meilliana Nasution",
         *         "email": "meilliana@gmlperformannce.co.id",
         *         "segment": "BUMN",
         *         "branch": "Jakarta"
         *       },
         *     ],
         *     "segments": [
         *       {
         *         "id": 1,
         *         "segment": "BUMN"
         *       },
         *       {
         *         "id": 2,
         *         "segment": "Government"
         *       },
         *       {
         *         "id": 3,
         *         "segment": "Private"
         *       },
         *       {
         *         "id": 4,
         *         "segment": "Public Seminar"
         *       }
         *     ],
         *     "errors": [
         *       {
         *         "code": "0",
         *         "description": ""
         *       }
         *     ],
         *     "info": {
         *       "page": 1,
         *       "perPage": 10,
         *       "total": 2
         *     }
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         * @apiErrorExample {json} Error-Response 
         *   {
         *     "clients": null,
         *     "industries": null,
         *     "relManagers": null,
         *     "segments": null,
         *     "errors": [
         *       {
         *         "code": "industry_filter" | "segment_filter" | "rm_filter",
         *         "description": "Invalid industry filter."
         *       }
         *     ],
         *     "info": null
         *   }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("list/{industryFilter}/{segmentFilter}/{rmFilter}/{page}/{perPage}/{search}")]
        public async Task<ActionResult<ClientListResponse>> getCrmClients(string industryFilter, string segmentFilter, string rmFilter, int page, int perPage, string search) 
        {
            page = page <= 0 ? 1 : page;
            perPage = perPage <= 0 ? 5 : perPage;

            if (search == null)
            {
                ClientListResponse t = new ClientListResponse(new[] { new Error("search", "Search cannot be null. Use \"*\" if you don't want to use search.") });
                return t;
            }


            // industryFilter comma separated ids of industries, when industry is 1
            ClientListResponse response = new ClientListResponse();
            
            var request = new ClientListRequest();
                        
            List<int> t1 = SplitString(industryFilter);
            if(t1 == null)
            {
                ClientListResponse t = new ClientListResponse(new[] { new Error("industry_filter", "Invalid industry filter.") });
                return t;
            }
            request.filterIndustries = t1;

            List<int> t2 = SplitString(segmentFilter);
            if (t2 == null)
            {
                ClientListResponse t = new ClientListResponse(new[] { new Error("segment_filter", "Invalid segment filter.") });
                return t;
            }
            request.filterSegments = t2;

            List<int> t3 = SplitString(rmFilter);
            if (t3 == null)
            {
                ClientListResponse t = new ClientListResponse(new[] { new Error("rm_filter", "Invalid rm filter.") });
                return t;
            }
            request.filterRelManagers = t3;

            request.page = page;
            request.countPerPage = perPage;
            request.search = search;

            var query = BuildQuery(request);

            response.clients = await query.Skip(request.countPerPage * (request.page - 1)).Take(request.countPerPage).ToListAsync<ClientInfo>();

            var industryQuery = from i in _context.CrmIndustries
                                where i.IsDeleted == false && !i.Industry.Equals("")
                                select new IndustryInfo()
                                {
                                    Id = i.Id,
                                    Industry = i.Industry
                                };
            response.industries = await industryQuery.ToListAsync();

            var segmentQuery = from s in _context.CrmSegments
                               where s.IsDeleted == false 
                               select new SegmentInfo()
                               {
                                   Id = s.Id,
                                   Segment = s.Segment
                               };
            response.segments = await segmentQuery.ToListAsync();

            var relManagerQuery = from rel in _context.CrmRelManagers
                              join user in _context.Users
                              on rel.UserId equals user.ID
                              join netuser in _context.AspNetUsers
                              on user.IdentityId equals netuser.Id
                              join branch in _context.CrmBranches
                              on rel.BranchId equals branch.Id
                              where rel.IsDeleted == false && rel.isActive == true
                              orderby user.FirstName
                              select new RelManagerInfo()
                              {
                                  Id = user.ID,
                                  RelManagerId = rel.Id,
                                  Name = user.FirstName,
                                  Email = netuser.Email,
                                  Segment = rel.SegmentId.ToString(),
                                  Branch = branch.Branch
                              };

            response.relManagers = await relManagerQuery.ToListAsync<RelManagerInfo>();

            foreach(RelManagerInfo irm in response.relManagers)
            {
                if(irm.Segment.Equals("0"))
                {
                    irm.Segment = "-";
                }
                else
                {
                    try
                    {
                        int sid = Convert.ToInt32(irm.Segment);
                        CrmSegment segment = _context.CrmSegments.Find(sid);
                        if(sid != null)
                        {
                            irm.Segment = segment.Segment;
                        }
                        else
                        {
                            irm.Segment = "-";
                        }
                    }
                    catch
                    {
                        irm.Segment = "-";
                    }
                }
            }

            int total = query.Count();                                  
            response.info = new PaginationInfo(page, perPage, total);

            return response;

         
        }

        /**
         * @api {get} /clients/search/{search} Search clients
         * @apiVersion 1.0.0
         * @apiName GetSearchClient
         * @apiGroup Clients
         * @apiPermission ApiUser
         * 
         * @apiParam {String} search          Tanda bintang (*) untuk tidak menggunakan search, atau kata yang mau d-search.
         * 
         * @apiSuccessExample Success-Response:
         *  [
         *      {
         *          "id": 28,
         *          "text": "3D NETWORKS INDONESIA, PT"
         *      },
         *      {
         *          "id": 29,
         *          "text": "3M INDONESIA, PT (MINNESOTA MINING MANUFACTURING)"
         *      },
         *      {
         *          "id": 3,
         *          "text": "Bank Central Asia"
         *      },
         *      {
         *          "id": 26,
         *          "text": "Bank Ekonomi"
         *      },
         *      {
         *          "id": 27,
         *          "text": "Bank Ekonomi"
         *      },
         *      {
         *          "id": 12,
         *          "text": "lkjka"
         *      },
         *      {
         *          "id": 10,
         *          "text": "PT Alam Sutera"
         *      },
         *      {
         *          "id": 2,
         *          "text": "PT Bank Mandiri Tbk."
         *      }
         *  ]
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("search/{search}")] 
        public async Task<ActionResult<List<GenericInfo>>> GetSearchClient(string search)
        {
            IQueryable<GenericInfo> query;
            if(search.Equals("*"))
            {
                query = from client in _context.CrmClients
                        where !client.IsDeleted
                        orderby client.Company
                        select new GenericInfo()
                        {
                            Id = client.Id,
                            Text = client.Company
                        };
            }
            else
            {
                query = from client in _context.CrmClients
                        where !client.IsDeleted && client.Company.Contains(search)
                        orderby client.Company
                        select new GenericInfo()
                        {
                            Id = client.Id,
                            Text = client.Company
                        };
            }
            return await query.ToListAsync<GenericInfo>();

        }

        // GET: v1/clients/5
        /**
         * @api {get} /clients/{id} Get client berdasarkan Id
         * @apiVersion 1.0.0
         * @apiName GetCrmClient
         * @apiGroup Clients
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} id        Id dari client yang bersangkutan.
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *     "client": {
         *       "id": 3,
         *       "company": "Bank BCA",
         *       "address1": "Jl. Sudirman",
         *       "address2": null,
         *       "address3": null,
         *       "phone": "0251-900899",
         *       "fax": null,
         *       "website": null,
         *       "remarks": null,
         *       "crmIndustryId": 8,
         *       "createdDate": "0001-01-01T00:00:00",
         *       "createdBy": 0,
         *       "lastUpdated": "2020-02-12T10:36:31.0782305",
         *       "lastUpdatedBy": 3,
         *       "isDeleted": false,
         *       "deletedBy": 0,
         *       "deletedDate": "2020-02-12T10:41:55.6528913"
         *     },
         *     "industry": {
         *       "id": 8,
         *       "industry": "Financial Services - Bank"
         *     },
         *     "contacts": [
         *       {
         *         "id": 9,
         *         "name": "Bandi",
         *         "salutation": null,
         *         "email": "bandi@gmail.com",
         *         "phone": "0819-878-909",
         *         "department": "Marketing",
         *         "position": "Manager",
         *         "valid": true
         *       }
         *     ],
         *     "relManagers": [
         *       {
         *         "id": 5,
         *         "relManagerId": 1,
         *         "name": "Leviana Wijaya",
         *         "email": "levi@gmlperformannce.co.id",
         *         "segment": "Private",
         *         "branch": "Jakarta"
         *       },
         *       {
         *         "id": 6,
         *         "relManagerId": 3,
         *         "name": "Meilliana Nasution",
         *         "email": "meilliana@gmlperformannce.co.id",
         *         "segment": "BUMN",
         *         "branch": "Jakarta"
         *       },
         *       {
         *         "id": 9,
         *         "relManagerId": 5,
         *         "name": "Irfan",
         *         "email": "irfan@lutanedukasi.co.id",
         *         "segment": "BUMN",
         *         "branch": "Jakarta"
         *       }
         *     ],
         *     "errors": [
         *       {
         *         "code": "0",
         *         "description": ""
         *       }
         *     ]
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         * @apiError NotFound Client Id salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("{id}")]
        public async Task<ActionResult<GetClientResponse>> GetCrmClient(int id)
        {
            CrmClient crmClient = _context.CrmClients.Where(a => a.Id == id && a.IsDeleted == false).FirstOrDefault<CrmClient>();
            if(crmClient == null || crmClient.Id == 0)
            {
                return NotFound();
            }

            var response = new GetClientResponse();
            response.Client = crmClient;
            response.Industry = getClientIndustryInfo(crmClient.CrmIndustryId);
            response.Contacts = getClientContacts(crmClient.Id);
            response.RelManagers = getClientRelManagers(crmClient.Id);

            return response;
        }

        /**
         * @api {get} /clients/contact/{month}/{industryFilter}/{page}/{perPage}/{search} Get client contacts
         * @apiVersion 1.0.0
         * @apiName GetClientContactList
         * @apiGroup Clients
         * @apiPermission ApiUser
         * 
         * @apiParam {String} month             Filter untuk bulan, dalam format YYYYMM, misal 202005 untuk bulan Mei 2020. Gunakan 0 untuk tidak menggunakan filter bulan.
         * @apiParam {String} industryFilter    0 untuk tidak menggunakan filter, atau comma-separated values dari industryId, misal 1,3.
         * @apiParam {Number} page              Halaman yang ditampilkan.
         * @apiParam {Number} perPage           Jumlah data per halaman.
         * @apiParam {String} search            * untuk tidak menggunakan search, atau kata yang dicari
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "contacts": [
         *           {
         *               "id": 38141,
         *               "clientId": 14378,
         *               "name": "Putri Andini",
         *               "company": "Mulia Sawit Agro Lestari ",
         *               "email": "putri.andini@msalgroup.com",
         *               "phone": "(081)288-2693-22_",
         *               "department": "HR",
         *               "position": "HR",
         *               "valid": true,
         *               "salutation": "Ms."
         *           },
         *           {
         *               "id": 38183,
         *               "clientId": 14387,
         *               "name": "purwanti",
         *               "company": "PTPN Holding",
         *               "email": "purwantibudilaksono@gmail.com",
         *               "phone": "(081)370-8537-05_",
         *               "department": "perencanaan",
         *               "position": "spv",
         *               "valid": true,
         *               "salutation": "Ms."
         *           }
         *   }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("contact/{month}/{industryFilter}/{page}/{perPage}/{search}")]
        public async Task<ActionResult<ClientContactResponse>> GetClientContactList(string month, string industryFilter, int page, int perPage, string search)
        {
            List<int> filterIndustries = SplitString(industryFilter);
            if (filterIndustries == null)
            {
                return BadRequest(new { error = "Invalid industry filter" });
            }

            DateTime fromDate = new DateTime(1900, 1, 1);
            DateTime toDate = new DateTime(2100, 12, 31);

            if(!month.Trim().Equals("0"))
            {
                month = string.Join("", new[] { month, "01 00:00:00,000" });
                fromDate = DateTime.ParseExact(month, "yyyyMMdd HH:mm:ss,fff", System.Globalization.CultureInfo.InvariantCulture);
                toDate = new DateTime(fromDate.AddMonths(1).Year, fromDate.AddMonths(1).Month, 1);
            }
            
            IQueryable<ClientContactInfo> query;
            if (filterIndustries.Count == 0)
            {
                if(search.Trim().Equals("*"))
                {
                    query = from contact in _context.CrmContacts
                            join client in _context.CrmClients
                            on contact.CrmClientId equals client.Id
                            where client.IsDeleted == false && contact.IsDeleted == false && contact.LastUpdated > fromDate && contact.LastUpdated < toDate
                            select new ClientContactInfo()
                            {
                                Id = contact.Id,
                                Name = contact.Name,
                                Company = client.Company,
                                Email = string.Join(", ", new[] { contact.Email1, contact.Email2, contact.Email3, contact.Email4 }),
                                Phone = string.Join(", ", new[] { contact.Phone1, contact.Phone2, contact.Phone3 }),
                                Department = contact.Department,
                                ClientId = client.Id,
                                Position = contact.Position,
                                Valid = contact.Valid,
                                Salutation = contact.Salutation
                            };

                }
                else
                {
                    query = from contact in _context.CrmContacts
                            join client in _context.CrmClients
                            on contact.CrmClientId equals client.Id
                            where client.IsDeleted == false && contact.IsDeleted == false && contact.LastUpdated > fromDate && contact.LastUpdated < toDate && contact.Name.Contains(search)
                            select new ClientContactInfo()
                            {
                                Id = contact.Id,
                                Name = contact.Name,
                                Company = client.Company,
                                Email = string.Join(", ", new[] { contact.Email1, contact.Email2, contact.Email3, contact.Email4 }),
                                Phone = string.Join(", ", new[] { contact.Phone1, contact.Phone2, contact.Phone3 }),
                                Department = contact.Department,
                                ClientId = client.Id,
                                Position = contact.Position,
                                Valid = contact.Valid,
                                Salutation = contact.Salutation
                            };

                }

            }
            else
            {
                if (search.Trim().Equals("*"))
                {
                    query = from contact in _context.CrmContacts
                            join client in _context.CrmClients
                            on contact.CrmClientId equals client.Id
                            join industry in _context.CrmIndustries
                            on client.CrmIndustryId equals industry.Id
                            where client.IsDeleted == false && contact.IsDeleted == false && filterIndustries.Contains(industry.Id) && contact.LastUpdated > fromDate && contact.LastUpdated < toDate
                            select new ClientContactInfo()
                            {
                                Id = contact.Id,
                                Name = contact.Name,
                                Company = client.Company,
                                Email = string.Join(", ", new[] { contact.Email1, contact.Email2, contact.Email3, contact.Email4 }),
                                Phone = string.Join(", ", new[] { contact.Phone1, contact.Phone2, contact.Phone3 }),
                                Department = contact.Department,
                                ClientId = client.Id,
                                Position = contact.Position,
                                Valid = contact.Valid,
                                Salutation = contact.Salutation
                            };

                }
                else
                {
                    query = from contact in _context.CrmContacts
                            join client in _context.CrmClients
                            on contact.CrmClientId equals client.Id
                            join industry in _context.CrmIndustries
                            on client.CrmIndustryId equals industry.Id
                            where client.IsDeleted == false && contact.IsDeleted == false && filterIndustries.Contains(industry.Id) && contact.LastUpdated > fromDate && contact.LastUpdated < toDate && contact.Name.Contains(search)
                            select new ClientContactInfo()
                            {
                                Id = contact.Id,
                                Name = contact.Name,
                                Company = client.Company,
                                Email = string.Join(", ", new[] { contact.Email1, contact.Email2, contact.Email3, contact.Email4 }),
                                Phone = string.Join(", ", new[] { contact.Phone1, contact.Phone2, contact.Phone3 }),
                                Department = contact.Department,
                                ClientId = client.Id,
                                Position = contact.Position,
                                Valid = contact.Valid,
                                Salutation = contact.Salutation
                            };

                }

            }

            ClientContactResponse response = new ClientContactResponse();
            int total = query.Count();
            response.Info = new PaginationInfo(page, perPage, total);
            response.Contacts = await query.Skip(perPage * (page - 1)).Take(perPage).ToListAsync<ClientContactInfo>();

            foreach(ClientContactInfo con in response.Contacts)
            {
                con.Email = con.Email.TrimEnd(new[] { ',', ' ' });
                con.Phone = con.Phone.TrimEnd(new[] { ',', ' ' });
            }
            return response;
        }

        // Ini ngga dipakai
        /**
         * @api {get} /clients/contact/export/{month}/{industryFilter} Export client contacts
         * @apiVersion 1.0.0
         * @apiName ExportClientContactList
         * @apiGroup Clients
         * @apiPermission ApiUser
         * 
         * @apiParam {String} month             Filter untuk bulan, dalam format YYYYMM, misal 202005 untuk bulan Mei 2020. Gunakan 0 untuk tidak menggunakan filter bulan.
         * @apiParam {String} industryFilter    0 untuk tidak menggunakan filter, atau comma-separated values dari industryId, misal 1,3.
         * 
         * @apiSuccessExample Success-Response:
         *   File Excel download
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("contact/export/{month}/{industryFilter}")]
        public async Task<IActionResult> ExportClientContactList(string month, string industryFilter)
        {
            List<int> filterIndustries = SplitString(industryFilter);
            if (filterIndustries == null)
            {
                return BadRequest(new { error = "Invalid industry filter" });
            }

            DateTime fromDate = new DateTime(1900, 1, 1);
            DateTime toDate = new DateTime(2100, 12, 31);

            if (!month.Trim().Equals("0"))
            {
                month = string.Join("", new[] { month, "01 00:00:00,000" });
                fromDate = DateTime.ParseExact(month, "yyyyMMdd HH:mm:ss,fff", System.Globalization.CultureInfo.InvariantCulture);
                toDate = new DateTime(fromDate.AddMonths(1).Year, fromDate.AddMonths(1).Month, 1);
            }

            IQueryable<ExportContactInfo> query;
            if (filterIndustries.Count == 0)
            {
                query = from contact in _context.CrmContacts
                        join client in _context.CrmClients
                        on contact.CrmClientId equals client.Id
                        join industry in _context.CrmIndustries
                        on client.CrmIndustryId equals industry.Id
                        where client.IsDeleted == false && contact.IsDeleted == false && contact.LastUpdated > fromDate && contact.LastUpdated < toDate
                        select new ExportContactInfo()
                        {
                            Id = contact.Id,
                            Info = contact.Valid ? "V" : "N",
                            Company = client.Company,
                            Executive = string.Join(", ", new[] { contact.Name, contact.Salutation }),
                            Title = contact.Position,
                            Department = contact.Department,
                            Address1 = client.Address1,
                            Address2 = client.Address2,
                            Address3 = client.Address3,
                            HP = contact.Phone1,
                            HP1 = contact.Phone2,
                            HP2 = contact.Phone3,
                            Phone = client.Phone,
                            Fax = client.Fax,
                            Email1 = contact.Email1,
                            Email2 = contact.Email2,
                            Email3 = contact.Email3,
                            Email4 = contact.Email4,
                            Website = client.Website,
                            Industry = industry.Industry,
                            Remarks = client.Remarks
                        };

            }
            else
            {
                query = from contact in _context.CrmContacts
                        join client in _context.CrmClients
                        on contact.CrmClientId equals client.Id
                        join industry in _context.CrmIndustries
                        on client.CrmIndustryId equals industry.Id
                        where client.IsDeleted == false && contact.IsDeleted == false && filterIndustries.Contains(industry.Id) && contact.LastUpdated > fromDate && contact.LastUpdated < toDate
                        select new ExportContactInfo()
                        {
                            Id = contact.Id,
                            Info = contact.Valid ? "V" : "N",
                            Company = client.Company,
                            Executive = string.Join(", ", new[] { contact.Name, contact.Salutation }),
                            Title = contact.Position,
                            Department = contact.Department,
                            Address1 = client.Address1,
                            Address2 = client.Address2,
                            Address3 = client.Address3,
                            HP = contact.Phone1,
                            HP1 = contact.Phone2,
                            HP2 = contact.Phone3,
                            Phone = client.Phone,
                            Fax = client.Fax,
                            Email1 = contact.Email1,
                            Email2 = contact.Email2,
                            Email3 = contact.Email3,
                            Email4 = contact.Email4,
                            Website = client.Website,
                            Industry = industry.Industry,
                            Remarks = client.Remarks
                        };

            }

            int total = query.Count();
            int page = 1;
            int perPage = 500;
            int n = 1;
            int currentRow = 1;
            int contactCount = 0;

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Contacts");

                worksheet.Cell(currentRow, 1).Value = "INFO";
                worksheet.Cell(currentRow, 2).Value = "COMPANY";
                worksheet.Cell(currentRow, 3).Value = "EXECUTIVE";
                worksheet.Cell(currentRow, 4).Value = "TITLE";
                worksheet.Cell(currentRow, 5).Value = "Department";
                worksheet.Cell(currentRow, 6).Value = "ADDRRESS 1";
                worksheet.Cell(currentRow, 7).Value = "ADDRRESS 2";
                worksheet.Cell(currentRow, 8).Value = "ADDRRESS 3";
                worksheet.Cell(currentRow, 9).Value = "HP";
                worksheet.Cell(currentRow, 10).Value = "HP 1";
                worksheet.Cell(currentRow, 11).Value = "HP 2";
                worksheet.Cell(currentRow, 12).Value = "PHONE";
                worksheet.Cell(currentRow, 13).Value = "FAX";
                worksheet.Cell(currentRow, 14).Value = "E-MAIL 1";
                worksheet.Cell(currentRow, 15).Value = "E-MAIL 2";
                worksheet.Cell(currentRow, 16).Value = "E-MAIL 3";
                worksheet.Cell(currentRow, 17).Value = "E-MAIL 4";
                worksheet.Cell(currentRow, 18).Value = "WEBSITE";
                worksheet.Cell(currentRow, 19).Value = "INDUSTRY";
                worksheet.Cell(currentRow, 20).Value = "BIDANG USAHA";

                while(contactCount < total)
                {
                    List<ExportContactInfo> contacts = await query.Skip(perPage * (page - 1)).Take(perPage).ToListAsync<ExportContactInfo>();
                    contactCount += contacts.Count;
                    page++;
                    foreach(ExportContactInfo info in contacts)
                    {
                        currentRow++;
                        worksheet.Cell(currentRow, 1).Value = info.Info;
                        worksheet.Cell(currentRow, 2).Value = info.Company;
                        worksheet.Cell(currentRow, 3).Value = info.Executive;
                        worksheet.Cell(currentRow, 4).Value = info.Title;
                        worksheet.Cell(currentRow, 5).Value = info.Department;
                        worksheet.Cell(currentRow, 6).Value = info.Address1;
                        worksheet.Cell(currentRow, 7).Value = info.Address2;
                        worksheet.Cell(currentRow, 8).Value = info.Address3;
                        worksheet.Cell(currentRow, 9).Value = info.HP;
                        worksheet.Cell(currentRow, 10).Value = info.HP1;
                        worksheet.Cell(currentRow, 11).Value = info.HP2;
                        worksheet.Cell(currentRow, 12).Value = info.Phone;
                        worksheet.Cell(currentRow, 13).Value = info.Fax;
                        worksheet.Cell(currentRow, 14).Value = info.Email1;
                        worksheet.Cell(currentRow, 15).Value = info.Email2;
                        worksheet.Cell(currentRow, 16).Value = info.Email3;
                        worksheet.Cell(currentRow, 17).Value = info.Email4;
                        worksheet.Cell(currentRow, 18).Value = info.Website;
                        worksheet.Cell(currentRow, 19).Value = info.Industry;
                        worksheet.Cell(currentRow, 20).Value = info.Remarks;
                    }
                }


                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "users.xlsx");
                }
            }

        }

        /**
         * @api {get} /clients/contact/exportjson/{month}/{industryFilter} Export client contacts JSON
         * @apiVersion 1.0.0
         * @apiName ExportJsonClientContactList
         * @apiGroup Clients
         * @apiPermission ApiUser
         * 
         * @apiParam {String} month             Filter untuk bulan, dalam format YYYYMM, misal 202005 untuk bulan Mei 2020. Gunakan 0 untuk tidak menggunakan filter bulan.
         * @apiParam {String} industryFilter    0 untuk tidak menggunakan filter, atau comma-separated values dari industryId, misal 1,3.
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "headers": [
         *           "INFO",
         *           "COMPANY",
         *           "EXECUTIVE",
         *           "TITLE",
         *           "Department",
         *           "ADDRRESS 1",
         *           "ADDRRESS 2",
         *           "ADDRRESS 3",
         *           "HP",
         *           "HP 1",
         *           "HP 2",
         *           "PHONE",
         *           "FAX",
         *           "E-MAIL 1",
         *           "E-MAIL 2",
         *           "E-MAIL 3",
         *           "E-MAIL 4",
         *           "WEBSITE",
         *           "INDUSTRY",
         *           "BIDANG USAHA"
         *       ],
         *       "items": [
         *           {
         *               "id": 10908,
         *               "info": "V",
         *               "company": "BUMI TEKNOKULTURA UNGGUL Tbk, PT",
         *               "executive": "Argo Nugroho, ",
         *               "title": "Corporate Secretary",
         *               "department": "Humas",
         *               "address1": "Komplek Permata Senayan Rukan Blok E No. 37-38 ",
         *               "address2": "Jl. Tentara Pelajar",
         *               "address3": "Jakarta Selatan 12210",
         *               "hp": "",
         *               "hP1": "",
         *               "hP2": "",
         *               "phone": "021 57940929 (Hunting) / 5300700",
         *               "fax": "021 57940930/021-57941330",
         *               "email1": "corporate@btek.co.id",
         *               "email2": "b_teknokutura@yahoo.com",
         *               "email3": "",
         *               "email4": "",
         *               "website": "",
         *               "industry": "Agriculture",
         *               "remarks": "Agriculture Plantation"
         *           },
         *           {
         *               "id": 11167,
         *               "info": "V",
         *               "company": "CAKRA MINERAL Tbk, PT",
         *               "executive": "Dexter Sjarif Putra, ",
         *               "title": "Corporate Secretary",
         *               "department": "Humas",
         *               "address1": "Komplek Perkantoran RedTop E 7-9, Kebon Kelapa",
         *               "address2": "Jl. Raya Pecenongan No. 72 ",
         *               "address3": "Jakarta Pusat 10120",
         *               "hp": "",
         *               "hP1": "",
         *               "hP2": "",
         *               "phone": "021 3519380/ 57852220",
         *               "fax": "021 3453704",
         *               "email1": "corporate.secretary@ckra.co.id",
         *               "email2": "dexter@ckra.co.id",
         *               "email3": "",
         *               "email4": "",
         *               "website": "www.ckra.co.id",
         *               "industry": "Agriculture",
         *               "remarks": "sektor perdangangan, pengangkutan, pembangunan, perindustrian, jasa dan pertanian"
         *           }
         *      ]
         *   }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("contact/exportjson/{month}/{industryFilter}")]
        public async Task<ActionResult<ExportJsonResponse>> ExportJsonClientContactList(string month, string industryFilter)
        {
            List<int> filterIndustries = SplitString(industryFilter);
            if (filterIndustries == null)
            {
                return BadRequest(new { error = "Invalid industry filter" });
            }

            DateTime fromDate = new DateTime(1900, 1, 1);
            DateTime toDate = new DateTime(2100, 12, 31);

            if (!month.Trim().Equals("0"))
            {
                month = string.Join("", new[] { month, "01 00:00:00,000" });
                fromDate = DateTime.ParseExact(month, "yyyyMMdd HH:mm:ss,fff", System.Globalization.CultureInfo.InvariantCulture);
                toDate = new DateTime(fromDate.AddMonths(1).Year, fromDate.AddMonths(1).Month, 1);
            }

            IQueryable<ExportContactInfo> query;
            if (filterIndustries.Count == 0)
            {
                query = from contact in _context.CrmContacts
                        join client in _context.CrmClients
                        on contact.CrmClientId equals client.Id
                        join industry in _context.CrmIndustries
                        on client.CrmIndustryId equals industry.Id
                        where client.IsDeleted == false && contact.IsDeleted == false && contact.LastUpdated > fromDate && contact.LastUpdated < toDate
                        select new ExportContactInfo()
                        {
                            Id = contact.Id,
                            Info = contact.Valid ? "V" : "N",
                            Company = client.Company,
                            Executive = string.Join(", ", new[] { contact.Name, contact.Salutation }),
                            Title = contact.Position,
                            Department = contact.Department,
                            Address1 = client.Address1,
                            Address2 = client.Address2,
                            Address3 = client.Address3,
                            HP = contact.Phone1,
                            HP1 = contact.Phone2,
                            HP2 = contact.Phone3,
                            Phone = client.Phone,
                            Fax = client.Fax,
                            Email1 = contact.Email1,
                            Email2 = contact.Email2,
                            Email3 = contact.Email3,
                            Email4 = contact.Email4,
                            Website = client.Website,
                            Industry = industry.Industry,
                            Remarks = client.Remarks
                        };

            }
            else
            {
                query = from contact in _context.CrmContacts
                        join client in _context.CrmClients
                        on contact.CrmClientId equals client.Id
                        join industry in _context.CrmIndustries
                        on client.CrmIndustryId equals industry.Id
                        where client.IsDeleted == false && contact.IsDeleted == false && filterIndustries.Contains(industry.Id) && contact.LastUpdated > fromDate && contact.LastUpdated < toDate
                        select new ExportContactInfo()
                        {
                            Id = contact.Id,
                            Info = contact.Valid ? "V" : "N",
                            Company = client.Company,
                            Executive = string.Join(", ", new[] { contact.Name, contact.Salutation }),
                            Title = contact.Position,
                            Department = contact.Department,
                            Address1 = client.Address1,
                            Address2 = client.Address2,
                            Address3 = client.Address3,
                            HP = contact.Phone1,
                            HP1 = contact.Phone2,
                            HP2 = contact.Phone3,
                            Phone = client.Phone,
                            Fax = client.Fax,
                            Email1 = contact.Email1,
                            Email2 = contact.Email2,
                            Email3 = contact.Email3,
                            Email4 = contact.Email4,
                            Website = client.Website,
                            Industry = industry.Industry,
                            Remarks = client.Remarks
                        };

            }

            int total = query.Count();
            int page = 1;
            int perPage = 500;
            int contactCount = 0;

            ExportJsonResponse response = new ExportJsonResponse();
            response.Headers = new List<string>();
            response.Headers.AddRange(new[] {"INFO","COMPANY","EXECUTIVE","TITLE","Department","ADDRRESS 1","ADDRRESS 2","ADDRRESS 3",
                "HP","HP 1","HP 2","PHONE","FAX","E-MAIL 1","E-MAIL 2","E-MAIL 3","E-MAIL 4","WEBSITE","INDUSTRY","BIDANG USAHA" });

            response.Items = new List<ExportContactInfo>();

            while (contactCount < total)
            {
                List<ExportContactInfo> contacts = await query.Skip(perPage * (page - 1)).Take(perPage).ToListAsync<ExportContactInfo>();
                contactCount += contacts.Count;
                page++;
                response.Items.AddRange(contacts);
            }

            return response;
        }

        // PUT: v1/clients/5
        /**
         * @api {put} /clients/{clientId} Edit data client
         * @apiVersion 1.0.0
         * @apiName PutCrmClient
         * @apiGroup Clients
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} clientId        Id dari client yang bersangkutan.
         * @apiParamExample {json} Request-Example:
         *   {
         *     "id": 3,
         *     "company": "Back Central Asia",
         *     "address1": "Menara BCA",
         *     "address2": "Jl. Jend. Sudirman",
         *     "address3": "Jakarta",
         *     "phone": "021-909089",
         *     "fax": "021-8908290",
         *     "website": "www.klikbca.com",
         *     "remarks": "",
         *     "industryId": 8,
         *     "userId": 3,
         *     "relManagerIds": [1, 3],
         *     "contacts": [
         *       {
         *         "id": 9,
         *         "name": "Bandi",
         *         "salutation": "Mr.",
         *         "email": "bandi@gmail.com",
         *         "phone": "0819-900872",
         *         "department": "Marketing",
         *         "position": "Manager",
         *         "valid": true
         *       }
         *     ]
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   {
         *     "code": "0",
         *     "description": ""
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         * @apiError BadRequest Client Id yang ada di URL berbeda dengan yang ada di body.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPut("{id}")]
        public async Task<ActionResult<Error>> PutCrmClient(int id, EditClientRequest client)
        {
            if (id != client.Id)
            {
                return BadRequest();
            }

            DateTime now = DateTime.Now;

            try
            {
                List<CrmClientRelManager> currentRMs = _context.CrmClientRelManagers.Where(a => a.CrmClientId == id).ToList<CrmClientRelManager>();
                if(currentRMs != null)
                {
                    foreach (int n in client.RelManagerIds)
                    {
                        if (!isRelManagerIdInRMList(n, currentRMs))
                        {
                            CrmClientRelManager RelManager = new CrmClientRelManager()
                            {
                                CrmRelManagerId = n,
                                CrmClientId = client.Id,
                            };
                            _context.CrmClientRelManagers.Add(RelManager);
                        }
                    }
                    foreach (CrmClientRelManager crm in currentRMs)
                    {
                        if (!client.RelManagerIds.Contains(crm.CrmRelManagerId))
                        {
                            _context.CrmClientRelManagers.Remove(crm);
                        }
                    }
                }
            }
            catch
            {
                return new Error("get_current_rm", "Error in getting current RMs.");
            }
            
            try
            {
                List<CrmContact> currentContacts = _context.CrmContacts.Where(a => a.CrmClientId == id && a.IsDeleted == false).ToList<CrmContact>();
                foreach (ContactInfo info in client.contacts)
                {
                    if (!isContactInfoInContact(info, currentContacts))
                    {
                        CrmContact crmContact = new CrmContact();
                        crmContact.Name = info.Name;

                        string[] emails = info.Email.Split(',');
                        if (emails.Length > 4)
                        {
                            return new Error("email", "Maximum number of emails is 4.");
                        }
                        switch (emails.Length)
                        {
                            case 0:
                                crmContact.Email1 = "";
                                crmContact.Email2 = "";
                                crmContact.Email3 = "";
                                crmContact.Email4 = "";
                                break;
                            case 1:
                                crmContact.Email1 = emails[0].Trim();
                                crmContact.Email2 = "";
                                crmContact.Email3 = "";
                                crmContact.Email4 = "";
                                break;
                            case 2:
                                crmContact.Email1 = emails[0].Trim();
                                crmContact.Email2 = emails[1].Trim();
                                crmContact.Email3 = "";
                                crmContact.Email4 = "";
                                break;
                            case 3:
                                crmContact.Email1 = emails[0].Trim();
                                crmContact.Email2 = emails[1].Trim();
                                crmContact.Email3 = emails[2].Trim();
                                crmContact.Email4 = "";
                                break;
                            case 4:
                                crmContact.Email1 = emails[0].Trim();
                                crmContact.Email2 = emails[1].Trim();
                                crmContact.Email3 = emails[2].Trim();
                                crmContact.Email4 = emails[3].Trim();
                                break;
                        }

                        string[] phones = info.Phone.Split(',');
                        if (phones.Length > 3)
                        {
                            return new Error("phone", "Maximum number of phones is 3.");
                        }
                        switch (phones.Length)
                        {
                            case 0:
                                crmContact.Phone1 = "";
                                crmContact.Phone2 = "";
                                crmContact.Phone3 = "";
                                break;
                            case 1:
                                crmContact.Phone1 = phones[0].Trim();
                                crmContact.Phone2 = "";
                                crmContact.Phone3 = "";
                                break;
                            case 2:
                                crmContact.Phone1 = phones[0].Trim();
                                crmContact.Phone2 = phones[1].Trim();
                                crmContact.Phone3 = "";
                                break;
                            case 3:
                                crmContact.Phone1 = phones[0].Trim();
                                crmContact.Phone2 = phones[1].Trim();
                                crmContact.Phone3 = phones[2].Trim();
                                break;
                        }
                        crmContact.Salutation = info.Salutation == "-" ? "" : info.Salutation;
                        crmContact.Department = info.Department;
                        crmContact.Position = info.Position;
                        crmContact.Valid = info.Valid;
                        crmContact.Source = "Sales";
                        crmContact.CrmClientId = client.Id;
                        crmContact.CreatedDate = now;
                        crmContact.CreatedBy = client.UserId;
                        crmContact.LastUpdated = now;
                        crmContact.LastUpdatedBy = client.UserId;
                        crmContact.IsDeleted = false;
                        crmContact.DeletedBy = 0;
                        
                       _context.CrmContacts.Add(crmContact);
                    }
                    else
                    {
                        CrmContact crmContact = await _context.CrmContacts.FindAsync(info.Id);
                        if(crmContact != null)
                        {
                            crmContact.Name = info.Name;

                            string[] emails = info.Email.Split(',');
                            if (emails.Length > 4)
                            {
                                return new Error("email", "Maximum number of emails is 4.");
                            }
                            switch (emails.Length)
                            {
                                case 0:
                                    crmContact.Email1 = "";
                                    crmContact.Email2 = "";
                                    crmContact.Email3 = "";
                                    crmContact.Email4 = "";
                                    break;
                                case 1:
                                    crmContact.Email1 = emails[0].Trim();
                                    crmContact.Email2 = "";
                                    crmContact.Email3 = "";
                                    crmContact.Email4 = "";
                                    break;
                                case 2:
                                    crmContact.Email1 = emails[0].Trim();
                                    crmContact.Email2 = emails[1].Trim();
                                    crmContact.Email3 = "";
                                    crmContact.Email4 = "";
                                    break;
                                case 3:
                                    crmContact.Email1 = emails[0].Trim();
                                    crmContact.Email2 = emails[1].Trim();
                                    crmContact.Email3 = emails[2].Trim();
                                    crmContact.Email4 = "";
                                    break;
                                case 4:
                                    crmContact.Email1 = emails[0].Trim();
                                    crmContact.Email2 = emails[1].Trim();
                                    crmContact.Email3 = emails[2].Trim();
                                    crmContact.Email4 = emails[3].Trim();
                                    break;
                            }

                            string[] phones = info.Phone.Split(',');
                            if (phones.Length > 3)
                            {
                                return new Error("phone", "Maximum number of phones is 3.");
                            }
                            switch (phones.Length)
                            {
                                case 0:
                                    crmContact.Phone1 = "";
                                    crmContact.Phone2 = "";
                                    crmContact.Phone3 = "";
                                    break;
                                case 1:
                                    crmContact.Phone1 = phones[0].Trim();
                                    crmContact.Phone2 = "";
                                    crmContact.Phone3 = "";
                                    break;
                                case 2:
                                    crmContact.Phone1 = phones[0].Trim();
                                    crmContact.Phone2 = phones[1].Trim();
                                    crmContact.Phone3 = "";
                                    break;
                                case 3:
                                    crmContact.Phone1 = phones[0].Trim();
                                    crmContact.Phone2 = phones[1].Trim();
                                    crmContact.Phone3 = phones[2].Trim();
                                    break;
                            }
                            crmContact.Salutation = info.Salutation == "-" ? "" : info.Salutation;
                            crmContact.Department = info.Department;
                            crmContact.Position = info.Position;
                            crmContact.Valid = info.Valid;
                            crmContact.Source = "Sales";
                            crmContact.CrmClientId = client.Id;
                            crmContact.LastUpdated = now;
                            crmContact.LastUpdatedBy = client.UserId;
                            crmContact.IsDeleted = false;
                            crmContact.DeletedBy = 0;
                            _context.Entry(crmContact).State = EntityState.Modified;
                        }
                    }

                }
                foreach (CrmContact cc in currentContacts)
                {
                    if (!isIdInContactInfoList(cc.Id, client.contacts))
                    {
                        cc.IsDeleted = true;
                        cc.DeletedBy = client.UserId;
                        cc.DeletedDate = now;
                    }
                }
            }
            catch
            {
                return new Error("get_current_contacts", "Error in getting current contacts.");
            }

            CrmClient crmClient = new CrmClient()
            {
                Id = client.Id,
                Company = client.Company,
                Address1 = client.Address1,
                Address2 = client.Address2,
                Address3 = client.Address3,
                Phone = client.Phone,
                Fax = client.Fax,
                Website = client.Website,
                Remarks = client.Remarks,
                Source = "Sales",
                CrmIndustryId = client.IndustryId,
                LastUpdated = now,
                LastUpdatedBy = client.UserId
            };
            _context.Entry(crmClient).State = EntityState.Modified;


            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CrmClientExists(id))
                {
                    return NotFound();
                }
                else
                {
                    return new Error("db_error", "Error in updating database.");
                }
            }

            return new Error("0", "");
        }

        /**
         * @api {put} /clients/contact/{id}/{clientId}/{userId} Edit data contact
         * @apiVersion 1.0.0
         * @apiName PutCrmClientContact
         * @apiGroup Clients
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} Id        Id dari contact yang bersangkutan.
         * @apiParam {Number} clientId  Id dari client.
         * @apiParam {Number} userId    Id dari user yang login.
         * @apiParamExample {json} Request-Example:
         *   {
         *         "id": 9,
         *         "name": "Bandi",
         *         "salutation": "Mr.",
         *         "email": "bandi@gmail.com",
         *         "phone": "0819-900872",
         *         "department": "Marketing",
         *         "position": "Manager",
         *         "valid": true
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   {
         *     "code": "0",
         *     "description": ""
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         * @apiError BadRequest Contact Id yang ada di URL berbeda dengan yang ada di body.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPut("contact/{id}/{clientId}/{userId}")]
        public async Task<ActionResult<Error>> PutCrmClientContact(int id, int clientId, int userId, ContactInfo info)
        {
            if (id != info.Id)
            {
                return BadRequest();
            }

            if(!CrmClientExists(clientId))
            {
                return NotFound(new { error = "clientId salah" });
            }

            CrmContact crmContact = await _context.CrmContacts.FindAsync(info.Id);


            if (crmContact != null)
            {
                DateTime now = DateTime.Now;

                crmContact.Name = info.Name;

                string[] emails = info.Email.Split(',');
                if (emails.Length > 4)
                {
                    return new Error("email", "Maximum number of emails is 4.");
                }
                switch (emails.Length)
                {
                    case 0:
                        crmContact.Email1 = "";
                        crmContact.Email2 = "";
                        crmContact.Email3 = "";
                        crmContact.Email4 = "";
                        break;
                    case 1:
                        crmContact.Email1 = emails[0].Trim();
                        crmContact.Email2 = "";
                        crmContact.Email3 = "";
                        crmContact.Email4 = "";
                        break;
                    case 2:
                        crmContact.Email1 = emails[0].Trim();
                        crmContact.Email2 = emails[1].Trim();
                        crmContact.Email3 = "";
                        crmContact.Email4 = "";
                        break;
                    case 3:
                        crmContact.Email1 = emails[0].Trim();
                        crmContact.Email2 = emails[1].Trim();
                        crmContact.Email3 = emails[2].Trim();
                        crmContact.Email4 = "";
                        break;
                    case 4:
                        crmContact.Email1 = emails[0].Trim();
                        crmContact.Email2 = emails[1].Trim();
                        crmContact.Email3 = emails[2].Trim();
                        crmContact.Email4 = emails[3].Trim();
                        break;
                }

                string[] phones = info.Phone.Split(',');
                if (phones.Length > 3)
                {
                    return new Error("phone", "Maximum number of phones is 3.");
                }
                switch (phones.Length)
                {
                    case 0:
                        crmContact.Phone1 = "";
                        crmContact.Phone2 = "";
                        crmContact.Phone3 = "";
                        break;
                    case 1:
                        crmContact.Phone1 = phones[0].Trim();
                        crmContact.Phone2 = "";
                        crmContact.Phone3 = "";
                        break;
                    case 2:
                        crmContact.Phone1 = phones[0].Trim();
                        crmContact.Phone2 = phones[1].Trim();
                        crmContact.Phone3 = "";
                        break;
                    case 3:
                        crmContact.Phone1 = phones[0].Trim();
                        crmContact.Phone2 = phones[1].Trim();
                        crmContact.Phone3 = phones[2].Trim();
                        break;
                }
                crmContact.Salutation = info.Salutation == "-" ? "" : info.Salutation;
                crmContact.Department = info.Department;
                crmContact.Position = info.Position;
                crmContact.Valid = info.Valid;
                crmContact.Source = "Sales";
                crmContact.CrmClientId = clientId;
                crmContact.LastUpdated = now;
                crmContact.LastUpdatedBy = userId;
                crmContact.IsDeleted = false;
                crmContact.DeletedBy = 0;
                _context.Entry(crmContact).State = EntityState.Modified;
                await _context.SaveChangesAsync();

            }
            else
            {
                return NotFound();
            }
            return new Error("ok", "");
        }

        // POST: v1/clients
        /**
         * @api {post} /clients Tambah client
         * @apiVersion 1.0.0
         * @apiName PostCrmClient
         * @apiGroup Clients
         * @apiPermission ApiUser
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "company": "Bank Ekonomi",
         *     "address1": "Gedung Ekonomi",
         *     "address2": "Jl. Gator Subroto",
         *     "address3": "Jakarta",
         *     "phone": "021-99289",
         *     "fax": "021-928290",
         *     "website": null,
         *     "remarks": null,
         *     "industryId": 8,
         *     "relManagerIds": [1],
         *     "contacts": [
         *       {
         *         "id": 0,
         *         "name": "Eko Paseko",
         *         "salutation": "-",
         *         "email": "eko@bankekonomi.co.id",
         *         "phone": "0918-0909289",
         *         "department": "Finance",
         *         "position": "Director",
         *         "valid": true
         *       }
         *     ],
         *     "userId": 1
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   {
         *     "client": {
         *       "id": 27,
         *       "company": "Bank Ekonomi",
         *       "address1": "Gedung Ekonomi",
         *       "address2": "Jl. Gator Subroto",
         *       "address3": "Jakarta",
         *       "phone": "021-99289",
         *       "fax": "021-928290",
         *       "website": null,
         *       "remarks": null,
         *       "crmIndustryId": 8,
         *       "createdDate": "2020-02-13T15:17:26.8573821+07:00",
         *       "createdBy": 1,
         *       "lastUpdated": "2020-02-13T15:17:26.8573821+07:00",
         *       "lastUpdatedBy": 1,
         *       "isDeleted": false,
         *       "deletedBy": 0,
         *       "deletedDate": "0001-01-01T00:00:00"
         *     },
         *     "industry": {
         *       "id": 8,
         *       "industry": "Financial Services - Bank"
         *     },
         *     "contacts": [
         *       {
         *         "id": 17,
         *         "name": "Eko Paseko",
         *         "salutation": "",
         *         "email": "eko@bankekonomi.co.id",
         *         "phone": "0918-0909289",
         *         "department": "Finance",
         *         "position": "Director",
         *         "valid": false
         *       }
         *     ],
         *     "relManagers": [
         *       {
         *         "id": 5,
         *         "relManagerId": 1,
         *         "name": "Leviana Wijaya",
         *         "email": "levi@gmlperformannce.co.id",
         *         "segment": "Private",
         *         "branch": "Jakarta"
         *       }
         *     ],
         *     "errors": [
         *       {
         *         "code": "0",
         *         "description": ""
         *       }
         *     ]
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost]
        public async Task<ActionResult<GetClientResponse>> PostCrmClient(AddClientRequest request)
        {
            DateTime now = DateTime.Now;

            CrmClient crmClient = new CrmClient()
            {
                Company = request.Company,
                Address1 = request.Address1,
                Address2 = request.Address2,
                Address3 = request.Address3,
                Phone = request.Phone,
                Fax = request.Fax,
                Website = request.Website,
                Remarks = request.Remarks,
                Source = "Sales",
                CrmIndustryId = request.IndustryId,
                CreatedDate = now,
                CreatedBy = request.UserId,
                LastUpdated = now,
                LastUpdatedBy = request.UserId,
                IsDeleted = false,
                DeletedBy = 0
            };
            _context.CrmClients.Add(crmClient);
            await _context.SaveChangesAsync();

            //var r = CreatedAtAction("GetCrmClient", new { id = crmClient.Id }, crmClient);

            foreach(var rmid in request.RelManagerIds)
            {
                CrmClientRelManager RelManager = new CrmClientRelManager()
                {
                    CrmRelManagerId = rmid,
                    CrmClientId = crmClient.Id,
                };
                _context.CrmClientRelManagers.Add(RelManager);
            }

            foreach(ContactInfo contact in request.contacts)
            {
                CrmContact crmContact = new CrmContact();
                crmContact.Name = contact.Name;

                string[] emails = contact.Email.Split(',');
                if (emails.Length > 4)
                {
                    return new GetClientResponse(new[] { new Error("email", "Maximum number of emails is 4.") });
                }
                switch (emails.Length)
                {
                    case 0:
                        crmContact.Email1 = "";
                        crmContact.Email2 = "";
                        crmContact.Email3 = "";
                        crmContact.Email4 = "";
                        break;
                    case 1:
                        crmContact.Email1 = emails[0].Trim();
                        crmContact.Email2 = "";
                        crmContact.Email3 = "";
                        crmContact.Email4 = "";
                        break;
                    case 2:
                        crmContact.Email1 = emails[0].Trim();
                        crmContact.Email2 = emails[1].Trim();
                        crmContact.Email3 = "";
                        crmContact.Email4 = "";
                        break;
                    case 3:
                        crmContact.Email1 = emails[0].Trim();
                        crmContact.Email2 = emails[1].Trim();
                        crmContact.Email3 = emails[2].Trim();
                        crmContact.Email4 = "";
                        break;
                    case 4:
                        crmContact.Email1 = emails[0].Trim();
                        crmContact.Email2 = emails[1].Trim();
                        crmContact.Email3 = emails[2].Trim();
                        crmContact.Email4 = emails[3].Trim();
                        break;
                }

                string[] phones = contact.Phone.Split(',');
                if (phones.Length > 3)
                {
                    return new GetClientResponse(new[] { new Error("phone", "Maximum number of phones is 3.") });
                }
                switch (phones.Length)
                {
                    case 0:
                        crmContact.Phone1 = "";
                        crmContact.Phone2 = "";
                        crmContact.Phone3 = "";
                        break;
                    case 1:
                        crmContact.Phone1 = phones[0].Trim();
                        crmContact.Phone2 = "";
                        crmContact.Phone3 = "";
                        break;
                    case 2:
                        crmContact.Phone1 = phones[0].Trim();
                        crmContact.Phone2 = phones[1].Trim();
                        crmContact.Phone3 = "";
                        break;
                    case 3:
                        crmContact.Phone1 = phones[0].Trim();
                        crmContact.Phone2 = phones[1].Trim();
                        crmContact.Phone3 = phones[2].Trim();
                        break;
                }
                crmContact.Salutation = contact.Salutation == "-" ? "" : contact.Salutation;
                crmContact.Department = contact.Department;
                crmContact.Position = contact.Position;
                crmContact.Valid = contact.Valid;
                crmContact.Source = "Sales";
                crmContact.CrmClientId = crmClient.Id;
                crmContact.CreatedDate = now;
                crmContact.CreatedBy = request.UserId;
                crmContact.LastUpdated = now;
                crmContact.LastUpdatedBy = request.UserId;
                crmContact.IsDeleted = false;
                crmContact.DeletedBy = 0;

                _context.CrmContacts.Add(crmContact);
            }
            await _context.SaveChangesAsync();

            var response = new GetClientResponse();
            response.Client = crmClient;
            response.Industry = getClientIndustryInfo(crmClient.CrmIndustryId);
            response.Contacts = getClientContacts(crmClient.Id);
            response.RelManagers = getClientRelManagers(crmClient.Id);

            return response;
        }

        // DELETE: v1/clients/5/1
        /**
         * @api {delete} /clients/{clientId}/{userId} Hapus client
         * @apiVersion 1.0.0
         * @apiName PostCrmClient
         * @apiGroup Clients
         * @apiPermission CanDeleteClient
         * 
         * @apiParam {Number} clientId  Id dari client yang ingin dihapus
         * @apiParam {Number} userId    Id dari user yang sedang login
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *     "id": 3,
         *     "company": "Back Central Asia",
         *     "address1": "Menara BCA",
         *     "address2": "Jl. Jend. Sudirman",
         *     "address3": "Jakarta",
         *     "phone": "021-909089",
         *     "fax": "021-8908290",
         *     "website": "www.klikbca.com",
         *     "remarks": "",
         *     "crmIndustryId": 8,
         *     "createdDate": "0001-01-01T00:00:00",
         *     "createdBy": 0,
         *     "lastUpdated": "2020-02-13T15:05:21.7242535",
         *     "lastUpdatedBy": 3,
         *     "isDeleted": true,
         *     "deletedBy": 1,
         *     "deletedDate": "2020-02-13T15:21:29.8798965+07:00"
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         * @apiError Notfound Client Id salah.
         */
        [Authorize(Policy = "ApiUser")]         // CanDeleteClient. Diturunkan dulu jadi ApiUser
        [HttpDelete("{id}/{userId}")]
        public async Task<ActionResult<CrmClient>> DeleteCrmClient(int id, int userId)
        {
            var crmClient = await _context.CrmClients.FindAsync(id);
            if (crmClient == null)
            {
                return NotFound();
            }

            DateTime now = DateTime.Now;

            crmClient.IsDeleted = true;
            crmClient.DeletedBy = userId;
            crmClient.DeletedDate = now;
            _context.Entry(crmClient).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return crmClient;
        }

        // DELETE: v1/rm/clients/5/1
        /**
         * @api {delete} /clients/rm/{clientId}/{relManagerId}/{userId} Hapus RM untuk client tertentu
         * @apiVersion 1.0.0
         * @apiName DeleteCrmClientRM
         * @apiGroup Clients
         * @apiPermission CanDeleteClient
         * 
         * @apiParam {Number} clientId      Id dari client yang ingin dihapus
         * @apiParam {Number} relManagerId  Id dari RM yang ingin dihapus 
         * @apiParam {Number} userId        Id dari user yang sedang login
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *     "code": "ok",
         *     "description": "Deletion successful"
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         * @apiError Notfound Client Id atau RM Id salah.
         */
        [Authorize(Policy = "CanDeleteClient")]
        [HttpDelete("rm/{clientId}/{relManagerId}/{userId}")]
        public async Task<ActionResult<Error>> DeleteCrmClientRM(int clientId, int relManagerId, int userId)
        {
            var crmClientRM = await _context.CrmClientRelManagers.Where(a => a.CrmClientId == clientId && a.CrmRelManagerId == relManagerId).FirstAsync();
            if (crmClientRM == null)
            {
                return NotFound();
            }

            _context.CrmClientRelManagers.Remove(crmClientRM);
            await _context.SaveChangesAsync();

            return new Error("ok", "Deletion successful");

        }

        // DELETE: v1/clients/5/1
        /**
         * @api {delete} /clients/rm/{clientId}/{contactId}/{userId} Hapus Contact 
         * @apiVersion 1.0.0
         * @apiName DeleteCrmClientContact
         * @apiGroup Clients
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} clientId      Id dari client yang ingin dihapus
         * @apiParam {Number} contactId     Id dari Contact yang ingin dihapus 
         * @apiParam {Number} userId        Id dari user yang sedang login
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *     "code": "ok",
         *     "description": "Deletion successful"
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         * @apiError Notfound Client Id atau Contact Id salah.
         */
        [Authorize(Policy = "ApiUser")]             // CanDeleteClient. Diturunkan dulu jadi ApiUser
        [HttpDelete("contact/{clientId}/{contactId}/{userId}")]
        public async Task<ActionResult<Error>> DeleteCrmClientContact(int clientId, int contactId, int userId)
        {
            var crmContact = await _context.CrmContacts.FindAsync(contactId);
            if (crmContact == null)
            {
                return NotFound();
            }

            DateTime now = DateTime.Now;

            crmContact.IsDeleted = true;
            crmContact.DeletedBy = userId;
            crmContact.DeletedDate = now;
            _context.Entry(crmContact).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return new Error("ok", "Deletion successful");
        }

        private bool CrmClientExists(int id)
        {
            return _context.CrmClients.Any(e => e.Id == id);
        }

        private IQueryable<ClientInfo> BuildQuery(ClientListRequest request)
        {
            IQueryable<ClientInfo> query;
            if(request.search.Equals("*"))
            {
                if (request.filterIndustries.Count == 0)
                {
                    if (request.filterSegments.Count == 0)
                    {
                        if (request.filterRelManagers.Count == 0)
                        {
                            query = from client in _context.CrmClients
                                    join industry in _context.CrmIndustries
                                    on client.CrmIndustryId equals industry.Id
                                    where client.IsDeleted == false
                                    select new ClientInfo()
                                    {
                                        Id = client.Id,
                                        Company = client.Company,
                                        Industry = industry.Industry,
                                        Address = string.Join("\r\n", new[] { client.Address1, client.Address2, client.Address3 }),
                                        phone = client.Phone
                                    };
                        }
                        else
                        {
                            query = from client in _context.CrmClients
                                    join industry in _context.CrmIndustries
                                    on client.CrmIndustryId equals industry.Id
                                    join clientrm in _context.CrmClientRelManagers
                                    on client.Id equals clientrm.CrmClientId
                                    join rm in _context.CrmRelManagers
                                    on clientrm.CrmRelManagerId equals rm.Id
                                    join user in _context.Users
                                    on rm.UserId equals user.ID
                                    where client.IsDeleted == false && request.filterRelManagers.Contains(user.ID)
                                    select new ClientInfo()
                                    {
                                        Id = client.Id,
                                        Company = client.Company,
                                        Industry = industry.Industry,
                                        Address = string.Join("\r\n", new[] { client.Address1, client.Address2, client.Address3 }),
                                        phone = client.Phone
                                    };
                        }
                    }
                    else
                    {
                        query = from client in _context.CrmClients
                                join industry in _context.CrmIndustries
                                on client.CrmIndustryId equals industry.Id
                                join clientrm in _context.CrmClientRelManagers
                                on client.Id equals clientrm.CrmClientId
                                join rm in _context.CrmRelManagers
                                on clientrm.CrmRelManagerId equals rm.Id
                                where client.IsDeleted == false && request.filterSegments.Contains(rm.SegmentId)
                                select new ClientInfo()
                                {
                                    Id = client.Id,
                                    Company = client.Company,
                                    Industry = industry.Industry,
                                    Address = string.Join("\r\n", new[] { client.Address1, client.Address2, client.Address3 }),
                                    phone = client.Phone
                                };
                    }
                }
                else
                {
                    if (request.filterSegments.Count == 0)
                    {
                        if (request.filterRelManagers.Count == 0)
                        {
                            query = from client in _context.CrmClients
                                    join industry in _context.CrmIndustries
                                    on client.CrmIndustryId equals industry.Id
                                    where client.IsDeleted == false && request.filterIndustries.Contains(industry.Id)
                                    select new ClientInfo()
                                    {
                                        Id = client.Id,
                                        Company = client.Company,
                                        Industry = industry.Industry,
                                        Address = string.Join("\r\n", new[] { client.Address1, client.Address2, client.Address3 }),
                                        phone = client.Phone
                                    };
                        }
                        else
                        {
                            query = from client in _context.CrmClients
                                    join industry in _context.CrmIndustries
                                    on client.CrmIndustryId equals industry.Id
                                    join clientrm in _context.CrmClientRelManagers
                                    on client.Id equals clientrm.CrmClientId
                                    join rm in _context.CrmRelManagers
                                    on clientrm.CrmRelManagerId equals rm.Id
                                    join user in _context.Users
                                    on rm.UserId equals user.ID
                                    where client.IsDeleted == false && request.filterRelManagers.Contains(user.ID) && request.filterIndustries.Contains(industry.Id)
                                    select new ClientInfo()
                                    {
                                        Id = client.Id,
                                        Company = client.Company,
                                        Industry = industry.Industry,
                                        Address = string.Join("\r\n", new[] { client.Address1, client.Address2, client.Address3 }),
                                        phone = client.Phone
                                    };
                        }
                    }
                    else
                    {
                        query = from client in _context.CrmClients
                                join industry in _context.CrmIndustries
                                on client.CrmIndustryId equals industry.Id
                                join clientrm in _context.CrmClientRelManagers
                                on client.Id equals clientrm.CrmClientId
                                join rm in _context.CrmRelManagers
                                on clientrm.CrmRelManagerId equals rm.Id
                                where client.IsDeleted == false && request.filterSegments.Contains(rm.SegmentId) && request.filterIndustries.Contains(industry.Id)
                                select new ClientInfo()
                                {
                                    Id = client.Id,
                                    Company = client.Company,
                                    Industry = industry.Industry,
                                    Address = string.Join("\r\n", new[] { client.Address1, client.Address2, client.Address3 }),
                                    phone = client.Phone
                                };
                    }
                }

            }
            else
            {
                if (request.filterIndustries.Count == 0)
                {
                    if (request.filterSegments.Count == 0)
                    {
                        if (request.filterRelManagers.Count == 0)
                        {
                            query = from client in _context.CrmClients
                                    join industry in _context.CrmIndustries
                                    on client.CrmIndustryId equals industry.Id
                                    where client.IsDeleted == false &&
                                    client.Company.Contains(request.search)
                                    select new ClientInfo()
                                    {
                                        Id = client.Id,
                                        Company = client.Company,
                                        Industry = industry.Industry,
                                        Address = string.Join("\r\n", new[] { client.Address1, client.Address2, client.Address3 }),
                                        phone = client.Phone
                                    };
                        }
                        else
                        {
                            query = from client in _context.CrmClients
                                    join industry in _context.CrmIndustries
                                    on client.CrmIndustryId equals industry.Id
                                    join clientrm in _context.CrmClientRelManagers
                                    on client.Id equals clientrm.CrmClientId
                                    join rm in _context.CrmRelManagers
                                    on clientrm.CrmRelManagerId equals rm.Id
                                    join user in _context.Users
                                    on rm.UserId equals user.ID
                                    where client.IsDeleted == false && request.filterRelManagers.Contains(user.ID) &&
                                    client.Company.Contains(request.search)
                                    select new ClientInfo()
                                    {
                                        Id = client.Id,
                                        Company = client.Company,
                                        Industry = industry.Industry,
                                        Address = string.Join("\r\n", new[] { client.Address1, client.Address2, client.Address3 }),
                                        phone = client.Phone
                                    };
                        }
                    }
                    else
                    {
                        query = from client in _context.CrmClients
                                join industry in _context.CrmIndustries
                                on client.CrmIndustryId equals industry.Id
                                join clientrm in _context.CrmClientRelManagers
                                on client.Id equals clientrm.CrmClientId
                                join rm in _context.CrmRelManagers
                                on clientrm.CrmRelManagerId equals rm.Id
                                where client.IsDeleted == false && request.filterSegments.Contains(rm.SegmentId) &&
                                client.Company.Contains(request.search)
                                select new ClientInfo()
                                {
                                    Id = client.Id,
                                    Company = client.Company,
                                    Industry = industry.Industry,
                                    Address = string.Join("\r\n", new[] { client.Address1, client.Address2, client.Address3 }),
                                    phone = client.Phone
                                };
                    }
                }
                else
                {
                    if (request.filterSegments.Count == 0)
                    {
                        if (request.filterRelManagers.Count == 0)
                        {
                            query = from client in _context.CrmClients
                                    join industry in _context.CrmIndustries
                                    on client.CrmIndustryId equals industry.Id
                                    where client.IsDeleted == false && request.filterIndustries.Contains(industry.Id) &&
                                    client.Company.Contains(request.search)
                                    select new ClientInfo()
                                    {
                                        Id = client.Id,
                                        Company = client.Company,
                                        Industry = industry.Industry,
                                        Address = string.Join("\r\n", new[] { client.Address1, client.Address2, client.Address3 }),
                                        phone = client.Phone
                                    };
                        }
                        else
                        {
                            query = from client in _context.CrmClients
                                    join industry in _context.CrmIndustries
                                    on client.CrmIndustryId equals industry.Id
                                    join clientrm in _context.CrmClientRelManagers
                                    on client.Id equals clientrm.CrmClientId
                                    join rm in _context.CrmRelManagers
                                    on clientrm.CrmRelManagerId equals rm.Id
                                    join user in _context.Users
                                    on rm.UserId equals user.ID
                                    where client.IsDeleted == false && request.filterRelManagers.Contains(user.ID) && request.filterIndustries.Contains(industry.Id) &&
                                    client.Company.Contains(request.search)
                                    select new ClientInfo()
                                    {
                                        Id = client.Id,
                                        Company = client.Company,
                                        Industry = industry.Industry,
                                        Address = string.Join("\r\n", new[] { client.Address1, client.Address2, client.Address3 }),
                                        phone = client.Phone
                                    };
                        }
                    }
                    else
                    {
                        query = from client in _context.CrmClients
                                join industry in _context.CrmIndustries
                                on client.CrmIndustryId equals industry.Id
                                join clientrm in _context.CrmClientRelManagers
                                on client.Id equals clientrm.CrmClientId
                                join rm in _context.CrmRelManagers
                                on clientrm.CrmRelManagerId equals rm.Id
                                where client.IsDeleted == false && request.filterSegments.Contains(rm.SegmentId) && request.filterIndustries.Contains(industry.Id) &&
                                client.Company.Contains(request.search)
                                select new ClientInfo()
                                {
                                    Id = client.Id,
                                    Company = client.Company,
                                    Industry = industry.Industry,
                                    Address = string.Join("\r\n", new[] { client.Address1, client.Address2, client.Address3 }),
                                    phone = client.Phone
                                };
                    }
                }

            }


            return query;
        }

        private IndustryInfo getClientIndustryInfo(int industryId)
        {
            var industryQuery = from i in _context.CrmIndustries
                                where i.Id == industryId && i.IsDeleted == false
                                select new IndustryInfo()
                                {
                                    Id = i.Id,
                                    Industry = i.Industry
                                };
            return industryQuery.FirstOrDefault<IndustryInfo>();
        }

        private List<ContactInfo> getClientContacts(int clientId)
        {
            var contactQuery = from c in _context.CrmContacts
                               where c.CrmClientId == clientId && c.IsDeleted == false
                               select new ContactInfo()
                               {
                                   Id = c.Id,
                                   Name = c.Name,
                                   Salutation = c.Salutation,
                                   Email = string.Join(",", new[] { c.Email1, c.Email2, c.Email3, c.Email4 }),
                                   Phone = string.Join(",", new[] { c.Phone1, c.Phone2, c.Phone3 }),
                                   Department = c.Department,
                                   Position = c.Position,
                                   Valid = c.Valid
                               };
            var contacts = contactQuery.ToList<ContactInfo>();
            char[] charsToTrim = { ',', '.', ' ' };
            foreach(var contact in contacts)
            {
                contact.Email = contact.Email.TrimEnd(charsToTrim);
                contact.Phone = contact.Phone.TrimEnd(charsToTrim);
            }
            return contacts;
        }

        private List<RelManagerInfo> getClientRelManagers(int clientId)
        {
           var relManagerQuery = from c in _context.CrmClientRelManagers
                                  join rel in _context.CrmRelManagers
                                  on c.CrmRelManagerId equals rel.Id
                                  join user in _context.Users
                                  on rel.UserId equals user.ID
                                  join netuser in _context.AspNetUsers
                                  on user.IdentityId equals netuser.Id
                                  join segment in _context.CrmSegments
                                  on rel.SegmentId equals segment.Id
                                  join branch in _context.CrmBranches
                                  on rel.BranchId equals branch.Id
                                  where c.CrmClientId == clientId && rel.IsDeleted == false && rel.isActive == true
                                  select new RelManagerInfo()
                                  {
                                      Id = user.ID,
                                      RelManagerId = rel.Id,
                                      Name = user.FirstName,
                                      Email = netuser.Email,
                                      Segment = segment.Segment,
                                      Branch = branch.Branch
                                  };

            return relManagerQuery.ToList<RelManagerInfo>();
        }

        private List<int> SplitString(string str)
        {
            List<int> r = new List<int>();

            if(str.Equals("0"))
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
        private bool isIdInContactInfoList(int id, List<ContactInfo> contacts)
        {
            foreach(ContactInfo contact in contacts)
            {
                if (contact.Id == id) return true;
            }
            return false;
        }
        private bool isRelManagerIdInRMList(int id, List<CrmClientRelManager> rms)
        {
            foreach(CrmClientRelManager rm in rms)
            {
                if (rm.CrmRelManagerId == id) return true;
            }
            return false;
        }
        private bool isContactInfoInContact(ContactInfo info, List<CrmContact> contacts)
        {
            foreach(CrmContact contact in contacts)
            {
                if (contact.Id == info.Id) return true;
            }
            return false;
        }
       

    }
}
