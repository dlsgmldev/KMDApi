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
using Microsoft.AspNetCore.Cors;
using KDMApi.Models.Pipeline;
using KDMApi.Models.Km;
using System.IO;
using KDMApi.Models.Crm;
using KDMApi.Services;
using KDMApi.Models.Helper;
using System.Data.SqlClient;
using System.Globalization;
using Org.BouncyCastle.Asn1.X509;
using System.Drawing.Imaging;

/*
SQL Update Db
use kmd;
alter table CrmDealInternalMembers add Nominal bigint not null default 0, 
UsePercent bit not null default 1;
alter table CrmDealTribes add Nominal bigint not null default 0, 
UsePercent bit not null default 1;
alter table CrmDealTribeInvoices add 
UsePercent bit not null default 1;
alter table CrmDealUserInvoices add  
UsePercent bit not null default 1;
 */
namespace KDMApi.Controllers
{
    [Route("v1/[controller]")]
    [ApiController]
    [EnableCors("QuBisaPolicy")]
    public class PipelineController : ControllerBase
    {
        private static string separator = "<!>";
        private static int EMAIL_ADD_TO_BE_INVOICED = 1;
        private static int EMAIL_EDIT_TO_BE_INVOICED = 2;
        private static int EMAIL_DELETE_TO_BE_INVOICED = 3;

        private readonly DefaultContext _context;
        private readonly FileService _fileService;
        private readonly UserService _userService;
        private readonly CrmReportService _crmReportService;
        private readonly IEmailService _emailService;
        private DataOptions _options;
        public PipelineController(DefaultContext context, Microsoft.Extensions.Options.IOptions<DataOptions> options, FileService fileService, UserService userService, CrmReportService crmReportService, IEmailService emailService)
        {
            _context = context;
            _options = options.Value;
            _fileService = fileService;
            _userService = userService;
            _crmReportService = crmReportService;
            _emailService = emailService;
        }


        // POST: v1/pipeline
        /**
         * @api {post} /pipeline POST deal 
         * @apiVersion 1.0.0
         * @apiName PostCrmDeal
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "userId": 1,
         *     "clientId": 2,
         *     "contactId": 1,
         *     "dealDate": "2020-05-20",
         *     "tribeId": 1,
         *     "segmentId": 1,
         *     "branchId": 1,
         *     "userIdRM": 1,
         *     "picId": 35,
         *     "name": "Pengembangan Budaya Organisasi",
         *     "stageId": 1,
         *     "probability": 10
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   {
         *       "id": 1,
         *       "name": "Pengembangan Budaya Organisasi",
         *       "dealDate": "2020-05-20T00:00:00",
         *       "probability": 10,
         *       "clientId": 2,
         *       "stageId": 1,
         *       "segmentId": 1,
         *       "branchId": 1,
         *       "stateId": 1,
         *       "createdDate": "2020-05-20T16:13:39.177801+07:00",
         *       "createdBy": 1,
         *       "lastUpdated": "2020-05-20T16:13:39.177801+07:00",
         *       "lastUpdatedBy": 1,
         *       "isDeleted": false,
         *       "deletedBy": 0,
         *       "deletedDate": null
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost]
        public async Task<ActionResult<CrmDeal>> PostCrmDeal(PostDealRequest request)
        {
            CrmDealRole role = GetDealRole("contact");
            if (role == null || role.Id == 0)
            {
                return BadRequest(new { error = "Role not found" });
            }

            CrmDealState state = GetDealState("open");
            if (state == null || state.Id == 0)
            {
                return BadRequest(new { error = "State not found" });
            }

            try
            {
                DateTime now = DateTime.Now;
                CrmDeal deal = new CrmDeal()
                {
                    Name = request.Name,
                    DealDate = request.DealDate,
                    Probability = request.Probability,
                    ClientId = request.ClientId,
                    StageId = request.StageId,
                    SegmentId = request.SegmentId,
                    BranchId = request.BranchId,
                    StateId = state.Id,
                    CreatedDate = now,
                    CreatedBy = request.UserId,
                    LastUpdated = now,
                    LastUpdatedBy = request.UserId,
                    IsDeleted = false
                };
                _context.CrmDeals.Add(deal);
                await _context.SaveChangesAsync();

                CrmDealTribe tribe = new CrmDealTribe()
                {
                    DealId = deal.Id,
                    TribeId = request.TribeId,
                    Percentage = 100,
                    UsePercent = true,
                    Nominal = 0,
                    CreatedDate = now,
                    CreatedBy = request.UserId,
                    LastUpdated = now,
                    LastUpdatedBy = request.UserId,
                    IsDeleted = false
                };
                _context.CrmDealTribes.Add(tribe);

                if (request.ContactId != 0)
                {
                    CrmDealExternalMember member = new CrmDealExternalMember()
                    {
                        DealId = deal.Id,
                        RoleId = role.Id,
                        ContactId = request.ContactId,
                        CreatedDate = now,
                        CreatedBy = request.UserId,
                        LastUpdated = now,
                        LastUpdatedBy = request.UserId,
                        IsDeleted = false
                    };
                    _context.CrmDealExternalMembers.Add(member);
                }

                if (request.UserIdRM != 0)
                {
                    CrmDealRole rmRole = GetDealRole("rm");
                    CrmDealInternalMember member = new CrmDealInternalMember()
                    {
                        DealId = deal.Id,
                        RoleId = rmRole.Id,
                        UserId = request.UserIdRM,
                        Percentage = 100,
                        UsePercent = true,
                        Nominal = 0,
                        CreatedDate = now,
                        CreatedBy = request.UserId,
                        LastUpdated = now,
                        LastUpdatedBy = request.UserId,
                        IsDeleted = false
                    };
                    _context.CrmDealInternalMembers.Add(member);
                }

                if (request.PicId != 0)
                {
                    CrmDealRole picRole = GetDealRole("pic");
                    CrmDealInternalMember member = new CrmDealInternalMember()
                    {
                        DealId = deal.Id,
                        RoleId = picRole.Id,
                        UserId = request.PicId,
                        Percentage = 100,
                        UsePercent = true,
                        Nominal = 0,
                        CreatedDate = now,
                        CreatedBy = request.UserId,
                        LastUpdated = now,
                        LastUpdatedBy = request.UserId,
                        IsDeleted = false
                    };
                    _context.CrmDealInternalMembers.Add(member);
                }
                await AddDealHistory("created", deal.Id, "", "", deal.DealDate, request.UserId, now, request.UserId, "Deal Created", "", "", 0, 0, 0);
                await _context.SaveChangesAsync();

                return deal;
            }
            catch
            {
                return BadRequest(new { error = "Error writing to database" });
            }

        }

        /**
         * @api {post} /pipeline/projection/invoice POST projection invoice  
         * @apiVersion 1.0.0
         * @apiName PostInvoiceChange
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "dealId": 1,
         *     "invoiceId": 13,
         *     "userId": 1,
         *     "month": 11,
         *     "year": 2020,
         *     "amount": 100000000,
         *     "remarks": "Termin 1"
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   {
         *       "id": 13,
         *       "proposalId": 7,
         *       "periodId": 11,
         *       "invoiceAmount": 100000000,
         *       "invoiceDate": "2020-11-01T00:00:00",
         *       "remarks": "Termin 1",
         *       "createdDate": "2020-05-23T12:03:07.6898834",
         *       "createdBy": 1,
         *       "lastUpdated": "2020-06-02T14:45:58.3464934+07:00",
         *       "lastUpdatedBy": 1,
         *       "isDeleted": false,
         *       "deletedBy": 0,
         *       "deletedDate": null
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost("projection/invoice")]
        public async Task<ActionResult<CrmDealProposalInvoice>> PostInvoiceChange(PostInvoiceRequest request)
        {
            CrmDealProposalInvoice invoice = _context.CrmDealProposalInvoices.Find(request.InvoiceId);
            if (invoice == null || invoice.Id == 0)
            {
                return NotFound();
            }

            DateTime now = DateTime.Now;
            int oldPeriodId = invoice.PeriodId;
            DateTime oldDate = invoice.InvoiceDate;

            DateTime invoiceDate = new DateTime(request.Year, request.Month, 1);
            int periodId = GetPeriodId(invoiceDate, request.userId);

            invoice.PeriodId = periodId;
            invoice.InvoiceAmount = request.Amount;
            invoice.InvoiceDate = invoiceDate;
            invoice.Remarks = request.Remarks;
            invoice.LastUpdated = now;
            invoice.LastUpdatedBy = request.userId;
            _context.Entry(invoice).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            if (oldPeriodId != periodId)
            {
                await AddDealHistory("invoice", request.DealId, request.InvoiceId.ToString(), invoiceDate.ToShortDateString(), now, request.userId, now, request.userId, "Invoice period changed from ", string.Join(" ", new[] { oldDate.ToString("MMMM", CultureInfo.CreateSpecificCulture("en")), oldDate.Year.ToString() }), string.Join(" ", new[] { invoiceDate.ToString("MMMM", CultureInfo.CreateSpecificCulture("en")), invoiceDate.Year.ToString() }), 0, 0, 0);
            }

            return _context.CrmDealProposalInvoices.Find(request.InvoiceId);
        }

        /**
         * @api {put} /pipeline/{dealId} PUT deal 
         * @apiVersion 1.0.0
         * @apiName PutCrmDeal
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * @apiParam {Number} dealId        Id dari deal yang bersangkutan, sama dengan id di request
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "id": 1,
         *     "userId": 1,
         *     "clientId": 2,
         *     "contactId": 1,
         *     "picId": 35,
         *     "dealDate": "2020-05-20",
         *     "segmentId": 1,
         *     "branchId": 1,
         *     "name": "Pengembangan Budaya Organisasi",
         *     "stageId": 1,
         *     "probability": 10
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   {
         *       "id": 1,
         *       "name": "Pengembangan Budaya Organisasi",
         *       "dealDate": "2020-05-20T00:00:00",
         *       "probability": 10,
         *       "clientId": 2,
         *       "stageId": 1,
         *       "tribeId": 1,
         *       "segmentId": 1,
         *       "branchId": 1,
         *       "stateId": 1,
         *       "createdDate": "2020-05-20T16:13:39.177801+07:00",
         *       "createdBy": 1,
         *       "lastUpdated": "2020-05-20T16:13:39.177801+07:00",
         *       "lastUpdatedBy": 1,
         *       "isDeleted": false,
         *       "deletedBy": 0,
         *       "deletedDate": null
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPut("{dealId}")]
        public async Task<ActionResult<CrmDeal>> PutCrmDeal(int dealId, PutDealRequest request)
        {
            if (dealId != request.Id)
            {
                return BadRequest();
            }

            if (!DealExists(dealId))
            {
                return NotFound();
            }

            CrmDealRole role = GetDealRole("contact");
            if (role == null || role.Id == 0)
            {
                return BadRequest(new { error = "Role not found" });
            }

            try
            {
                DateTime now = DateTime.Now;
                CrmDeal deal = _context.CrmDeals.Find(dealId);

                if (deal == null || deal.Id == 0)
                {
                    return NotFound();
                }

                deal.Name = request.Name;
                deal.DealDate = request.DealDate;
                deal.Probability = request.Probability;
                deal.ClientId = request.ClientId;
                deal.StageId = request.StageId;
                deal.SegmentId = request.SegmentId;
                deal.BranchId = request.BranchId;
                deal.LastUpdated = now;
                deal.LastUpdatedBy = request.UserId;

                _context.Entry(deal).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                await addOrUpdatePIC(dealId, request.PicId, request.UserId, now);

                CrmDealExternalMember curMember = _context.CrmDealExternalMembers.Where(a => a.DealId == dealId && a.RoleId == role.Id && !a.IsDeleted).FirstOrDefault();
                if (curMember != null && curMember.Id > 0)
                {
                    curMember.ContactId = request.ContactId;
                    curMember.LastUpdated = now;
                    curMember.LastUpdatedBy = request.UserId;
                    _context.Entry(curMember).State = EntityState.Modified;
                }
                else
                {
                    CrmDealExternalMember member = new CrmDealExternalMember()
                    {
                        DealId = deal.Id,
                        RoleId = role.Id,
                        ContactId = request.ContactId,
                        CreatedDate = now,
                        CreatedBy = request.UserId,
                        LastUpdated = now,
                        LastUpdatedBy = request.UserId,
                        IsDeleted = false
                    };

                    _context.CrmDealExternalMembers.Add(member);
                }

                await _context.SaveChangesAsync();

                return deal;
            }
            catch
            {
                return BadRequest(new { error = "Error writing to database" });
            }
        }

        /**
         * @api {put} /pipeline/client/{clientId} PUT client contacts 
         * @apiVersion 1.0.0
         * @apiName PutClientContacts
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * @apiParam {Number} clientId        Id dari client yang bersangkutan, sama dengan clientId di request         
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "clientId": 2,
         *     "dealId": 1,
         *     "userId": 2,
         *     "companyName": "Bank Central Asia (BCA)",
         *     "contactId": 2,
         *     "memberIds": [
         *       1
         *     ]
         *   }
         *   
         * @apiSuccessExample Success-Response:
         * NoContent
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPut("client/{clientId}")]
        public async Task<IActionResult> PutClientContacts(int clientId, CompanyInfo request)
        {
            if (clientId != request.ClientId)
            {
                return BadRequest();
            }

            if (!_context.CrmClients.Any(e => e.Id == clientId))
            {
                return NotFound();
            }

            CrmDealRole role = GetDealRole("contact");
            CrmDealRole roleMember = GetDealRole("member");

            if (role == null || role.Id == 0 || roleMember == null || roleMember.Id == 0)
            {
                return BadRequest(new { error = "Role not found" });
            }

            DateTime now = DateTime.Now;

            CrmClient client = _context.CrmClients.Find(clientId);
            if (!client.Company.Equals(request.CompanyName))
            {
                client.Company = request.CompanyName;
                _context.Entry(client).State = EntityState.Modified;
            }

            List<CrmDealExternalMember> curContacts = await _context.CrmDealExternalMembers.Where(a => a.DealId == request.DealId && !a.IsDeleted).ToListAsync();
            foreach (CrmDealExternalMember curContact in curContacts)
            {
                if (curContact.RoleId == role.Id)
                {
                    if (curContact.ContactId != request.ContactId && request.ContactId != 0)
                    {
                        curContact.IsDeleted = true;
                        curContact.DeletedBy = request.UserId;
                        curContact.LastUpdated = now;
                        curContact.LastUpdatedBy = request.UserId;
                        _context.Entry(curContact).State = EntityState.Modified;
                    }
                }
                else
                {
                    if (!request.MemberIds.Contains(curContact.Id))
                    {
                        curContact.IsDeleted = true;
                        curContact.DeletedBy = request.UserId;
                        curContact.LastUpdated = now;
                        curContact.LastUpdatedBy = request.UserId;
                        _context.Entry(curContact).State = EntityState.Modified;
                    }
                }
            }

            if (request.ContactId != 0)
            {
                CrmDealExternalMember member = curContacts.FirstOrDefault(a => a.Id == request.ContactId);
                if (member == null || member.Id == 0)
                {
                    CrmDealExternalMember member1 = new CrmDealExternalMember()
                    {
                        DealId = request.DealId,
                        RoleId = role.Id,
                        ContactId = request.ContactId,
                        CreatedDate = now,
                        CreatedBy = request.UserId,
                        LastUpdated = now,
                        LastUpdatedBy = request.UserId,
                        IsDeleted = false
                    };

                    _context.CrmDealExternalMembers.Add(member1);

                }
            }

            foreach (int id in request.MemberIds)
            {
                if (id != 0)
                {
                    CrmDealExternalMember member2 = curContacts.FirstOrDefault(a => a.Id == id);


                    if (member2 == null || member2.Id == 0)
                    {
                        CrmDealExternalMember member1 = new CrmDealExternalMember()
                        {
                            DealId = request.DealId,
                            RoleId = roleMember.Id,
                            ContactId = id,
                            CreatedDate = now,
                            CreatedBy = request.UserId,
                            LastUpdated = now,
                            LastUpdatedBy = request.UserId,
                            IsDeleted = false
                        };

                        _context.CrmDealExternalMembers.Add(member1);

                    }

                }
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /**
         * @api {get} /Pipeline/master GET data master
         * @apiVersion 1.0.0
         * @apiName GetMasterData
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiSuccessExample Success-Response:
         * {
         *     "segments": [
         *         {
         *             "id": 1,
         *             "text": "BUMN"
         *         },
         *         {
         *             "id": 2,
         *             "text": "Government"
         *         },
         *         {
         *             "id": 3,
         *             "text": "Private"
         *         },
         *         {
         *             "id": 4,
         *             "text": "Public Seminar"
         *         }
         *     ],
         *     "tribes": [
         *         {
         *             "id": 1,
         *             "text": "Strategy and Execution"
         *         },
         *         {
         *             "id": 2,
         *             "text": "Organizational Excellence"
         *         }
         *     ],
         *     "branches": [
         *         {
         *             "id": 1,
         *             "text": "Jakarta"
         *         },
         *         {
         *             "id": 2,
         *             "text": "Medan"
         *         },
         *         {
         *             "id": 3,
         *             "text": "Surabaya"
         *         },
         *         {
         *             "id": 4,
         *             "text": "Makassar"
         *         }
         *     ],
         *     "rms": [
         *         {
         *             "id": 1,
         *             "userId": 5,
         *             "segmentId": 3,
         *             "branchId": 1,
         *             "leaderId": 0,
         *             "name": "Leviana Wijaya",
         *             "email": "levi@gmlperformanceco.id"
         *         },
         *         {
         *             "id": 2,
         *             "userId": 7,
         *             "segmentId": 3,
         *             "branchId": 1,
         *             "leaderId": 0,
         *             "name": "Grace",
         *             "email": "grace@gmlperformance.co.id"
         *         }
         *     ],
         *     "stages": [
         *         {
         *             "id": 1,
         *             "text": "Lead In"
         *         },
         *         {
         *             "id": 2,
         *             "text": "Proposal Development"
         *         },
         *         {
         *             "id": 3,
         *             "text": "Proposal Sent"
         *         },
         *         {
         *             "id": 4,
         *             "text": "Presentation"
         *         },
         *         {
         *             "id": 5,
         *             "text": "Negotiation"
         *         }
         *     ],
         *     "states": [
         *         {
         *             "id": 1,
         *             "text": "Open"
         *         },
         *         {
         *             "id": 2,
         *             "text": "Won"
         *         },
         *         {
         *             "id": 3,
         *             "text": "Lost"
         *         },
         *         {
         *             "id": 4,
         *             "text": "Reopen"
         *         }
         *     ],
         *     "proposalTypes": [
         *         {
         *             "id": 1,
         *             "text": "Workshop"
         *         },
         *         {
         *             "id": 2,
         *             "text": "Project"
         *         }
         *     ]
         * }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("master")]
        public async Task<ActionResult<PipelineMasterResponse>> GetMasterData()
        {
            return new PipelineMasterResponse()
            {
                Branches = await GetAllBranches(),
                Segments = await GetAllSegments(),
                States = await GetAllStates(),
                Stages = await GetAllStages(),
                Rms = await GetAllRMs(),
                Tribes = await GetAllTribes(),
                ProposalTypes = await GetProposalTypes()
            };
        }

        /**
         * @api {get} /pipeline/{tribeFilter}/{segmentFilter}/{rmFilter}/{probabilityFilter} GET pipeline
         * @apiVersion 1.0.0
         * @apiName GetPipelineItems
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {String} tribeFilter           0 untuk tidak menggunakan filter, atau comma-separated values dari tribeId, misal 1,3.
         * @apiParam {String} segmentFilter         0 untuk tidak menggunakan filter, atau comma-separated values dari segmentId, misal 2,3.
         * @apiParam {String} rmFilter              0 untuk tidak menggunakan filter, atau comma-separated values dari userId dari RM, misal 4,7. Perhatikan id dan userId itu berbeda.
         * @apiParam {String} probabilityFilter     0 untuk tidak menggunakan filter, atau comma-separated values dari probability, misal 10,20 
         * 
         * @apiSuccessExample Success-Response:
         *   [
         *     {
         *       "dealId": 1,
         *       "dealName": "Pengembangan Budaya Organisasi",
         *       "clientId": 2,
         *       "clientName": "PT Bank Mandiri Tbk.",
         *       "probability": 10,
         *       "age": 4,
         *       "stage": 1,
         *       "dealDate": "2020-05-20T00:00:00",
         *       "proposalId": 30,
         *       "proposalValue": 38500000,
         *       "invoicePeriod": 0,
         *       "rms": [
         *           {
         *               "id": 29,
         *               "text": "Monalisa Bangun",
         *               "percent": 100.0
         *           }
         *       ],
         *       "access": [
         *           {
         *                "id": 27,
         *                "text": "Laura Theresia Purba"
         *           }
         *       ]
         *     },
         *     {
         *       "dealId": 4,
         *       "dealName": "Pelatihan Kepemimpinan",
         *       "clientId": 2,
         *       "clientName": "PT Bank Mandiri Tbk.",
         *       "probability": 10,
         *       "age": 4,
         *       "stage": 1,
         *       "dealDate": "2020-05-20T00:00:00",
         *       "proposalId": 30,
         *       "proposalValue": 38500000,
         *       "invoicePeriod": 0,
         *       "rms": [
         *           {
         *               "id": 29,
         *               "text": "Monalisa Bangun",
         *               "percent": 100.0
         *           }
         *       ],
         *       "access": [
         *           {
         *                "id": 27,
         *                "text": "Laura Theresia Purba"
         *           }
         *       ]
         *     }
         *   ]
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("{tribeFilter}/{segmentFilter}/{rmFilter}/{probabilityFilter}")]
        public async Task<ActionResult<List<PipelineItem>>> GetPipelineItems(string tribeFilter, string segmentFilter, string rmFilter, string probabilityFilter)
        {
            CrmDealRole role = GetDealRole("rm");

            CrmDealState state = GetDealState("open");
            DateTime now = DateTime.Now;

            string selectSql = "SELECT DISTINCT deal.Id AS DealId, deal.Name AS DealName, client.Id AS ClientId, client.Company AS ClientName, deal.Probability AS Probability, DATEDIFF(day, deal.DealDate, GETDATE()) AS Age, deal.StageId AS Stage, deal.DealDate, ISNULL(proposal.Id, 0) As ProposalId, ISNULL(proposal.Id, 0) As InvoicePeriod, ISNULL(proposal.ProposalValue, 0 ) As ProposalValue";
            string fromSql = "FROM dbo.CrmDeals AS deal";
            string joinSql0 = "JOIN dbo.CrmClients AS client ON deal.ClientId = client.Id";
            string joinSql0b = "JOIN dbo.CrmDealTribes AS dealTribe ON deal.Id = dealTribe.DealId";
            string joinSql1 = "JOIN dbo.CoreTribes AS tribe ON dealTribe.TribeId = tribe.Id ";
            string joinSql2 = "JOIN dbo.CrmSegments AS segment ON deal.SegmentId = segment.Id ";
            string joinSql2b = "LEFT JOIN dbo.CrmDealProposals AS proposal ON deal.Id = proposal.DealId AND proposal.IsActive = 1";
            string orderBy = "ORDER BY deal.StageId, deal.DealDate";

            List<string> wheres = new List<string>();
            wheres.Add(string.Join(" ", new[] { "WHERE deal.IsDeleted = 0", "AND", "deal.StateId", "=", state.Id.ToString() }));
            wheres.Add(CreateWhereClause(tribeFilter, "AND ", "tribe.Id", "OR"));
            wheres.Add(CreateWhereClause(segmentFilter, "AND ", "segment.Id", "OR"));
            wheres.Add(CreateWhereClause(probabilityFilter, "AND ", "deal.Probability", "OR"));

            string joinSql3 = "";
            if (!rmFilter.Equals("0"))
            {
                joinSql3 = "JOIN dbo.CrmDealInternalMembers AS member ON deal.Id = member.DealId";
                wheres.Add(string.Join(" ", new[] { "AND", "member.RoleId", "=", role.Id.ToString() }));
                wheres.Add(CreateWhereClause(rmFilter, "AND ", "member.UserId", "OR"));
            }
            string whereSql = string.Join(" ", wheres);
            string sql = string.Join(" ", new[] { selectSql, fromSql, joinSql0, joinSql0b, joinSql1, joinSql2, joinSql2b, joinSql3, whereSql, orderBy });

            List<PipelineItem> items = await _context.PipelineItems.FromSql(sql).ToListAsync<PipelineItem>();
            foreach (PipelineItem item in items)
            {
                item.Rms = await GetInternalMembers(item.DealId, role.Id);
                item.Access = GetAccess(item.Rms);
                if (item.ProposalId != 0)
                {
                    item.InvoicePeriod = _context.CrmDealProposalInvoices.Where(a => a.ProposalId == item.ProposalId && !a.IsDeleted).Count();
                }
            }
            return items;
        }

        /**
         * @api {get} /pipeline/{fromMonth}/{toMonth}/{tribeFilter}/{segmentFilter}/{rmFilter}/{probabilityFilter} GET pipeline filter period
         * @apiVersion 1.0.0
         * @apiName GetPipelineItemsByMonth
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {String} fromMonth             Filter untuk bulan, dalam format YYYYMM, misal 202005 untuk bulan Mei 2020. Gunakan 0 untuk tidak menggunakan filter bulan.
         * @apiParam {String} toMonth               Filter untuk bulan, dalam format YYYYMM, misal 202007 untuk bulan Juli 2020. Gunakan 0 untuk tidak menggunakan filter bulan.
         * @apiParam {String} tribeFilter           0 untuk tidak menggunakan filter, atau comma-separated values dari tribeId, misal 1,3.
         * @apiParam {String} segmentFilter         0 untuk tidak menggunakan filter, atau comma-separated values dari segmentId, misal 2,3.
         * @apiParam {String} rmFilter              0 untuk tidak menggunakan filter, atau comma-separated values dari userId dari RM, misal 4,7. Perhatikan id dan userId itu berbeda.
         * @apiParam {String} probabilityFilter     0 untuk tidak menggunakan filter, atau comma-separated values dari probability, misal 10,20 
         * 
         * @apiSuccessExample Success-Response:
         *   [
         *     {
         *       "dealId": 1,
         *       "dealName": "Pengembangan Budaya Organisasi",
         *       "clientId": 2,
         *       "clientName": "PT Bank Mandiri Tbk.",
         *       "probability": 10,
         *       "age": 4,
         *       "stage": 1,
         *       "dealDate": "2020-05-20T00:00:00",
         *       "proposalId": 30,
         *       "proposalValue": 38500000,
         *       "invoicePeriod": 0,
         *       "rms": [
         *           {
         *               "id": 29,
         *               "text": "Monalisa Bangun",
         *               "percent": 100.0
         *           }
         *       ],
         *       "access": [
         *           {
         *                "id": 27,
         *                "text": "Laura Theresia Purba"
         *           }
         *       ]
         *     },
         *     {
         *       "dealId": 4,
         *       "dealName": "Pelatihan Kepemimpinan",
         *       "clientId": 2,
         *       "clientName": "PT Bank Mandiri Tbk.",
         *       "probability": 10,
         *       "age": 4,
         *       "stage": 1,
         *       "dealDate": "2020-05-20T00:00:00",
         *       "proposalId": 30,
         *       "proposalValue": 38500000,
         *       "invoicePeriod": 0,
         *       "rms": [
         *           {
         *               "id": 29,
         *               "text": "Monalisa Bangun",
         *               "percent": 100.0
         *           }
         *       ],
         *       "access": [
         *           {
         *                "id": 27,
         *                "text": "Laura Theresia Purba"
         *           }
         *       ]
         *     }
         *   ]
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("{fromMonth}/{toMonth}/{tribeFilter}/{segmentFilter}/{rmFilter}/{probabilityFilter}")]
        public async Task<ActionResult<List<PipelineItem>>> GetPipelineItemsByMonth(string fromMonth, string toMonth, string tribeFilter, string segmentFilter, string rmFilter, string probabilityFilter)
        {
            CrmDealRole role = GetDealRole("rm");

            CrmDealState state = GetDealState("open");
            DateTime now = DateTime.Now;

            string selectSql = "SELECT DISTINCT deal.Id AS DealId, deal.Name AS DealName, client.Id AS ClientId, client.Company AS ClientName, deal.Probability AS Probability, DATEDIFF(day, deal.DealDate, GETDATE()) AS Age, deal.StageId AS Stage, deal.DealDate, ISNULL(proposal.Id, 0) As ProposalId, ISNULL(proposal.Id, 0) As InvoicePeriod, ISNULL(proposal.ProposalValue, 0) As ProposalValue, deal.DealDate as StatusDate";
            string fromSql = "FROM dbo.CrmDeals AS deal";
            string joinSql0 = "JOIN dbo.CrmClients AS client ON deal.ClientId = client.Id";
            string joinSql0b = "JOIN dbo.CrmDealTribes AS dealTribe ON deal.Id = dealTribe.DealId";
            string joinSql1 = "JOIN dbo.CoreTribes AS tribe ON dealTribe.TribeId = tribe.Id ";
            string joinSql2 = "JOIN dbo.CrmSegments AS segment ON deal.SegmentId = segment.Id ";
            string joinSql2b = "LEFT JOIN dbo.CrmDealProposals AS proposal ON deal.Id = proposal.DealId AND proposal.IsActive = 1";
            string orderBy = "ORDER BY deal.StageId, deal.DealDate";

            string whereFromMonth = "'19000101'";
            string whereToMonth = "'21001231'";

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
                return BadRequest(new { error = "Time format error" });
            }

            string whereMonthClause = string.Join("", new[] { "(deal.DealDate >= ", whereFromMonth, " AND deal.DealDate <= ", whereToMonth, ")" }); // "deal.DealDate BETWEEN { ts '2008-12-20 00:00:00'} AND { ts '2008-12-20 23:59:59'}";
            List<string> wheres = new List<string>();
            wheres.Add(string.Join(" ", new[] { "WHERE", whereMonthClause, "AND deal.IsDeleted = 0", "AND", "deal.StateId", "=", state.Id.ToString() }));
            wheres.Add(CreateWhereClause(tribeFilter, "AND ", "tribe.Id", "OR"));
            wheres.Add(CreateWhereClause(segmentFilter, "AND ", "segment.Id", "OR"));
            wheres.Add(CreateWhereClause(probabilityFilter, "AND ", "deal.Probability", "OR"));

            string joinSql3 = "";
            if (!rmFilter.Equals("0"))
            {
                joinSql3 = "JOIN dbo.CrmDealInternalMembers AS member ON deal.Id = member.DealId";
                wheres.Add(string.Join(" ", new[] { "AND", "member.RoleId", "=", role.Id.ToString() }));
                wheres.Add(CreateWhereClause(rmFilter, "AND ", "member.UserId", "OR"));
            }
            string whereSql = string.Join(" ", wheres);
            string sql = string.Join(" ", new[] { selectSql, fromSql, joinSql0, joinSql0b, joinSql1, joinSql2, joinSql2b, joinSql3, whereSql, orderBy });

            List<PipelineItem> items = new List<PipelineItem>();
            items = await _context.PipelineItems.FromSql(sql).ToListAsync<PipelineItem>();

            foreach (PipelineItem item in items)
            {
                item.Rms = await GetInternalMembers(item.DealId, role.Id);
                item.Access = GetAccess(item.Rms);
                if (item.ProposalId != 0)
                {
                    item.InvoicePeriod = _context.CrmDealProposalInvoices.Where(a => a.ProposalId == item.ProposalId && !a.IsDeleted).Count();
                }
                item.StatusDate = GetStatusDate(item.DealId);
            }

            return items;
        }

        /**
         * @api {get} /pipeline/projection/{tribeFilter}/{segmentFilter}/{rmFilter}/{probabilityFilter} GET projection
         * @apiVersion 1.0.0
         * @apiName GetProjectionItems
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {String} tribeFilter           0 untuk tidak menggunakan filter, atau comma-separated values dari tribeId, misal 1,3.
         * @apiParam {String} segmentFilter         0 untuk tidak menggunakan filter, atau comma-separated values dari segmentId, misal 2,3.
         * @apiParam {String} rmFilter              0 untuk tidak menggunakan filter, atau comma-separated values dari userId dari RM, misal 4,7. Perhatikan id dan userId itu berbeda.
         * @apiParam {String} probabilityFilter     0 untuk tidak menggunakan filter, atau comma-separated values dari probability, misal 10,20 
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "stages": [
         *           {
         *               "stage": 7,
         *               "month": 7,
         *               "year": 2020,
         *               "text": "July"
         *           },
         *           {
         *               "stage": 8,
         *               "month": 8,
         *               "year": 2020,
         *               "text": "August"
         *           },
         *           {
         *               "stage": 9,
         *               "month": 9,
         *               "year": 2020,
         *               "text": "September"
         *           },
         *           {
         *               "stage": 10,
         *               "month": 10,
         *               "year": 2020,
         *               "text": "October"
         *           },
         *           {
         *               "stage": 11,
         *               "month": 11,
         *               "year": 2020,
         *               "text": "..."
         *           }
         *       ],
         *       "projection": [
         *           {
         *               "dealId": 1,
         *               "stage": 7,
         *               "month": 7,
         *               "year": 2020,
         *               "invoiceDate": "2020-11-01T00:00:00",
         *               "remarks": "Termin 1",
         *               "dealName": "Pengembangan Budaya Organisasi",
         *               "clientId": 2,
         *               "clientName": "PT Bank Mandiri Tbk.",
         *               "probability": 80,
         *               "age": 6,
         *               "dealDate": "2020-05-20T00:00:00",
         *               "proposalValue": 60000000
         *               "rms": [
         *                  {
         *                      "id": 29,
         *                      "text": "Monalisa Bangun",
         *                      "percent": 100.0
         *                  }
         *               ],
         *               "access": [
         *                  {
         *                      "id": 27,
         *                      "text": "Laura Theresia Purba"
         *                  }
         *               ]
         *           },
         *           {
         *               "dealId": 1,
         *               "stage": 9,
         *               "month": 9,
         *               "year": 2020,
         *               "invoiceDate": "2021-11-01T00:00:00",
         *               "remarks": "Termin 2",
         *               "dealName": "Pengembangan Budaya Organisasi",
         *               "clientId": 2,
         *               "clientName": "PT Bank Mandiri Tbk.",
         *               "probability": 80,
         *               "age": 6,
         *               "dealDate": "2020-05-20T00:00:00",
         *               "proposalValue": 40000000
         *               "rms": [
         *                  {
         *                      "id": 29,
         *                      "text": "Monalisa Bangun",
         *                      "percent": 100.0
         *                  }
         *               ],
         *               "access": [
         *                  {
         *                      "id": 27,
         *                      "text": "Laura Theresia Purba"
         *                  }
         *               ]
         *           }
         *       ]
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("projection/{tribeFilter}/{segmentFilter}/{rmFilter}/{probabilityFilter}")]
        public async Task<ActionResult<ProjectionResponse>> GetProjectionItems(string tribeFilter, string segmentFilter, string rmFilter, string probabilityFilter)
        {
            ProjectionResponse response = new ProjectionResponse();

            CrmDealState state = GetDealState("open");
            CrmDealRole role = GetDealRole("rm");

            string selectSql = "SELECT DISTINCT deal.Id AS DealId, deal.Name AS DealName, client.Id AS ClientId, client.Company AS ClientName, deal.Probability AS Probability, DATEDIFF(day, deal.DealDate, GETDATE()) AS Age, MONTH(invoice.InvoiceDate) as Stage, MONTH(invoice.InvoiceDate) as Month, YEAR(invoice.InvoiceDate) as Year, deal.DealDate, ISNULL(invoice.InvoiceAmount, 0 ) As Value, invoice.Id AS InvoiceId, invoice.Remarks, invoice.invoiceDate";
            string fromSql = "FROM dbo.CrmDealProposalInvoices AS invoice";
            string joinSql1 = "JOIN dbo.CrmDealProposals AS proposal ON invoice.ProposalId = proposal.Id AND proposal.IsActive = 1";
            string joinSql2 = "JOIN dbo.CrmDeals as deal ON proposal.DealId = deal.Id";
            string joinSql3 = "JOIN dbo.CrmClients AS client ON deal.ClientId = client.Id";
            string joinSql3b = "JOIN dbo.CrmDealTribes AS dealTribe ON dealTribe.DealId = deal.Id ";
            string joinSql4 = "JOIN dbo.CoreTribes AS tribe ON tribe.Id = dealTribe.TribeId";
            string joinSql5 = "JOIN dbo.CrmSegments AS segment ON deal.SegmentId = segment.Id ";
            string orderBy = "ORDER BY Year, Month";

            List<string> wheres = new List<string>();
            wheres.Add(string.Join(" ", new[] { "WHERE deal.IsDeleted = 0", "AND", "deal.StateId", "=", state.Id.ToString() }));
            wheres.Add(CreateWhereClause(tribeFilter, "AND ", "tribe.Id", "OR"));
            wheres.Add(CreateWhereClause(segmentFilter, "AND ", "segment.Id", "OR"));
            wheres.Add(CreateWhereClause(probabilityFilter, "AND ", "deal.Probability", "OR"));

            string joinSql6 = "";
            if (!rmFilter.Equals("0"))
            {
                joinSql6 = "JOIN dbo.CrmDealInternalMembers AS member ON deal.Id = member.DealId";
                wheres.Add(string.Join(" ", new[] { "AND", "member.RoleId", "=", role.Id.ToString() }));
                wheres.Add(CreateWhereClause(rmFilter, "AND ", "member.UserId", "OR"));
            }
            string whereSql = string.Join(" ", wheres);
            string sql = string.Join(" ", new[] { selectSql, fromSql, joinSql1, joinSql2, joinSql3, joinSql3b, joinSql4, joinSql5, joinSql6, whereSql, orderBy });

            response.Projection = await _context.ProjectionItems.FromSql(sql).ToListAsync<ProjectionItem>();

            selectSql = "SELECT TOP(1) MONTH(invoice.InvoiceDate) AS Month, YEAR(invoice.InvoiceDate) as Year, deal.StageId as Stage, deal.Name as Text";   // Stage and Text are not used
            orderBy = "ORDER BY Year desc, Month desc";
            sql = string.Join(" ", new[] { selectSql, fromSql, joinSql1, joinSql2, joinSql3, joinSql3b, joinSql4, joinSql5, joinSql6, whereSql, orderBy });

            var clast = _context.MonthStageInfos.FromSql(sql).Count(a => a.Month > 0);

            MonthStageInfo last = null;
            if (clast != 0)
            {
                last = _context.MonthStageInfos.FromSql(sql).First();
            }


            orderBy = "ORDER BY Year, Month";
            sql = string.Join(" ", new[] { selectSql, fromSql, joinSql1, joinSql2, joinSql3, joinSql3b, joinSql4, joinSql5, joinSql6, whereSql, orderBy });

            var co = _context.MonthStageInfos.FromSql(sql).Count(a => a.Month > 0);

            int minMonth = 0;
            int minYear = 0;

            if (co == 0)
            {
                DateTime now = DateTime.Now;
                response.Stages = GetMonthStages(now.Month, now.Year, 5, last);
                minMonth = now.Month;
                minYear = now.Year;
            }
            else
            {
                var fi = _context.MonthStageInfos.FromSql(sql).First();
                response.Stages = GetMonthStages(fi.Month, fi.Year, 5, last);
                minMonth = fi.Month;
                minYear = fi.Year;
            }

            foreach (ProjectionItem item in response.Projection)
            {
                if (item.Year == minYear)
                {
                    item.Stage = (item.Month - minMonth) + 1;
                }
                else
                {
                    int i = (item.Year - minYear) * 12;
                    item.Stage = ((item.Month + i) - minMonth) + 1;
                }

                if (item.Stage > 5) item.Stage = 5;

                item.Rms = await GetInternalMembers(item.DealId, role.Id);
                item.Access = GetAccess(item.Rms);
            }

            return response;
        }

        /**
         * @api {get} /pipeline/projection/{fromMonth}/{toMonth}/{tribeFilter}/{segmentFilter}/{rmFilter}/{probabilityFilter} GET projection filter period
         * @apiVersion 1.0.0
         * @apiName GetProjectionItemsByMonth
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {String} fromMonth             Filter untuk bulan, dalam format YYYYMM, misal 202005 untuk bulan Mei 2020. Gunakan 0 untuk tidak menggunakan filter bulan.
         * @apiParam {String} toMonth               Filter untuk bulan, dalam format YYYYMM, misal 202007 untuk bulan Juli 2020. Gunakan 0 untuk tidak menggunakan filter bulan.
         * @apiParam {String} tribeFilter           0 untuk tidak menggunakan filter, atau comma-separated values dari tribeId, misal 1,3.
         * @apiParam {String} segmentFilter         0 untuk tidak menggunakan filter, atau comma-separated values dari segmentId, misal 2,3.
         * @apiParam {String} rmFilter              0 untuk tidak menggunakan filter, atau comma-separated values dari userId dari RM, misal 4,7. Perhatikan id dan userId itu berbeda.
         * @apiParam {String} probabilityFilter     0 untuk tidak menggunakan filter, atau comma-separated values dari probability, misal 10,20 
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "stages": [
         *           {
         *               "stage": 7,
         *               "month": 7,
         *               "year": 2020,
         *               "text": "July"
         *           },
         *           {
         *               "stage": 8,
         *               "month": 8,
         *               "year": 2020,
         *               "text": "August"
         *           },
         *           {
         *               "stage": 9,
         *               "month": 9,
         *               "year": 2020,
         *               "text": "September"
         *           },
         *           {
         *               "stage": 10,
         *               "month": 10,
         *               "year": 2020,
         *               "text": "October"
         *           },
         *           {
         *               "stage": 11,
         *               "month": 11,
         *               "year": 2020,
         *               "text": "..."
         *           }
         *       ],
         *       "projection": [
         *           {
         *               "dealId": 1,
         *               "stage": 7,
         *               "month": 7,
         *               "year": 2020,
         *               "invoiceDate": "2020-11-01T00:00:00",
         *               "remarks": "Termin 1",
         *               "dealName": "Pengembangan Budaya Organisasi",
         *               "clientId": 2,
         *               "clientName": "PT Bank Mandiri Tbk.",
         *               "probability": 80,
         *               "age": 6,
         *               "dealDate": "2020-05-20T00:00:00",
         *               "proposalValue": 60000000,
         *               "rms": [
         *                  {
         *                      "id": 29,
         *                      "text": "Monalisa Bangun",
         *                      "percent": 100.0
         *                  }
         *               ],
         *               "access": [
         *                  {
         *                      "id": 27,
         *                      "text": "Laura Theresia Purba"
         *                  }
         *               ]
         *           },
         *           {
         *               "dealId": 1,
         *               "stage": 9,
         *               "month": 9,
         *               "year": 2020,
         *               "invoiceDate": "2021-11-01T00:00:00",
         *               "remarks": "Termin 2",
         *               "dealName": "Pengembangan Budaya Organisasi",
         *               "clientId": 2,
         *               "clientName": "PT Bank Mandiri Tbk.",
         *               "probability": 80,
         *               "age": 6,
         *               "dealDate": "2020-05-20T00:00:00",
         *               "proposalValue": 40000000,
         *               "rms": [
         *                  {
         *                      "id": 29,
         *                      "text": "Monalisa Bangun",
         *                      "percent": 100.0
         *                  }
         *               ],
         *               "access": [
         *                  {
         *                      "id": 27,
         *                      "text": "Laura Theresia Purba"
         *                  }
         *               ]
         *           }
         *       ]
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("projection/{fromMonth}/{toMonth}/{tribeFilter}/{segmentFilter}/{rmFilter}/{probabilityFilter}")]
        public async Task<ActionResult<ProjectionResponse>> GetProjectionItemsByMonth(string fromMonth, string toMonth, string tribeFilter, string segmentFilter, string rmFilter, string probabilityFilter)
        {
            ProjectionResponse response = new ProjectionResponse();

            CrmDealState state = GetDealState("open");
            CrmDealRole role = GetDealRole("rm");


            string selectSql = "SELECT DISTINCT deal.Id AS DealId, deal.Name AS DealName, client.Id AS ClientId, client.Company AS ClientName, deal.Probability AS Probability, DATEDIFF(day, deal.DealDate, GETDATE()) AS Age, MONTH(invoice.InvoiceDate) as Stage, MONTH(invoice.InvoiceDate) as Month, YEAR(invoice.InvoiceDate) as Year, deal.DealDate, ISNULL(invoice.InvoiceAmount, 0 ) As Value, invoice.Id AS InvoiceId, invoice.Remarks, invoice.invoiceDate";
            string fromSql = "FROM dbo.CrmDealProposalInvoices AS invoice";
            string joinSql1 = "JOIN dbo.CrmDealProposals AS proposal ON invoice.ProposalId = proposal.Id AND proposal.IsActive = 1";
            string joinSql2 = "JOIN dbo.CrmDeals as deal ON proposal.DealId = deal.Id";
            string joinSql3 = "JOIN dbo.CrmClients AS client ON deal.ClientId = client.Id";
            string joinSql3b = "JOIN dbo.CrmDealTribes AS dealTribe ON dealTribe.DealId = deal.Id ";
            string joinSql4 = "JOIN dbo.CoreTribes AS tribe ON tribe.Id = dealTribe.TribeId";
            string joinSql5 = "JOIN dbo.CrmSegments AS segment ON deal.SegmentId = segment.Id ";
            string orderBy = "ORDER BY Year, Month";


            string whereFromMonth = "'19000101'";
            string whereToMonth = "'21001231'";

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
                return BadRequest(new { error = "Time format error" });
            }

            string whereMonthClause = string.Join("", new[] { "(invoice.InvoiceDate >= ", whereFromMonth, " AND invoice.InvoiceDate <= ", whereToMonth, ")" }); // "deal.DealDate BETWEEN { ts '2008-12-20 00:00:00'} AND { ts '2008-12-20 23:59:59'}";
            List<string> wheres = new List<string>();
            wheres.Add(string.Join(" ", new[] { "WHERE", whereMonthClause, "AND deal.IsDeleted = 0", "AND", "deal.StateId", "=", state.Id.ToString() }));
            wheres.Add(CreateWhereClause(tribeFilter, "AND ", "tribe.Id", "OR"));
            wheres.Add(CreateWhereClause(segmentFilter, "AND ", "segment.Id", "OR"));
            wheres.Add(CreateWhereClause(probabilityFilter, "AND ", "deal.Probability", "OR"));

            string joinSql6 = "";
            if (!rmFilter.Equals("0"))
            {
                joinSql6 = "JOIN dbo.CrmDealInternalMembers AS member ON deal.Id = member.DealId";
                wheres.Add(string.Join(" ", new[] { "AND", "member.RoleId", "=", role.Id.ToString() }));
                wheres.Add(CreateWhereClause(rmFilter, "AND ", "member.UserId", "OR"));
            }
            string whereSql = string.Join(" ", wheres);
            string sql = string.Join(" ", new[] { selectSql, fromSql, joinSql1, joinSql2, joinSql3, joinSql3b, joinSql4, joinSql5, joinSql6, whereSql, orderBy });

            response.Projection = await _context.ProjectionItems.FromSql(sql).ToListAsync<ProjectionItem>();

            selectSql = "SELECT TOP(1) MONTH(invoice.InvoiceDate) AS Month, YEAR(invoice.InvoiceDate) as Year, deal.StageId as Stage, deal.Name as Text";   // Stage and Text are not used
            orderBy = "ORDER BY Year desc, Month desc";
            sql = string.Join(" ", new[] { selectSql, fromSql, joinSql1, joinSql2, joinSql3, joinSql3b, joinSql4, joinSql5, joinSql6, whereSql, orderBy });

            var clast = _context.MonthStageInfos.FromSql(sql).Count(a => a.Month > 0);

            MonthStageInfo last = null;
            if (clast != 0)
            {
                last = _context.MonthStageInfos.FromSql(sql).First();
            }


            orderBy = "ORDER BY Year, Month";
            sql = string.Join(" ", new[] { selectSql, fromSql, joinSql1, joinSql2, joinSql3, joinSql3b, joinSql4, joinSql5, joinSql6, whereSql, orderBy });

            var co = _context.MonthStageInfos.FromSql(sql).Count(a => a.Month > 0);

            int minMonth = 0;
            int minYear = 0;

            if (co == 0)
            {
                DateTime now = DateTime.Now;
                response.Stages = GetMonthStages(now.Month, now.Year, 5, last);
                minMonth = now.Month;
                minYear = now.Year;
            }
            else
            {
                var fi = _context.MonthStageInfos.FromSql(sql).First();
                response.Stages = GetMonthStages(fi.Month, fi.Year, 5, last);
                minMonth = fi.Month;
                minYear = fi.Year;
            }

            foreach (ProjectionItem item in response.Projection)
            {
                if (item.Year == minYear)
                {
                    item.Stage = (item.Month - minMonth) + 1;
                }
                else
                {
                    int i = (item.Year - minYear) * 12;
                    item.Stage = ((item.Month + i) - minMonth) + 1;
                }

                if (item.Stage > 5) item.Stage = 5;

                item.Rms = await GetInternalMembers(item.DealId, role.Id);
                item.Access = GetAccess(item.Rms);

            }

            return response;
        }

        /**
         * @api {get} /pipeline/lostdeal/{fromMonth}/{toMonth}/{tribeFilter}/{segmentFilter}/{rmFilter}/{page}/{perPage}/{search} GET lost deal filter period
         * @apiVersion 1.0.0
         * @apiName GetLostDealByMonth
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {String} fromMonth             Filter untuk bulan, dalam format YYYYMM, misal 202005 untuk bulan Mei 2020. Gunakan 0 untuk tidak menggunakan filter bulan.
         * @apiParam {String} toMonth               Filter untuk bulan, dalam format YYYYMM, misal 202007 untuk bulan Juli 2020. Gunakan 0 untuk tidak menggunakan filter bulan.
         * @apiParam {String} tribeFilter           0 untuk tidak menggunakan filter, atau comma-separated values dari tribeId, misal 1,3.
         * @apiParam {String} segmentFilter         0 untuk tidak menggunakan filter, atau comma-separated values dari segmentId, misal 2,3.
         * @apiParam {String} rmFilter              0 untuk tidak menggunakan filter, atau comma-separated values dari userId dari RM, misal 4,7. Perhatikan id dan userId itu berbeda.
         * @apiParam {Number} page                  Halaman yang ditampilkan.
         * @apiParam {Number} perPage               Jumlah data per halaman.
         * @apiParam {String} search                Tanda bintang (*) untuk tidak menggunakan search, atau kata yang mau d-search.
         * 
         * @apiSuccessExample Success-Response:
         *   [
         *       {
         *           "dealId": 10,
         *           "dealName": "Lost Deal",
         *           "clientId": 29,
         *           "clientName": "3M INDONESIA, PT (MINNESOTA MINING MANUFACTURING)",
         *           "probability": 10,
         *           "age": 0,
         *           "stage": 1,
         *           "dealDate": "2020-06-24T00:00:00",
         *           "proposalValue": 0,
         *           "rms": [
         *               {
         *                   "Id": "22",
         *                   "Text": "Ayu Retno",
         *               }
         *           ]
         *       }
         *   ]
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("lostdeal/{fromMonth}/{toMonth}/{tribeFilter}/{segmentFilter}/{rmFilter}/{page}/{perPage}/{search}")]
        public async Task<ActionResult<LostDealResponse>> GetLostDealByMonth(string fromMonth, string toMonth, string tribeFilter, string segmentFilter, string rmFilter, int page, int perPage, string search)
        {
            CrmDealState state = GetDealState("lost");
            DateTime now = DateTime.Now;

            CrmDealRole role = GetDealRole("rm");

            string selectSql = "SELECT DISTINCT deal.Id AS DealId, deal.Name AS DealName, client.Id AS ClientId, client.Company AS ClientName, deal.Probability AS Probability, DATEDIFF(day, deal.DealDate, GETDATE()) AS Age, deal.StageId AS Stage, deal.DealDate, ISNULL(proposal.ProposalValue, 0 ) As ProposalValue";
            string fromSql = "FROM dbo.CrmDeals AS deal";
            string joinSql0 = "JOIN dbo.CrmClients AS client ON deal.ClientId = client.Id";
            string joinSql0b = "JOIN dbo.CrmDealTribes AS dealTribe ON deal.Id = dealTribe.DealId";
            string joinSql1 = "JOIN dbo.CoreTribes AS tribe ON dealTribe.TribeId = tribe.Id ";
            string joinSql2 = "JOIN dbo.CrmSegments AS segment ON deal.SegmentId = segment.Id ";
            string joinSql2b = "LEFT JOIN dbo.CrmDealProposals AS proposal ON deal.Id = proposal.DealId AND proposal.IsActive = 1";
            string orderBy = "";

            string whereFromMonth = "'19000101'";
            string whereToMonth = "'21001231'";

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
                return BadRequest(new { error = "Time format error" });
            }

            string whereMonthClause = string.Join("", new[] { "(deal.DealDate >= ", whereFromMonth, " AND deal.DealDate <= ", whereToMonth, ")" }); // "deal.DealDate BETWEEN { ts '2008-12-20 00:00:00'} AND { ts '2008-12-20 23:59:59'}";
            List<string> wheres = new List<string>();
            wheres.Add(string.Join(" ", new[] { "WHERE", whereMonthClause, "AND deal.IsDeleted = 0", "AND", "deal.StateId", "=", state.Id.ToString() }));
            wheres.Add(CreateWhereClause(tribeFilter, "AND ", "tribe.Id", "OR"));
            wheres.Add(CreateWhereClause(segmentFilter, "AND ", "segment.Id", "OR"));

            if (!search.Trim().Equals("*"))
            {
                wheres.Add(string.Join("", new[] { " AND (deal.Name LIKE ", "'%", search.Trim(), "%' OR client.Company LIKE ", "'%", search.Trim(), "%')" }));
            }

            string joinSql3 = "";
            if (!rmFilter.Equals("0"))
            {
                joinSql3 = "JOIN dbo.CrmDealInternalMembers AS member ON deal.Id = member.DealId";
                wheres.Add(string.Join(" ", new[] { "AND", "member.RoleId", "=", role.Id.ToString() }));
                wheres.Add(CreateWhereClause(rmFilter, "AND ", "member.UserId", "OR"));
            }
            string whereSql = string.Join(" ", wheres);
            string sql = string.Join(" ", new[] { selectSql, fromSql, joinSql0, joinSql0b, joinSql1, joinSql2, joinSql2b, joinSql3, whereSql, orderBy });

            int total = _context.LostDealItems.FromSql(sql).Count();

            List<LostDealItem> items = await _context.LostDealItems.FromSql(sql).Skip(perPage * (page - 1)).Take(perPage).ToListAsync<LostDealItem>();

            foreach (LostDealItem item in items)
            {
                var rmq = from member in _context.CrmDealInternalMembers
                          join user in _context.Users
                          on member.UserId equals user.ID
                          where member.DealId == item.DealId && member.RoleId == role.Id && !member.IsDeleted
                          select new GenericInfo()
                          {
                              Id = member.UserId,
                              Text = user.FirstName
                          };

                item.Rms = await rmq.ToListAsync();
            }

            LostDealResponse response = new LostDealResponse();
            response.items = items;
            response.info = new PaginationInfo(page, perPage, total);

            return response;
        }

        /**
         * @api {get} /pipeline/invoice/{fromMonth}/{toMonth}/{tribeFilter}/{segmentFilter}/{rmFilter}/{tobe} GET invoice filter period
         * @apiVersion 1.0.0
         * @apiName GetInvoiceByMonth
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {String} fromMonth             Filter untuk bulan, dalam format YYYYMM, misal 202005 untuk bulan Mei 2020. Gunakan 0 untuk tidak menggunakan filter bulan. Jika tidak menggunakan filter, default adalah bulan Januari dari tahun ybs. fromMonth tidak boleh sebelum bulan Jan dari tahun yang bersangkutan.
         * @apiParam {String} toMonth               Filter untuk bulan, dalam format YYYYMM, misal 202007 untuk bulan Juli 2020. Gunakan 0 untuk tidak menggunakan filter bulan.
         * @apiParam {String} tribeFilter           0 untuk tidak menggunakan filter, atau comma-separated values dari tribeId, misal 1,3.
         * @apiParam {String} segmentFilter         0 untuk tidak menggunakan filter, atau comma-separated values dari segmentId, misal 2,3.
         * @apiParam {String} rmFilter              0 untuk tidak menggunakan filter, atau comma-separated values dari userId dari RM, misal 4,7. Perhatikan id dan userId itu berbeda.
         * @apiParam {Number} tobe                  1 untuk to be invoiced, 0 untuk invoiced
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "stages": [
         *           {
         *               "stage": 1,
         *               "month": 2,
         *               "year": 2020,
         *               "text": "Jan - Feb 2020"
         *           },
         *           {
         *               "stage": 2,
         *               "month": 3,
         *               "year": 2020,
         *               "text": "March"
         *           },
         *           {
         *               "stage": 3,
         *               "month": 4,
         *               "year": 2020,
         *               "text": "April"
         *           },
         *           {
         *               "stage": 4,
         *               "month": 5,
         *               "year": 2020,
         *               "text": "May"
         *           },
         *           {
         *               "stage": 5,
         *               "month": 6,
         *               "year": 2020,
         *               "text": "June"
         *           }
         *       ],
         *       "invoice": [
         *           {
         *               "invoiceId": 14,
         *               "dealId": 8,
         *               "dealName": "Jasa Penyedia Fasilitator Pelatihan CSEP",
         *               "clientId": 1737,
         *               "clientName": "BPJS KESEHATAN",
         *               "segment": {
         *                   "id": 3,
         *                   "text": "Private"
         *               }
         *               "invoiceDate": "2020-07-31T00:00:00",
         *               "month": 7,
         *               "year": 2020,
         *               "stage": 5,
         *               "amount": 45000000,
         *               "remarks": null,
         *               "rms": [
         *                   {
         *                       "id": 6,
         *                       "text": "Muhammad Iman Rizki"
         *                   },
         *                   {
         *                       "id": 4,
         *                       "text": "Grace Louise Harsa"
         *                   }
         *               ],
         *               "access": [
         *                   {
         *                        "id": 27,
         *                        "text": "Laura Theresia Purba"
         *                   }
         *               ]
         *           }
         *       ]
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("invoice/{fromMonth}/{toMonth}/{tribeFilter}/{segmentFilter}/{rmFilter}/{tobe}")]
        public async Task<ActionResult<InvoiceResponse>> GetInvoiceByMonth(string fromMonth, string toMonth, string tribeFilter, string segmentFilter, string rmFilter, int tobe)
        {
            CrmDealRole rm = GetDealRole("rm");

            InvoiceResponse response = new InvoiceResponse();

            string selectSql = "SELECT DISTINCT invoice.id as InvoiceId, deal.Id AS DealId, deal.Name AS DealName, client.Id AS ClientId, client.Company AS ClientName, ISNULL(invoice.Amount, 0 ) As Amount, invoice.invoiceDate, invoice.Remarks, MONTH(invoice.InvoiceDate) as Month, YEAR(invoice.InvoiceDate) as Year, MONTH(invoice.InvoiceDate) as Stage, invoice.OriginalFilename as Filename, branch.Id as BranchId, branch.Branch as BranchName, segment.Id as SegmentId, segment.Segment as SegmentName ";
            string fromSql = "FROM dbo.CrmDealInvoices AS invoice";
            string joinSql2 = "JOIN dbo.CrmDeals as deal ON invoice.DealId = deal.Id";
            string joinSql3 = "JOIN dbo.CrmClients AS client ON deal.ClientId = client.Id";
            string joinSql3b = "JOIN dbo.CrmDealTribes AS dealTribe ON dealTribe.DealId = deal.Id ";

            if (tobe == 0)
            {
                joinSql3b = "JOIN dbo.CrmDealTribeInvoices AS dealTribe ON dealTribe.InvoiceId = invoice.Id ";
            }
            string joinSql4 = "JOIN dbo.CoreTribes AS tribe ON tribe.Id = dealTribe.TribeId";
            string joinSql5 = "JOIN dbo.CrmSegments AS segment ON deal.SegmentId = segment.Id ";
            string joinSql7 = "JOIN dbo.CrmBranches AS branch ON deal.BranchId = branch.Id ";
            string orderBy = "ORDER BY Year, Month";

            if (tobe == 0)
            {
                orderBy = "ORDER BY Year desc, Month desc";
            }

            string whereFromMonth = string.Join("", new[] { "'", DateTime.Now.Year.ToString(), "0101", "'" });
            string whereToMonth = "'21001231'";

            // update 2022_05_25
            int lastmonth = 0;
            int lastyear = 0;

            try
            {
                if (!fromMonth.Trim().Equals("0") /*&& Int32.Parse(fromMonth) > DateTime.Today.Year * 100*/ )
                {
                    whereFromMonth = string.Join("", new[] { "'", fromMonth, "01", "'" });
                }
                if (!toMonth.Trim().Equals("0"))
                {
                    string year = toMonth.Substring(0, 4);
                    string month = toMonth.Substring(4);

                    lastmonth = Int16.Parse(month);
                    lastyear = Int16.Parse(year);

                    int nDay = DateTime.DaysInMonth(Int32.Parse(year), Int32.Parse(month));
                    whereToMonth = string.Join("", new[] { "'", toMonth, nDay.ToString(), "'" });
                }
            }
            catch
            {
                return BadRequest(new { error = "Time format error" });
            }

            string whereMonthClause = string.Join("", new[] { "(invoice.InvoiceDate >= ", whereFromMonth, " AND invoice.InvoiceDate <= ", whereToMonth, ")" }); // "deal.DealDate BETWEEN { ts '2008-12-20 00:00:00'} AND { ts '2008-12-20 23:59:59'}";
            List<string> wheres = new List<string>();
            wheres.Add(string.Join(" ", new[] { "WHERE", whereMonthClause, "AND deal.IsDeleted = 0 AND invoice.IsDeleted = 0 AND invoice.IsToBe = ", tobe.ToString() }));
            wheres.Add(CreateWhereClause(tribeFilter, "AND ", "tribe.Id", "OR"));
            wheres.Add(CreateWhereClause(segmentFilter, "AND ", "segment.Id", "OR"));

            string joinSql6 = "";
            if (!rmFilter.Equals("0"))
            {
                if (tobe == 1)
                {
                    CrmDealRole role = GetDealRole("rm");
                    joinSql6 = "JOIN dbo.CrmDealInternalMembers AS member ON deal.Id = member.DealId";
                    wheres.Add(string.Join(" ", new[] { "AND", "member.RoleId", "=", role.Id.ToString() }));
                    wheres.Add(CreateWhereClause(rmFilter, "AND ", "member.UserId", "OR"));
                }
                else
                {
                    joinSql6 = "JOIN dbo.CrmDealUserInvoices AS member ON invoice.Id = member.InvoiceId";
                    wheres.Add(CreateWhereClause(rmFilter, "AND ", "member.UserId", "OR"));
                }
            }
            string whereSql = string.Join(" ", wheres);
            string sql = string.Join(" ", new[] { selectSql, fromSql, joinSql2, joinSql3, joinSql3b, joinSql4, joinSql5, joinSql7, joinSql6, whereSql, orderBy });

            List<InvoiceItem> items = await _context.InvoiceItems.FromSql(sql).ToListAsync<InvoiceItem>();

            if (tobe == 1)
            {
                selectSql = "SELECT TOP(1) MONTH(invoice.InvoiceDate) AS Month, YEAR(invoice.InvoiceDate) as Year, deal.StageId as Stage, deal.Name as Text";   // Stage and Text are not used
                orderBy = "ORDER BY Year desc, Month desc";
                sql = string.Join(" ", new[] { selectSql, fromSql, joinSql2, joinSql3, joinSql3b, joinSql4, joinSql5, joinSql6, whereSql, orderBy });

                var clast = _context.MonthStageInfos.FromSql(sql).Count(a => a.Month > 0);

                MonthStageInfo last = null;
                if (clast != 0)
                {
                    last = _context.MonthStageInfos.FromSql(sql).First();
                }


                orderBy = "ORDER BY Year, Month";
                sql = string.Join(" ", new[] { selectSql, fromSql, joinSql2, joinSql3, joinSql3b, joinSql4, joinSql5, joinSql6, whereSql, orderBy });

                var co = _context.MonthStageInfos.FromSql(sql).Count(a => a.Month > 0);

                int minMonth = 0;
                int minYear = 0;

                if (co == 0)
                {
                    DateTime now = DateTime.Now;
                    response.Stages = GetMonthStages(now.Month, now.Year, 5, last);
                    minMonth = now.Month;
                    minYear = now.Year;
                }
                else
                {
                    var fi = _context.MonthStageInfos.FromSql(sql).First();
                    response.Stages = GetMonthStages(fi.Month, fi.Year, 5, last);
                    minMonth = fi.Month;
                    minYear = fi.Year;
                }

                foreach (InvoiceItem item in items)
                {
                    if (item.Year == minYear)
                    {
                        item.Stage = (item.Month - minMonth) + 1;
                    }
                    else
                    {
                        int i = (item.Year - minYear) * 12;
                        item.Stage = ((item.Month + i) - minMonth) + 1;
                    }

                    if (item.Stage > 5) item.Stage = 5;


                    InvoiceItemResponse itemResponse = new InvoiceItemResponse()
                    {
                        InvoiceId = item.InvoiceId,
                        DealId = item.DealId,
                        DealName = item.DealName,
                        InvoiceDate = item.InvoiceDate,
                        Month = item.Month,
                        Year = item.Year,
                        Stage = item.Stage,
                        Amount = item.Amount,
                        Filename = item.Filename,
                        Remarks = item.Remarks
                    };

                    itemResponse.Segment.Id = item.SegmentId;
                    itemResponse.Segment.Text = item.SegmentName;

                    itemResponse.Client.Id = item.ClientId;
                    itemResponse.Client.Text = item.ClientName;
                    itemResponse.Branch.Id = item.BranchId;
                    itemResponse.Branch.Text = item.BranchName;

                    itemResponse.Pic = GetInternalMember(item.DealId, "pic");
                    itemResponse.Rms = await GetInternalMembers(item.DealId, rm.Id);
                    itemResponse.Access = GetAccess(itemResponse.Rms);
                    itemResponse.Tribes = await GetTribeMembers(item.DealId);

                    response.Invoice.Add(itemResponse);
                }
            }
            else
            {
                // update 2022_05-25
                int curMonth = DateTime.Today.Month;
                int curYear = DateTime.Today.Year;

                if (lastmonth != 0 && lastyear != 0)
                {
                    curMonth = lastmonth;
                    curYear = lastyear;
                }
                MonthStageInfo[] infos = new MonthStageInfo[5];

                for (int i = 5; i > 0; i--)
                {
                    MonthStageInfo info = new MonthStageInfo()
                    {
                        Stage = i,
                        Month = curMonth,
                        Year = curYear,
                        Text = String.Format("{0:MMMM}", new DateTime(curYear, curMonth, 1))
                    };
                    infos[i - 1] = info;
                    if (i > 1)
                    {
                        curMonth--;
                        if (curMonth == 0)
                        {
                            curMonth = 12;
                            curYear--;
                        }
                    }
                }

                if (curMonth > 1)
                {
                    string fromStr = String.Format("{0:MMM}", new DateTime(curYear, 1, 1));
                    string toStr = String.Format("{0:MMM}", new DateTime(curYear, curMonth, 1));
                    infos[0].Text = string.Join(" - ", new[] { fromStr, toStr });
                }

                response.Stages = infos.ToList<MonthStageInfo>();

                foreach (InvoiceItem item in items)
                {
                    if (item.Month <= curMonth && item.Year == curYear)
                    {
                        item.Stage = 1;
                    }
                    else
                    {
                        if (item.Year == curYear)
                        {
                            item.Stage = item.Month - curMonth + 1;
                        }
                        else
                        {
                            int t = item.Stage + 13;
                            item.Stage = t - curMonth;
                        }
                    }
                    if (item.Stage > 5) item.Stage = 5;

                    InvoiceItemResponse itemResponse = new InvoiceItemResponse()
                    {
                        InvoiceId = item.InvoiceId,
                        DealId = item.DealId,
                        DealName = item.DealName,
                        InvoiceDate = item.InvoiceDate,
                        Month = item.Month,
                        Year = item.Year,
                        Stage = item.Stage,
                        Amount = item.Amount,
                        Filename = item.Filename,
                        Remarks = item.Remarks
                    };
                    itemResponse.Client.Id = item.ClientId;
                    itemResponse.Client.Text = item.ClientName;

                    itemResponse.Branch.Id = item.BranchId;
                    itemResponse.Branch.Text = item.BranchName;

                    itemResponse.Segment.Id = item.SegmentId;
                    itemResponse.Segment.Text = item.SegmentName;

                    GenericInfo info = GetInvoiceContact(item.InvoiceId);

                    if (info != null)
                    {
                        itemResponse.Contact.Id = info.Id;
                        itemResponse.Contact.Text = info.Text;
                    }

                    itemResponse.Pic = GetInternalMember(item.DealId, "pic");
                    itemResponse.Rms = await GetInternalMembers(item.DealId, rm.Id);
                    itemResponse.Access = GetAccess(itemResponse.Rms);
                    itemResponse.Tribes = await GetTribeMembers(item.DealId);

                    response.Invoice.Add(itemResponse);


                }
            }

            return response;
        }



        /**
         * @api {get} /pipeline/summary/{group1}/{group2}/{group3}/{tribeFilter}/{segmentFilter}/{rmFilter} GET summary
         * @apiVersion 1.0.0
         * @apiName GetSummary
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {String} group1                Probability yang masuk group 1, comma-separated, misal 10,20
         * @apiParam {String} group2                Probability yang masuk group 1, comma-separated, misal 30,40,50
         * @apiParam {String} group3                Probability yang masuk group 1, comma-separated, misal 60,70,80
         * @apiParam {String} tribeFilter           0 untuk tidak menggunakan filter, atau comma-separated values dari tribeId, misal 1,3.
         * @apiParam {String} segmentFilter         0 untuk tidak menggunakan filter, atau comma-separated values dari segmentId, misal 2,3.
         * @apiParam {String} rmFilter              0 untuk tidak menggunakan filter, atau comma-separated values dari userId dari RM, misal 4,7. Perhatikan id dan userId itu berbeda.
         * 
         * @apiSuccessExample Success-Response:
         *   [
         *       {
         *           "amount": 100000000
         *       },
         *       {
         *           "amount": 0
         *       },
         *       {
         *           "amount": 0
         *       }
         *   ]
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("summary/{group1}/{group2}/{group3}/{tribeFilter}/{segmentFilter}/{rmFilter}")]
        public async Task<ActionResult<List<SummaryItem>>> GetSummary(string group1, string group2, string group3, string tribeFilter, string segmentFilter, string rmFilter)
        {
            List<SummaryItem> response = new List<SummaryItem>();

            CrmDealState state = GetDealState("open");

            SummaryItem item1 = GetSummaryItem(tribeFilter, segmentFilter, rmFilter, group1, state.Id);
            SummaryItem item2 = GetSummaryItem(tribeFilter, segmentFilter, rmFilter, group2, state.Id);
            SummaryItem item3 = GetSummaryItem(tribeFilter, segmentFilter, rmFilter, group3, state.Id);

            response.AddRange(new[] { item1, item2, item3 });

            return response;
        }

        /**
         * @api {get} /pipeline/summary/{fromMonth}/{toMonth}/{group1}/{group2}/{group3}/{tribeFilter}/{segmentFilter}/{rmFilter} GET summary filter period
         * @apiVersion 1.0.0
         * @apiName GetSummaryByMonth
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {String} fromMonth             Filter untuk bulan, dalam format YYYYMM, misal 202005 untuk bulan Mei 2020. Gunakan 0 untuk tidak menggunakan filter bulan.
         * @apiParam {String} toMonth               Filter untuk bulan, dalam format YYYYMM, misal 202007 untuk bulan Juli 2020. Gunakan 0 untuk tidak menggunakan filter bulan.
         * @apiParam {String} group1                Probability yang masuk group 1, comma-separated, misal 10,20
         * @apiParam {String} group2                Probability yang masuk group 2, comma-separated, misal 30,40,50
         * @apiParam {String} group3                Probability yang masuk group 3, comma-separated, misal 60,70,80
         * @apiParam {String} tribeFilter           0 untuk tidak menggunakan filter, atau comma-separated values dari tribeId, misal 1,3.
         * @apiParam {String} segmentFilter         0 untuk tidak menggunakan filter, atau comma-separated values dari segmentId, misal 2,3.
         * @apiParam {String} rmFilter              0 untuk tidak menggunakan filter, atau comma-separated values dari userId dari RM, misal 4,7. Perhatikan id dan userId itu berbeda.
         * 
         * @apiSuccessExample Success-Response:
         *   [
         *       {
         *           "amount": 100000000
         *       },
         *       {
         *           "amount": 0
         *       },
         *       {
         *           "amount": 0
         *       }
         *   ]
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("summary/{fromMonth}/{toMonth}/{group1}/{group2}/{group3}/{tribeFilter}/{segmentFilter}/{rmFilter}")]
        public async Task<ActionResult<List<SummaryItem>>> GetSummaryByMonth(string fromMonth, string toMonth, string group1, string group2, string group3, string tribeFilter, string segmentFilter, string rmFilter)
        {
            List<SummaryItem> response = new List<SummaryItem>();

            CrmDealState state = GetDealState("open");

            SummaryItem item1 = GetSummaryItemByMonth(fromMonth, toMonth, tribeFilter, segmentFilter, rmFilter, group1, state.Id);
            SummaryItem item2 = GetSummaryItemByMonth(fromMonth, toMonth, tribeFilter, segmentFilter, rmFilter, group2, state.Id);
            SummaryItem item3 = GetSummaryItemByMonth(fromMonth, toMonth, tribeFilter, segmentFilter, rmFilter, group3, state.Id);

            response.AddRange(new[] { item1, item2, item3 });

            return response;
        }

        /**
         * @api {get} /pipeline/drop/{dealId}/{userId}/{stage} GET drop
         * @apiVersion 1.0.0
         * @apiName GetDropStage
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} dealId           Id dari deal yang dipindahkan ke stage lain
         * @apiParam {Number} userId           Id dari user yang melakukan
         * @apiParam {Number} stage            Stage yang jadi target drop, 1 untuk Lead In, 2 untuk Proposal Development, 3 untuk Proposal Sent, 4 untuk Presentation, dan 5 untuk Negotiation
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "dealId": 1,
         *       "dealName": "Pengembangan Budaya Organisasi",
         *       "clientId": 2,
         *       "clientName": "PT Bank Mandiri Tbk.",
         *       "probability": 10,
         *       "age": 4,
         *       "stage": 2,
         *       "dealDate": "2020-05-20T00:00:00"
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("drop/{dealId}/{userId}/{stage}")]
        public async Task<ActionResult<PipelineItem>> GetDropStage(int dealId, int userId, int stage)
        {
            CrmDeal deal = _context.CrmDeals.Find(dealId);
            if (deal == null || deal.Id == 0)
            {
                return NotFound();
            }

            DateTime now = DateTime.Now;
            int fromStage = deal.StageId;

            if (fromStage == stage)
            {
                return BadRequest(new { error = "New stage is the same with the old stage" });
            }

            deal.StageId = stage;
            deal.LastUpdated = now;
            deal.LastUpdatedBy = userId;

            int newProb = 0;
            switch (stage)
            {
                case 1:
                    newProb = 10;
                    break;
                case 2:
                    newProb = 20;
                    break;
                case 3:
                    newProb = 40;
                    break;
                case 4:
                    newProb = 50;
                    break;
                default:
                    newProb = 70;
                    break;
            }
            if (deal.Probability < newProb)
            {
                await AddDealHistory("prob", dealId, deal.Probability.ToString(), newProb.ToString(), now, userId, now, userId, "Probability:", deal.Probability.ToString(), newProb.ToString(), 0, 0, 0);
                deal.Probability = newProb;
            }

            _context.Entry(deal).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            CrmDealStage fromStageDb = _context.CrmDealStages.Find(fromStage);
            CrmDealStage toStageDb = _context.CrmDealStages.Find(stage);

            await AddDealHistory("stage", dealId, fromStage.ToString(), stage.ToString(), now, userId, now, userId, "Stage:", fromStageDb.Stage, toStageDb.Stage, 0, fromStageDb.Id, toStageDb.Id);

            var query = from d in _context.CrmDeals
                        join client in _context.CrmClients
                        on d.ClientId equals client.Id
                        where d.Id == dealId
                        select new PipelineItem()
                        {
                            DealId = d.Id,
                            DealName = d.Name,
                            ClientId = client.Id,
                            ClientName = client.Company,
                            Probability = d.Probability,
                            Age = (DateTime.Now - d.DealDate).Days,
                            Stage = d.StageId,
                            DealDate = d.DealDate
                        };

            return query.FirstOrDefault();
        }

        /**
         * @api {get} /pipeline/projection/drop/{dealId}/{invoiceId}/{month}/{year}/{userId} GET drop projection
         * @apiVersion 1.0.0
         * @apiName GetDropProjection
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} dealId             Id dari deal yang bersangkutan
         * @apiParam {Number} invoiceId          Id dari invoice yang dipindahkan 
         * @apiParam {Number} month              Bulan dalam bentuk angka
         * @apiParam {Number} year               Tahun dalam bentuk angka
         * @apiParam {Number} userId             Id dari user yang melakukan
         * 
         * @apiSuccessExample Success-Response:
         *   NoRespond
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("projection/drop/{dealId}/{invoiceId}/{month}/{year}/{userId}")]
        public async Task<ActionResult> GetDropProjection(int dealId, int invoiceId, int month, int year, int userId)
        {
            CrmDealProposalInvoice invoice = _context.CrmDealProposalInvoices.Find(invoiceId);
            if (invoice == null || invoice.Id == 0)
            {
                return NotFound();
            }

            DateTime oldDate = invoice.InvoiceDate;
            DateTime invoiceDate = new DateTime(year, month, 1);
            int periodId = GetPeriodId(invoiceDate, userId);
            if (periodId == invoice.PeriodId)
            {
                return BadRequest(new { error = "New period is the same with the old period" });
            }

            DateTime now = DateTime.Now;

            invoice.PeriodId = periodId;
            invoice.InvoiceDate = invoiceDate;
            invoice.LastUpdated = now;
            invoice.LastUpdatedBy = userId;
            _context.Entry(invoice).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            await AddDealHistory("invoice", dealId, invoiceId.ToString(), invoiceDate.ToShortDateString(), now, userId, now, userId, "Invoice period changed from ", string.Join(" ", new[] { oldDate.ToString("MMMM", CultureInfo.CreateSpecificCulture("en")), oldDate.Year.ToString() }), string.Join(" ", new[] { invoiceDate.ToString("MMMM", CultureInfo.CreateSpecificCulture("en")), invoiceDate.Year.ToString() }), 0, 0, 0);

            return NoContent();
        }


        /**
         * @api {get} /pipeline/state/{dealId}/{userId}/{state} GET change state
         * @apiVersion 1.0.0
         * @apiName GetChangeState
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} dealId           Id dari deal yang dipindahkan ke stage lain
         * @apiParam {Number} userId           Id dari user yang melakukan
         * @apiParam {Number} state            State yang baru, 1 untuk Open, 2 untuk Won, 3 untuk Lost, 4 untuk Reopen
         *  
         * @apiSuccessExample Success-Response:
         *   {
         *       "dealId": 1,
         *       "dealName": "Pengembangan Budaya Organisasi",
         *       "clientId": 2,
         *       "clientName": "PT Bank Mandiri Tbk.",
         *       "probability": 10,
         *       "age": 4,
         *       "stage": 2,
         *       "dealDate": "2020-05-20T00:00:00"
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("state/{dealId}/{userId}/{state}")]
        public async Task<ActionResult<PipelineItem>> GetChangeState(int dealId, int userId, int state)
        {
            // TODO Funtion ini harus diupdate sewaktu mengerjakan Lost Deal dan Invoice. 
            // Begitu pindah ke state WON, langsung masuk ke to be invoice.
            // Begitu masuk ke state LOST, langsung masuk ke Lost Deal.
            CrmDeal deal = _context.CrmDeals.Find(dealId);
            if (deal == null || deal.Id == 0)
            {
                return NotFound();
            }

            if (state == 4) state = 1;

            DateTime now = DateTime.Now;
            int fromState = deal.StateId;

            if (fromState == state)
            {
                return BadRequest(new { error = "New state is the same with the old state" });
            }

            deal.StateId = state;
            deal.LastUpdated = now;
            deal.LastUpdatedBy = userId;
            _context.Entry(deal).State = EntityState.Modified;
            _context.SaveChanges();

            CrmDealState fromStateDb = _context.CrmDealStates.Find(fromState);
            CrmDealState toStateDb = _context.CrmDealStates.Find(state);

            CrmDealState wonState = GetDealState("won");
            if (wonState.Id == state)
            {
                deal.Probability = 90;
                await AddToToBeInvoicedAsync(deal, now, userId);
            }

            await AddDealHistory("stage", dealId, fromState.ToString(), state.ToString(), now, userId, now, userId, "State:", fromStateDb.State, toStateDb.State, 0, fromStateDb.Id, toStateDb.Id);

            var query = from d in _context.CrmDeals
                        join client in _context.CrmClients
                        on d.ClientId equals client.Id
                        where d.Id == dealId
                        select new PipelineItem()
                        {
                            DealId = d.Id,
                            DealName = d.Name,
                            ClientId = client.Id,
                            ClientName = client.Company,
                            Probability = d.Probability,
                            Age = (DateTime.Now - d.DealDate).Days,
                            Stage = d.StageId,
                            DealDate = d.DealDate
                        };



            return query.FirstOrDefault();
        }

        /**
         * @api {get} /pipeline/probability/{dealId}/{userId}/{probability} GET change probability
         * @apiVersion 1.0.0
         * @apiName GetChangeProbability
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} dealId           Id dari deal yang dipindahkan ke stage lain
         * @apiParam {Number} userId           Id dari user yang melakukan
         * @apiParam {Number} probability      Probability yang baru, seperti 10, 20, dst. 
         *  
         * @apiSuccessExample Success-Response:
         *   {
         *       "dealId": 1,
         *       "dealName": "Pengembangan Budaya Organisasi",
         *       "clientId": 2,
         *       "clientName": "PT Bank Mandiri Tbk.",
         *       "probability": 10,
         *       "age": 4,
         *       "stage": 2,
         *       "dealDate": "2020-05-20T00:00:00"
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("probability/{dealId}/{userId}/{probability}")]
        public async Task<ActionResult<PipelineItem>> GetChangeProbability(int dealId, int userId, int probability)
        {
            // TODO Funtion ini harus diupdate sewaktu mengerjakan Lost Deal dan Invoice
            CrmDeal deal = _context.CrmDeals.Find(dealId);
            if (deal == null || deal.Id == 0)
            {
                return NotFound();
            }

            int originalStage = deal.StageId;

            DateTime now = DateTime.Now;
            int fromProbability = deal.Probability;

            if (probability <= 10)
            {
                deal.StageId = 1;
            }
            else if (probability <= 30)
            {
                deal.StageId = 2;
            }
            else if (probability <= 40)
            {
                deal.StageId = 3;
            }
            else if (probability <= 60)
            {
                deal.StageId = 4;
            }
            else
            {
                deal.StageId = 5;
            }

            deal.Probability = probability;
            deal.LastUpdated = now;
            deal.LastUpdatedBy = userId;
            _context.Entry(deal).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            await AddDealHistory("prob", dealId, fromProbability.ToString(), probability.ToString(), now, userId, now, userId, "Probability:", fromProbability.ToString(), probability.ToString(), 0, 0, 0);

            if (originalStage != deal.StageId)
            {
                CrmDealStage fromStageDb = _context.CrmDealStages.Find(originalStage);
                CrmDealStage toStageDb = _context.CrmDealStages.Find(deal.StageId);

                await AddDealHistory("stage", dealId, originalStage.ToString(), deal.StageId.ToString(), now, userId, now, userId, "Stage:", fromStageDb.Stage, toStageDb.Stage, 0, fromStageDb.Id, toStageDb.Id);
            }

            var query = from d in _context.CrmDeals
                        join client in _context.CrmClients
                        on d.ClientId equals client.Id
                        where d.Id == dealId
                        select new PipelineItem()
                        {
                            DealId = d.Id,
                            DealName = d.Name,
                            ClientId = client.Id,
                            ClientName = client.Company,
                            Probability = d.Probability,
                            Age = (DateTime.Now - d.DealDate).Days,
                            Stage = d.StageId,
                            DealDate = d.DealDate
                        };

            return query.FirstOrDefault();
        }

        /**
         * @api {post} /pipeline/proposal POST proposal
         * @apiVersion 1.0.0
         * @apiName PostProposal
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "id": 0,
         *     "userId": 1,
         *     "dealId": 1,
         *     "sentById": 3,
         *     "typeId": 1,
         *     "sentDate": "2020-05-22",
         *     "contactIds": [
         *       3,4
         *     ],
         *     "filename": "proposal.pdf",
         *     "fileBase64": "isi file dalam format base64",
         *     "proposalValue": 100000000,
         *     "invoices": [
         *       {
         *         "id": 0,
         *         "invoiceDate": "2020-07-01",
         *         "invoiceAmount": 60000000,
         *         "remarks": "Termin 1"
         *       },
         *       {
         *         "id": 0,
         *         "invoiceDate": "2020-09-01",
         *         "invoiceAmount": 40000000,
         *         "remarks": "Termin 2"
         *       }
         *     ]
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   {
         *     "id": 11,
         *     "proposalType": {
         *       "id": 1,
         *       "text": "Workshop"
         *     },
         *     "filename": "proposal.pdf",
         *     "url": "/download/deal/4/xe4c1px2.kn4.pptx/proposal.pptx",
         *     "proposalValue": 100000000,
         *     "invoices": [
         *       {
         *         "id": 16,
         *         "invoiceDate": "2020-07-01T00:00:00.000Z",
         *         "invoiceAmount": 60000000,
         *         "remarks": "Termin 1"
         *       },
         *       {
         *         "id": 17,
         *         "invoiceDate": "2020-09-01T00:00:00.000Z",
         *         "invoiceAmount": 40000000,
         *         "remarks": "Termin 2"
         *       }
         *     ],
         *     "errors": [
         *       {
         *         "code": "ok",
         *         "description": ""
         *       }
         *     ]
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost("proposal")]
        public async Task<ActionResult<PostProposalResponse>> PostProposal(PostProposalRequest request)
        {
            if (!DealExists(request.DealId))
            {
                return NotFound();
            }

            List<Error> errors = new List<Error>();

            DateTime now = DateTime.Now;

            CrmDealProposal proposal = new CrmDealProposal();
            try
            {

                CrmDealProposal curProp = _context.CrmDealProposals.Where(a => a.DealId == request.DealId && a.IsActive).FirstOrDefault();
                if (curProp != null && curProp.Id > 0)
                {
                    curProp.IsActive = false;
                    curProp.LastUpdated = now;
                    curProp.LastUpdatedBy = request.UserId;

                    _context.Entry(curProp).State = EntityState.Modified;
                }

                proposal.ProposalValue = request.ProposalValue;
                proposal.PeriodId = GetPeriodId(request.SentDate, request.UserId);
                proposal.DealId = request.DealId;
                proposal.TypeId = request.TypeId;
                proposal.SentById = request.SentById;
                proposal.SentDate = request.SentDate;
                proposal.Filename = "";
                proposal.OriginalFilename = request.Filename;
                proposal.RootFolder = _options.DataRootDirectory;
                proposal.IsActive = true;
                proposal.CreatedDate = now;
                proposal.CreatedBy = request.UserId;
                proposal.LastUpdated = now;
                proposal.LastUpdatedBy = request.UserId;
                proposal.IsDeleted = false;
                proposal.DeletedBy = 0;

                if (request.FileBase64 == null && curProp != null)
                {
                    proposal.Filename = curProp.Filename;
                    proposal.OriginalFilename = curProp.OriginalFilename;
                }

                _context.CrmDealProposals.Add(proposal);
                await _context.SaveChangesAsync();

            }
            catch
            {
                return BadRequest(new { error = "Error writing to database. Please check the ids." });
            }

            if (request.FileBase64 != null && request.Filename != null)
            {
                var error = SaveFileUpload(request.FileBase64, request.Filename, proposal.DealId);
                if (error.Code.Equals("ok"))
                {
                    string[] names = error.Description.Split(separator);
                    if (names.Length >= 3)
                    {
                        proposal.Filename = names[1];
                        _context.Entry(proposal).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                    }
                    await IntegrateToKc(proposal.Id, request.UserId, now);
                }
                else
                {
                    errors.Add(error);
                }
            }

            var role = GetDealRole("sent");
            foreach (int n in request.ContactIds)
            {
                CrmDealProposalSentContact contact = new CrmDealProposalSentContact()
                {
                    ProposalId = proposal.Id,
                    ContactId = n
                };
                _context.CrmDealProposalSentContacts.Add(contact);

                CrmDealExternalMember curContact = _context.CrmDealExternalMembers.Where(a => a.DealId == request.DealId && a.ContactId == n).FirstOrDefault();
                if (curContact == null || curContact.Id == 0)
                {
                    CrmDealExternalMember member = new CrmDealExternalMember()
                    {
                        DealId = request.DealId,
                        RoleId = role.Id,
                        ContactId = n,
                        CreatedDate = now,
                        CreatedBy = request.UserId,
                        LastUpdated = now,
                        LastUpdatedBy = request.UserId,
                        IsDeleted = false
                    };
                    _context.CrmDealExternalMembers.Add(member);
                }
            }

            foreach (InvoicePeriodInfo invoice in request.Invoices)
            {
                CrmDealProposalInvoice inv = new CrmDealProposalInvoice()
                {
                    ProposalId = proposal.Id,
                    PeriodId = GetPeriodId(invoice.InvoiceDate, request.UserId),
                    InvoiceAmount = invoice.InvoiceAmount,
                    InvoiceDate = invoice.InvoiceDate,
                    Remarks = invoice.Remarks,
                    CreatedDate = now,
                    CreatedBy = request.UserId,
                    LastUpdated = now,
                    LastUpdatedBy = request.UserId,
                    IsDeleted = false,
                    DeletedBy = 0
                };
                _context.CrmDealProposalInvoices.Add(inv);
            }
            await _context.SaveChangesAsync();

            PostProposalResponse response = GetProposalInfo(proposal.Id);
            response.Errors.AddRange(errors);

            await AddDealHistory("prop", request.DealId, "", proposal.Id.ToString(), proposal.SentDate, proposal.SentById, now, request.UserId, "Proposal", "", "", 0, 0, 0);

            return response;
        }

        private async Task<int> IntegrateToKc(int proposalId, int userId, DateTime now)
        {
            var query = from prop in _context.CrmDealProposals
                        join deal in _context.CrmDeals on prop.DealId equals deal.Id
                        join dealTribe in _context.CrmDealTribes on deal.Id equals dealTribe.DealId
                        join tribe in _context.CoreTribes on dealTribe.TribeId equals tribe.Id
                        join client in _context.CrmClients on deal.ClientId equals client.Id
                        where !string.IsNullOrEmpty(prop.Filename) && !prop.IsDeleted && !deal.IsDeleted && prop.Id == proposalId
                        select new
                        {
                            prop.Id,
                            DealId = deal.Id,
                            tribe.Shortname,
                            prop.SentDate,
                            client.Company,
                            DealName = deal.Name,
                            prop.OriginalFilename,
                            prop.Filename,
                            prop.RootFolder
                        };
            var obj = query.FirstOrDefault();
            if (obj != null)
            {
                string fullpath = Path.Combine(new[] { obj.RootFolder, "deal", obj.DealId.ToString(), obj.Filename });
                if (System.IO.File.Exists(fullpath))
                {
                    KmFile rootFolder = _context.KmFiles.Where(a => a.ParentId == 0 && a.Onegml && a.IsFolder && !a.IsDeleted && a.Name.Equals("Pre-sales")).FirstOrDefault();
                    if (rootFolder != null)
                    {
                        KmFile tribeFolder = _context.KmFiles.Where(a => a.ParentId == rootFolder.Id && a.IsFolder && !a.IsDeleted && a.Name.Equals(obj.Shortname.ToUpper())).FirstOrDefault();
                        if (tribeFolder == null)
                        {
                            tribeFolder = GetFolder(obj.Shortname.ToUpper(), rootFolder.Id, obj.RootFolder, now, userId);
                            _context.KmFiles.Add(tribeFolder);
                            _context.SaveChanges();
                        }

                        string year = obj.SentDate.ToString("yyyy");
                        KmFile yearFolder = _context.KmFiles.Where(a => a.ParentId == tribeFolder.Id && a.IsFolder && !a.IsDeleted && a.Name.Equals(year)).FirstOrDefault();
                        if (yearFolder == null)
                        {
                            yearFolder = GetFolder(year, tribeFolder.Id, obj.RootFolder, now, userId);
                            _context.KmFiles.Add(yearFolder);
                            _context.SaveChanges();
                        }

                        KmFile clientFolder = _context.KmFiles.Where(a => a.ParentId == yearFolder.Id && a.IsFolder && !a.IsDeleted && a.Name.Equals(obj.Company)).FirstOrDefault();
                        if (clientFolder == null)
                        {
                            clientFolder = GetFolder(obj.Company, yearFolder.Id, obj.RootFolder, now, userId);
                            _context.KmFiles.Add(clientFolder);
                            _context.SaveChanges();
                        }

                        KmFile dealFolder = _context.KmFiles.Where(a => a.ParentId == clientFolder.Id && a.IsFolder && !a.IsDeleted && a.Name.Equals(obj.DealName)).FirstOrDefault();
                        if (dealFolder == null)
                        {
                            dealFolder = GetFolder(obj.DealName, clientFolder.Id, obj.RootFolder, now, userId);
                            _context.KmFiles.Add(dealFolder);
                            _context.SaveChanges();
                        }

                        KmFile curFile = _context.KmFiles.Where(a => a.ParentId == dealFolder.Id && a.Filename.Equals(obj.Filename)).FirstOrDefault();
                        if (curFile == null)
                        {
                            string ext = Path.GetExtension(fullpath).Substring(1).ToLower();
                            KmFile file = new KmFile()
                            {
                                ParentId = dealFolder.Id,
                                Name = obj.OriginalFilename,
                                Filename = obj.Filename,
                                FileType = ext,
                                IsFolder = false,
                                RootFolder = obj.RootFolder,
                                Description = "",
                                ProjectId = 0,
                                Onegml = true,
                                OwnerId = 0,
                                CreatedBy = userId,
                                CreatedDate = now,
                                LastUpdatedBy = userId,
                                LastUpdated = now,
                                IsDeleted = false,
                                DeletedDate = new DateTime(1970, 1, 1),
                                DeletedBy = 0,
                                Fullpath = fullpath,
                                Extracted = false
                            };
                            _context.KmFiles.Add(file);
                            await _context.SaveChangesAsync();

                            return file.Id;
                        }

                    }
                }
            }

            return 0;
        }

        private KmFile GetFolder(string nm, int parentId, string rootFolder, DateTime now, int userId)
        {
            return new KmFile()
            {
                ParentId = parentId,
                Name = nm,
                Filename = "",
                FileType = "",
                IsFolder = true,
                RootFolder = rootFolder,
                Description = nm,
                ProjectId = 0,
                Onegml = true,
                OwnerId = 2,
                CreatedBy = userId,
                CreatedDate = now,
                LastUpdatedBy = userId,
                LastUpdated = now,
                IsDeleted = false,
                DeletedBy = 0,
                Fullpath = "",
                Extracted = false
            };
        }

        /**
         * @api {post} /pipeline/tobeinvoiced POST to be invoiced
         * @apiVersion 1.0.0
         * @apiName PostToBeInvoiced
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "id": 0,
         *     "clientId": 1737,
         *     "dealId": 8,
         *     "userId": 1,
         *     "picId": 35,
         *     "invoiceDate": "2020-06-24T00:00:00.000Z",
         *     "amount": 120000000,
         *     "remarks": "Termin 1"
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   {
         *       "id": 15,
         *       "dealId": 8,
         *       "amount": 120000000,
         *       "periodId": 6,
         *       "invoiceDate": "2020-06-24T00:00:00Z",
         *       "filename": "",
         *       "originalFilename": "",
         *       "remarks": "Termin 1",
         *       "isToBe": true,
         *       "createdDate": "2020-06-18T08:51:01.9884936+07:00",
         *       "createdBy": 1,
         *       "lastUpdated": "2020-06-18T08:51:01.9884936+07:00",
         *       "lastUpdatedBy": 1,
         *       "isDeleted": false,
         *       "deletedBy": 0,
         *       "deletedDate": null
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost("tobeinvoiced")]
        public async Task<ActionResult<CrmDealInvoice>> PostToBeInvoiced(PostToBeInvoiced request)
        {
            CrmDeal deal = _context.CrmDeals.Find(request.DealId);
            if (deal == null)
            {
                return NotFound(new { error = "Deal not found. Please check dealId" });
            }

            DateTime now = DateTime.Now;

            try
            {
                if (deal.ClientId != request.ClientId)
                {
                    deal.ClientId = request.ClientId;
                    deal.LastUpdated = now;
                    deal.LastUpdatedBy = request.UserId;
                    _context.Entry(deal).State = EntityState.Modified;
                }

                CrmDealInvoice invoice = new CrmDealInvoice()
                {
                    DealId = deal.Id,
                    Amount = request.Amount,
                    PeriodId = GetPeriodId(request.InvoiceDate, request.UserId),
                    InvoiceDate = request.InvoiceDate,
                    Filename = "",
                    OriginalFilename = "",
                    RootFolder = _options.DataRootDirectory,
                    Remarks = request.Remarks,
                    IsToBe = true,
                    CreatedDate = now,
                    CreatedBy = request.UserId,
                    LastUpdated = now,
                    LastUpdatedBy = request.UserId,
                    IsDeleted = false,
                    DeletedBy = 0
                };
                _context.CrmDealInvoices.Add(invoice);
                await _context.SaveChangesAsync();

                await addOrUpdatePIC(deal.Id, request.PicId, request.UserId, now);

                string prevData = invoice.InvoiceDate.ToString("DD MMM YYYY", CultureInfo.CreateSpecificCulture("en"));
                _ = await AddDealHistory("tobe", deal.Id, prevData, invoice.Id.ToString(), invoice.InvoiceDate, request.UserId, now, request.UserId, "To Be Invoiced", "", "", 0, 0, 0, invoice.Remarks, invoice.Amount);

                await SendEmail(EMAIL_ADD_TO_BE_INVOICED, deal, invoice);

                return invoice;
            }
            catch
            {
                return BadRequest(new { error = "Error updating database. Please check clientId" });
            }
        }


        /**
         * @api {put} /pipeline/tobeinvoiced/{invoiceId} PUT to be invoiced 
         * @apiVersion 1.0.0
         * @apiName PutToBeInvoiced
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * @apiParam {Number} invoiceId        Id dari invoice yang bersangkutan, sama dengan id di request
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "id": 15,
         *     "clientId": 1737,
         *     "dealId": 8,
         *     "userId": 1,
         *     "invoiceDate": "2020-06-24T00:00:00.000Z",
         *     "amount": 122000000,
         *     "remarks": "Termin 1"
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   {
         *       "id": 15,
         *       "dealId": 8,
         *       "amount": 122000000,
         *       "periodId": 6,
         *       "invoiceDate": "2020-06-24T00:00:00Z",
         *       "filename": "",
         *       "originalFilename": "",
         *       "remarks": "Termin 1",
         *       "isToBe": true,
         *       "createdDate": "2020-06-18T08:51:01.9884936",
         *       "createdBy": 1,
         *       "lastUpdated": "2020-06-18T08:52:00.1540498+07:00",
         *       "lastUpdatedBy": 1,
         *       "isDeleted": false,
         *       "deletedBy": 0,
         *       "deletedDate": null
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPut("tobeinvoiced/{invoiceId}")]
        public async Task<ActionResult<CrmDealInvoice>> PutToBeInvoiced(int invoiceId, PostToBeInvoiced request)
        {
            if (invoiceId != request.Id)
            {
                return BadRequest();
            }

            CrmDeal deal = _context.CrmDeals.Find(request.DealId);
            if (deal == null)
            {
                return NotFound(new { error = "Deal not found. Please check dealId" });
            }

            DateTime now = DateTime.Now;

            try
            {
                if (deal.ClientId != request.ClientId)
                {
                    deal.ClientId = request.ClientId;
                    deal.LastUpdated = now;
                    deal.LastUpdatedBy = request.UserId;
                    _context.Entry(deal).State = EntityState.Modified;
                }

                CrmDealInvoice invoice = _context.CrmDealInvoices.Find(invoiceId);

                if (invoice == null)
                {
                    return NotFound(new { error = "Invoice not found. Please check invoiceId" });
                }

                if (!invoice.IsToBe)
                {
                    return BadRequest(new { error = "Already invoiced. Cannot be changed back." });
                }

                if (invoice.InvoiceDate != request.InvoiceDate)
                {
                    await AddDealHistory("tobedate", deal.Id, invoice.InvoiceDate.ToString(), request.InvoiceDate.ToString(), now, request.UserId, now, request.UserId, "Changing to be invoiced date from ", invoice.InvoiceDate.ToString(), request.InvoiceDate.ToString(), 0, 0, 0, request.Remarks);
                }
                /*
                                if(invoice.Amount != request.Amount)
                                {
                                    await AddDealHistory("tobeamount", deal.Id, invoice.Amount.ToString(), request.Amount.ToString(), now, request.UserId, now, request.UserId, "Changing invoice value for invoice date ", request.InvoiceDate.ToString(), "", 0, 0, 0, request.Remarks, request.Amount);
                                }
                                */
                if (invoice.Remarks == null)
                {
                    await UpdateDealHistoryRemarks("tobe", deal.Id, invoice.Id, request.Remarks);
                }
                else if (!invoice.Remarks.Equals(request.Remarks))
                {
                    await UpdateDealHistoryRemarks("tobe", deal.Id, invoice.Id, request.Remarks);
                }

                invoice.DealId = deal.Id;
                invoice.Amount = request.Amount;
                invoice.PeriodId = GetPeriodId(request.InvoiceDate, request.UserId);
                invoice.InvoiceDate = request.InvoiceDate;
                invoice.Remarks = request.Remarks;
                invoice.LastUpdated = now;
                invoice.LastUpdatedBy = request.UserId;

                _context.Entry(invoice).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                await SendEmail(EMAIL_EDIT_TO_BE_INVOICED, deal, invoice);

                return invoice;

            }
            catch
            {
                return BadRequest(new { error = "Error updating database. Please check clientId and invoiceId" });
            }

        }

        /**
         * @api {delete} /pipeline/invoice/{invoiceId}/{userId} DELETE invoice
         * @apiVersion 1.0.0
         * @apiName DeleteInvoice
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {invoiceId} invoiceId  Id dari deal yang ingin dihapus
         * @apiParam {Number} userId        Id dari user yang sedang login
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "id": 15,
         *       "dealId": 8,
         *       "amount": 122000000,
         *       "periodId": 6,
         *       "invoiceDate": "2020-06-24T00:00:00",
         *       "filename": "",
         *       "originalFilename": "",
         *       "remarks": "Termin 1",
         *       "isToBe": true,
         *       "createdDate": "2020-06-18T08:51:01.9884936",
         *       "createdBy": 1,
         *       "lastUpdated": "2020-06-18T08:52:00.1540498",
         *       "lastUpdatedBy": 1,
         *       "isDeleted": true,
         *       "deletedBy": 1,
         *       "deletedDate": "2020-06-18T09:10:49.2259374+07:00"
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         * @apiError Notfound Deal Id salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpDelete("invoice/{invoiceId}/{userId}")]
        public async Task<ActionResult<CrmDealInvoice>> DeleteInvoice(int invoiceId, int userId)
        {
            CrmDealInvoice invoice = _context.CrmDealInvoices.Find(invoiceId);
            if (invoice == null)
            {
                return NotFound();
            }

            CrmDeal deal = _context.CrmDeals.Find(invoice.DealId);
            if (deal == null)
            {
                return NotFound();
            }

            DateTime now = DateTime.Now;

            invoice.IsDeleted = true;
            invoice.DeletedBy = userId;
            invoice.DeletedDate = now;
            _context.Entry(invoice).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            await SendEmail(EMAIL_DELETE_TO_BE_INVOICED, deal, invoice);

            return invoice;
        }



        // Yang ini ga dipakai karena bermasalah ya base64 nya
        [Authorize(Policy = "ApiUser")]
        [HttpPost("pricing")]
        [DisableRequestSizeLimit]
        public async Task<ActionResult<PostPricingResponse>> PostPricing(PostPricing request)
        {
            if (!DealExists(request.DealId))
            {
                return NotFound();
            }

            List<Error> errors = new List<Error>();

            DateTime now = DateTime.Now;

            CrmDealPNL pnl = new CrmDealPNL();
            try
            {
                CrmDealPNL curPNL = _context.CrmDealPNLs.Where(a => a.DealId == request.DealId && a.IsActive).FirstOrDefault();
                if (curPNL != null && curPNL.Id > 0)
                {
                    curPNL.IsActive = false;
                    curPNL.LastUpdated = now;
                    curPNL.LastUpdatedBy = request.UserId;

                    _context.Entry(curPNL).State = EntityState.Modified;
                }

                pnl.DealId = request.DealId;
                pnl.OriginalFilename = request.Filename;
                pnl.RootFolder = _options.DataRootDirectory;
                pnl.IsActive = true;
                pnl.CreatedDate = now;
                pnl.CreatedBy = request.UserId;
                pnl.LastUpdated = now;
                pnl.LastUpdatedBy = request.UserId;
                pnl.IsDeleted = false;
                pnl.DeletedBy = 0;

                if (request.FileBase64 == null && curPNL != null)
                {
                    pnl.Filename = curPNL.Filename;
                    pnl.OriginalFilename = curPNL.OriginalFilename;
                }

                _context.CrmDealPNLs.Add(pnl);
                await _context.SaveChangesAsync();

            }
            catch
            {
                return BadRequest(new { error = "Error writing to database. Please check the ids." });
            }
            if (request.FileBase64 != null && request.Filename != null)
            {
                var error = SaveFileUpload(request.FileBase64, request.Filename, request.DealId);
                if (error.Code.Equals("ok"))
                {
                    string[] names = error.Description.Split(separator);
                    if (names.Length >= 3)
                    {
                        pnl.Filename = names[1];
                        _context.Entry(pnl).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                    }

                }
                else
                {
                    errors.Add(error);
                }
            }

            PostPricingResponse response = new PostPricingResponse();
            response.Id = pnl.Id;
            response.DealId = pnl.DealId;
            response.Filename = pnl.OriginalFilename;

            response.Errors.AddRange(errors);

            await AddDealHistory("pnl", request.DealId, pnl.OriginalFilename, pnl.Id.ToString(), now, request.UserId, now, request.UserId, "Pricing", "", "", 0, 0, 0);

            return response;
        }

        /**
         * @api {post} /pipeline/pricing/upload POST pricing
         * @apiVersion 1.0.0
         * @apiName PostPricingUploadFile
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} DocumentType          1 untuk pricing, 2 untuk agreement
         * @apiParam {Number} userId                User Id yang mengupload
         * @apiParam {Number} dealId                Id dari deal yang bersangkutan
         * @apiParam {Files} files                  File yang diupload
         * @apiDescription Pakai formdata
         *   
         * @apiSuccessExample Success-Response:
         *   {
         *       "id": 3,
         *       "dealId": 1,
         *       "filename": null,
         *       "errors": [
         *       ]
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost("pricing/upload")]
        public async Task<ActionResult<PostPricingResponse>> PostPricingUploadFile([FromForm] PostPricingUploadFile request)
        {
            if (!DealExists(request.DealId))
            {
                return NotFound();
            }

            if (request.DocumentType == 0) request.DocumentType = 1;

            List<Error> errors = new List<Error>();

            DateTime now = DateTime.Now;

            CrmDealPNL pnl = new CrmDealPNL();

            CrmDealPNL curPNL = _context.CrmDealPNLs.Where(a => a.DealId == request.DealId && a.IsActive && a.DocumentType == request.DocumentType).FirstOrDefault();
            if (curPNL != null && curPNL.Id > 0)
            {
                curPNL.IsActive = false;
                curPNL.LastUpdated = now;
                curPNL.LastUpdatedBy = request.UserId;

                _context.Entry(curPNL).State = EntityState.Modified;
            }

            pnl.DealId = request.DealId;
            pnl.IsActive = true;
            pnl.DocumentType = request.DocumentType;
            pnl.CreatedDate = now;
            pnl.CreatedBy = request.UserId;
            pnl.LastUpdated = now;
            pnl.LastUpdatedBy = request.UserId;
            pnl.IsDeleted = false;
            pnl.DeletedBy = 0;
            pnl.OriginalFilename = "";
            pnl.RootFolder = _options.DataRootDirectory;
            pnl.Filename = "";

            if (request.Files == null && curPNL != null)
            {
                pnl.Filename = curPNL.Filename;
                pnl.OriginalFilename = curPNL.OriginalFilename;
            }

            try
            {
                if (request.Files != null)
                {
                    string fileDir = GetDealDirectory(request.DealId);
                    if (_fileService.CheckAndCreateDirectory(fileDir))
                    {
                        foreach (IFormFile formFile in request.Files)
                        {
                            var fileExt = System.IO.Path.GetExtension(formFile.FileName).Substring(1).ToLower();
                            string randomName = Path.GetRandomFileName() + "." + fileExt;

                            var fileName = Path.Combine(fileDir, randomName);

                            Stream stream = formFile.OpenReadStream();
                            _fileService.CopyStream(stream, fileName);
                            stream.Dispose();

                            pnl.OriginalFilename = formFile.FileName;
                            pnl.Filename = randomName;

                        }
                    }
                    else
                    {
                        return BadRequest(new { error = "Error in saving file" });
                    }
                }

                _context.CrmDealPNLs.Add(pnl);
                await _context.SaveChangesAsync();
            }
            catch
            {
                return BadRequest(new { error = "Error in saving file" });
            }

            PostPricingResponse response = new PostPricingResponse();
            response.Id = pnl.Id;
            response.DealId = pnl.DealId;
            response.Filename = pnl.OriginalFilename;

            response.Errors.AddRange(errors);

            string type = pnl.DocumentType == 1 ? "Pricing" : "Agreeement";

            await AddDealHistory("pnl", request.DealId, pnl.OriginalFilename, pnl.Id.ToString(), now, request.UserId, now, request.UserId, "Document", type, "", 0, 0, 0);

            return response;
        }

        /**
         * @api {put} /pipeline/proposal/{proposalId} PUT proposal
         * @apiVersion 1.0.0
         * @apiName PutProposal
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * @apiDescription Perhatikan id yang di REQUEST beda dengan yang di RESPONSE, karena id yang lama tidak benar-benar diganti isinya, tetapi hanya dibuat tidak aktif, karena masih perlu ditampilkan di history.
         * 
         * @apiParam {Number} proposalId        Id dari proposal yang bersangkutan.
         * @apiParamExample {json} Request-Example:
         *   {
         *     "id": 11,
         *     "userId": 1,
         *     "dealId": 1,
         *     "sentById": 3,
         *     "typeId": 1,
         *     "sentDate": "2020-05-22",
         *     "contactIds": [
         *       3,4
         *     ],
         *     "filename": "proposal.pdf",
         *     "fileBase64": "isi file dalam format base64",
         *     "proposalValue": 100000000,
         *     "invoices": [
         *       {
         *         "id": 0,
         *         "invoiceDate": "2020-07-01",
         *         "invoiceAmount": 60000000,
         *         "remarks": "Termin 1"
         *       },
         *       {
         *         "id": 0,
         *         "invoiceDate": "2020-09-01",
         *         "invoiceAmount": 40000000,
         *         "remarks": "Termin 2"
         *       }
         *     ]
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   {
         *     "id": 12,
         *     "proposalType": {
         *       "id": 1,
         *       "text": "Workshop"
         *     },
         *     "filename": "proposal.pdf",
         *     "url": "/download/deal/4/xe4c1px2.kn4.pptxo/proposal.pptx",
         *     "proposalValue": 100000000,
         *     "invoices": [
         *       {
         *         "id": 16,
         *         "invoiceDate": "2020-07-01T00:00:00.000Z",
         *         "invoiceAmount": 60000000,
         *         "remarks": "Termin 1"
         *       },
         *       {
         *         "id": 17,
         *         "invoiceDate": "2020-09-01T00:00:00.000Z",
         *         "invoiceAmount": 40000000,
         *         "remarks": "Termin 2"
         *       }
         *     ],
         *     "errors": [
         *       {
         *         "code": "ok",
         *         "description": ""
         *       }
         *     ]
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         * @apiError BadRequest Proposal Id yang ada di URL berbeda dengan yang ada di body.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPut("proposal/{id}")]
        [DisableRequestSizeLimit]
        public async Task<ActionResult<PostProposalResponse>> PutProposal(int id, PostProposalRequest request)
        {
            if (id != request.Id)
            {
                return BadRequest();
            }

            if (!DealExists(request.DealId))
            {
                return NotFound();
            }

            ActionResult<PostProposalResponse> response = await PostProposal(request);

            return response;
        }

        /**
         * @api {delete} /pipeline/{dealId}/{userId} DELETE deal
         * @apiVersion 1.0.0
         * @apiName DeleteDeal
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {dealId} dealId  Id dari deal yang ingin dihapus
         * @apiParam {Number} userId  Id dari user yang sedang login
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "id": 4,
         *       "name": "Pengembangan Budaya Organisasi",
         *       "dealDate": "2020-05-20T00:00:00",
         *       "probability": 10,
         *       "clientId": 2,
         *       "stageId": 1,
         *       "tribeId": 1,
         *       "segmentId": 1,
         *       "branchId": 1,
         *       "stateId": 1,
         *       "createdDate": "2020-05-22T11:54:57.2053593",
         *       "createdBy": 1,
         *       "lastUpdated": "2020-05-22T11:54:57.2053593",
         *       "lastUpdatedBy": 1,
         *       "isDeleted": true,
         *       "deletedBy": 1,
         *       "deletedDate": "2020-06-02T11:34:39.082415+07:00"
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         * @apiError Notfound Deal Id salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpDelete("{dealId}/{userId}")]
        public async Task<ActionResult<CrmDeal>> DeleteDeal(int dealId, int userId)
        {
            var deal = await _context.CrmDeals.FindAsync(dealId);
            if (deal == null)
            {
                return NotFound();
            }

            DateTime now = DateTime.Now;

            deal.IsDeleted = true;
            deal.DeletedBy = userId;
            deal.DeletedDate = now;
            _context.Entry(deal).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return deal;

        }

        /**
         * @api {delete} /pipeline/proposal/{proposalId}/{userId} DELETE proposal
         * @apiVersion 1.0.0
         * @apiName DeleteProposal
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} proposalId  Id dari proposal yang ingin dihapus
         * @apiParam {Number} userId    Id dari user yang sedang login
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *     "id": 18,
         *     "proposalValue": 75000000,
         *     "periodId": 21,
         *     "dealId": 18,
         *     "sentById": 1,
         *     "sentDate": "2020-05-22T12:08:02.955Z",
         *     "typeId": 1,
         *     "filename": "uxjsk1uv.jsk.pdf",
         *     "originalFilename": "proposal.pdf",
         *     "isActive": true,
         *     "createdDate": "2020-05-22T12:08:02.955Z",
         *     "createdBy": 0,
         *     "lastUpdated": "2020-05-22T12:08:02.955Z",
         *     "lastUpdatedBy": 0,
         *     "isDeleted": true,
         *     "deletedBy": 1,
         *     "deletedDate": "2020-05-22T12:08:02.955Z"
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         * @apiError Notfound Proposal Id salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpDelete("proposal/{id}/{userId}")]
        public async Task<ActionResult<CrmDealProposal>> DeleteProposal(int id, int userId)
        {
            var proposal = await _context.CrmDealProposals.FindAsync(id);
            if (proposal == null)
            {
                return NotFound();
            }

            DateTime now = DateTime.Now;

            proposal.IsDeleted = true;
            proposal.DeletedBy = userId;
            proposal.DeletedDate = now;
            _context.Entry(proposal).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return proposal;
        }

        /**
         * @api {post} /pipeline/visit POST visit
         * @apiVersion 1.0.0
         * @apiName PostVisit
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * @apiDescription Perhatikan list rms dan consultants yang di REQUEST adalah userId, khususnya untuk RM, karena userId beda dengan Id untuk RM.
         * 
         * @apiParam {Number} dealId     Id untuk deal yang bersangkutan. Atau 0 untuk yang tidak terkait sama deal tertentu.
         * @apiParam {Number} companyId  Id untuk company. Hanya digunakan untuk sales visit with no deal. 
         * @apiParam {string} visitDate  Format YYYY-MM-DD
         * @apiParam {string} fromTime   Format HH:MM tanpa AM/PM
         * @apiParam {string} toTime     Format HH:MM tanpa AM/PM
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "id": 0,
         *     "dealId": 1,
         *     "CompanyId": 0,
         *     "userId": 1,
         *     "visitDate": "2020-05-20",
         *     "startTime": "14:00",
         *     "endTime": "16:00",
         *     "tribes": [
         *       1,2
         *     ],
         *     "contacts": [
         *       1,2
         *     ],
         *     "rms": [
         *       1
         *     ],
         *     "consultants": [
         *       2,3
         *     ],
         *     "location": "Virtual lewat Zoom",
         *     "objective": "Objective meeting",
         *     "nextStep": "Next step",
         *     "remark": "Remark"
         *   }
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "id": 1,
         *       "dealId": 1,
         *       "visitFromTime": "2020-05-20T14:00:00",
         *       "visitToTime": "2020-05-20T16:00:00",
         *       "periodId": 5,
         *       "location": "Virtual lewat Zoom",
         *       "objective": "Objective meeting",
         *       "nextStep": "Next step",
         *       "remark": "Remark",
         *       "createdDate": "2020-05-23T14:32:06.4058444+07:00",
         *       "createdBy": 1,
         *       "lastUpdated": "2020-05-23T14:32:06.4058444+07:00",
         *       "lastUpdatedBy": 1,
         *       "isDeleted": false,
         *       "deletedBy": 0,
         *       "deletedDate": null
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         * @apiError Notfound Proposal Id salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost("visit")]
        public async Task<ActionResult<CrmDealVisit>> PostVisit(SalesVisitRequest request)
        {
            DateTime now = DateTime.Now;

            DateTime visitDate;

            try
            {
                visitDate = Convert.ToDateTime(request.VisitDate);
            }
            catch
            {
                return BadRequest(new { error = "Bad date format." });
            }

            if (visitDate == null)
            {
                return BadRequest(new { error = "Bad date format." });
            }

            string[] fromtime = request.StartTime.Split(":");
            if (fromtime.Length != 2)
            {
                return BadRequest(new { error = "Bad time format." });
            }
            string[] totime = request.EndTime.Split(":");
            if (totime.Length != 2)
            {
                return BadRequest(new { error = "Bad time format." });
            }

            CrmDealVisit visit = new CrmDealVisit();
            visit.DealId = request.DealId;
            try
            {
                visit.VisitFromTime = new DateTime(visitDate.Year, visitDate.Month, visitDate.Day, int.Parse(fromtime[0]), int.Parse(fromtime[1]), 0);
                visit.VisitToTime = new DateTime(visitDate.Year, visitDate.Month, visitDate.Day, int.Parse(totime[0]), int.Parse(totime[1]), 0);
            }
            catch
            {
                return BadRequest(new { error = "Bad date or time format." });
            }
            visit.PeriodId = GetPeriodId(visit.VisitFromTime, request.UserId);
            visit.Location = request.Location;
            visit.Objective = request.Objective;
            visit.NextStep = request.NextStep;
            visit.Remark = request.Remark;
            visit.CreatedDate = now;
            visit.CreatedBy = request.UserId;
            visit.LastUpdated = now;
            visit.LastUpdatedBy = request.UserId;
            visit.IsDeleted = false;
            visit.DeletedBy = 0;
            visit.ClientId = request.ClientId > 0 ? request.ClientId : 0;

            try
            {
                _context.CrmDealVisits.Add(visit);
                await _context.SaveChangesAsync();
            }
            catch
            {
                return BadRequest(new { error = "Error writing to database" });
            }

            try
            {
                AddVisitContacts(request.Contacts, visit.Id);
            }
            catch
            {
                return BadRequest(new { error = "Error writing to database for contacts" });
            }

            try
            {
                AddVisitUsers(request.Consultants, visit.Id, false, true);
            }
            catch
            {
                return BadRequest(new { error = "Error writing to database for users" });
            }

            try
            {
                if (request.Tribes != null)
                {
                    AddVisitTribes(request.Tribes, visit.Id);
                }
            }
            catch
            {
                return BadRequest(new { error = "Error writing to database for Tribes" });
            }

            try
            {
                AddVisitUsers(request.Rms, visit.Id, true, false);
            }
            catch
            {
                return BadRequest(new { error = "Error writing to database for RMs" });
            }
            if (request.DealId != 0)
            {
                await AddDealHistory("meeting", request.DealId, "", visit.Id.ToString(), visit.VisitFromTime, request.UserId, now, request.UserId, "Sales Meeting", "", "", 0, 0, 0);
            }

            return _context.CrmDealVisits.Find(visit.Id);
        }

        /**
         * @api {put} /pipeline/visit/{visitId} PUT visit
         * @apiVersion 1.0.0
         * @apiName PutVisit
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {string} visitDate  Format YYYY-MM-DD
         * @apiParam {string} fromTime   Format HH:MM tanpa AM/PM
         * @apiParam {string} toTime     Format HH:MM tanpa AM/PM
         * @apiParam {Number} visitId    Id dari visit yang bersangkutan.
         * @apiDescription Perhatikan list rms dan consultants yang di REQUEST adalah userId, khususnya untuk RM, karena userId beda dengan Id untuk RM.
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "id": 4,
         *     "dealId": 1,
         *     "userId": 1,
         *     "visitDate": "2020-05-20",
         *     "startTime": "14:00",
         *     "endTime": "16:00",
         *     "contacts": [
         *       1,2
         *     ],
         *     "rms": [
         *       1
         *     ],
         *     "consultants": [
         *       2,3
         *     ],
         *     "location": "Virtual lewat Zoom",
         *     "objective": "Objective meeting",
         *     "nextStep": "Next step",
         *     "remark": "Remark"
         *   }
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "id": 4,
         *       "dealId": 1,
         *       "visitFromTime": "2020-05-20T14:00:00",
         *       "visitToTime": "2020-05-20T16:00:00",
         *       "periodId": 5,
         *       "location": "Virtual lewat Zoom",
         *       "objective": "Objective meeting",
         *       "nextStep": "Next step",
         *       "remark": "Remark",
         *       "createdDate": "2020-05-23T14:32:06.4058444+07:00",
         *       "createdBy": 1,
         *       "lastUpdated": "2020-05-23T14:32:06.4058444+07:00",
         *       "lastUpdatedBy": 1,
         *       "isDeleted": false,
         *       "deletedBy": 0,
         *       "deletedDate": null
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         * @apiError Notfound Visit Id salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPut("visit/{id}")]
        public async Task<ActionResult<CrmDealVisit>> PutVisit(int id, SalesVisitRequest request)
        {
            if (id != request.Id)
            {
                return BadRequest();
            }
            /*
            if (!DealExists(request.DealId))
            {
                return NotFound();
            }
            */
            if (!VisitExists(id))
            {
                return NotFound();
            }

            DateTime now = DateTime.Now;
            string[] dates = request.VisitDate.Split("-");
            if (dates.Length != 3)
            {
                return BadRequest(new { error = "Bad date format." });
            }
            string[] fromtime = request.StartTime.Split(":");
            if (fromtime.Length != 2)
            {
                return BadRequest(new { error = "Bad time format." });
            }
            string[] totime = request.EndTime.Split(":");
            if (totime.Length != 2)
            {
                return BadRequest(new { error = "Bad time format." });
            }

            CrmDealVisit visit = await _context.CrmDealVisits.FindAsync(id);
            if (visit == null || visit.Id == 0)
            {
                return NotFound();
            }

            visit.DealId = request.DealId;
            try
            {
                visit.VisitFromTime = new DateTime(int.Parse(dates[0]), int.Parse(dates[1]), int.Parse(dates[2]), int.Parse(fromtime[0]), int.Parse(fromtime[1]), 0);
                visit.VisitToTime = new DateTime(int.Parse(dates[0]), int.Parse(dates[1]), int.Parse(dates[2]), int.Parse(totime[0]), int.Parse(totime[1]), 0);
            }
            catch
            {
                return BadRequest(new { error = "Bad date or time format." });
            }
            visit.PeriodId = GetPeriodId(visit.VisitFromTime, request.UserId);
            visit.ClientId = request.ClientId;
            visit.Location = request.Location;
            visit.Objective = request.Objective;
            visit.NextStep = request.NextStep;
            visit.Remark = request.Remark;
            visit.LastUpdated = now;
            visit.LastUpdatedBy = request.UserId;
            visit.IsDeleted = false;
            visit.DeletedBy = 0;

            try
            {
                _context.Entry(visit).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                AddVisitContacts(request.Contacts, visit.Id);
                AddVisitUsers(request.Consultants, visit.Id, false, true);
                AddVisitUsers(request.Rms, visit.Id, true, false);
                AddVisitTribes(request.Tribes, visit.Id);
            }
            catch
            {
                return BadRequest(new { error = "Error writing to database" });
            }

            await UpdateDealHistory("meeting", request.DealId, "", visit.Id.ToString(), visit.VisitFromTime, request.UserId, now, request.UserId, "Sales Meeting", "", "", 0, 0, 0);

            return _context.CrmDealVisits.Find(visit.Id);
        }

        /**
         * @api {delete} /pipeline/visit/{visitId}/{userId} DELETE visit
         * @apiVersion 1.0.0
         * @apiName DeleteVisit
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} visitId   Id dari visit yang ingin dihapus
         * @apiParam {Number} userId    Id dari user yang sedang login
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "id": 4,
         *       "dealId": 1,
         *       "visitFromTime": "2020-05-20T14:00:00",
         *       "visitToTime": "2020-05-20T16:00:00",
         *       "periodId": 5,
         *       "location": "Virtual lewat Zoom",
         *       "objective": "Objective meeting",
         *       "nextStep": "Next step",
         *       "remark": "Remark",
         *       "createdDate": "2020-05-23T15:14:24.9013755",
         *       "createdBy": 1,
         *       "lastUpdated": "2020-05-23T15:14:56.4728974",
         *       "lastUpdatedBy": 1,
         *       "isDeleted": true,
         *       "deletedBy": 1,
         *       "deletedDate": "2020-05-23T15:25:06.6105151+07:00"
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         * @apiError Notfound Visit Id salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpDelete("visit/{visitId}/{userId}")]
        public async Task<ActionResult<CrmDealVisit>> DeleteVisit(int visitId, int userId)
        {
            var visit = await _context.CrmDealVisits.FindAsync(visitId);
            if (visit == null)
            {
                return NotFound();
            }

            DateTime now = DateTime.Now;

            visit.IsDeleted = true;
            visit.DeletedBy = userId;
            visit.DeletedDate = now;
            _context.Entry(visit).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return visit;
        }


        /**
         * @api {post} /pipeline/owners POST owners 
         * @apiVersion 1.0.0
         * @apiName PostOwners
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * @apiDescription Perhatikan list rms dan consultants yang di REQUEST adalah userId, khususnya untuk RM, karena userId beda dengan Id untuk RM.
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "dealId": 1,
         *     "useerId": 1,
         *     "segmentId": 2,
         *     "branchId": 1,
         *     "picId": 35,
         *     "consultants": [
         *       11
         *     ],
         *     "rms": [
         *       {
         *         "userId": 5,
         *         "percent": 60,
         *         "usePercent": true,
         *         "Nominal", 0
         *       },
         *       {
         *         "userId": 7,
         *         "percent": 40
         *         "usePercent": true,
         *         "Nominal", 0
         *       }
         *     ]
         *     "tribes": [
         *         {
         *           "tribeId": 1,
         *           "percent": 60
         *           "usePercent": true,
         *           "Nominal", 0
         *         },
         *         {
         *           "tribeId": 2,
         *           "percent": 40
         *           "usePercent": true,
         *           "Nominal", 0
         *         },
         *     ]
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   {
         *       "dealId": 1,
         *       "branch": {
         *           "id": 1,
         *           "text": "Jakarta"
         *       },
         *       "segment": {
         *           "id": 2,
         *           "text": "Government"
         *       },
         *       "consultants": [
         *           {
         *               "id": 11,
         *               "text": "Fitoni"
         *           }
         *       ],
         *       "rms": [
         *           {
         *               "id": 1,
         *               "userId": 5,
         *               "segmentId": 3,
         *               "branchId": 1,
         *               "leaderId": 0,
         *               "name": "Leviana Wijaya",
         *               "email": "levi@gmlperformance.co.id",
         *               "percentage": 60
         *           },
         *           {
         *               "id": 2,
         *               "userId": 7,
         *               "segmentId": 3,
         *               "branchId": 1,
         *               "leaderId": 0,
         *               "name": "Grace",
         *               "email": "grace@gmlperformance.co.id",
         *               "percentage": 40
         *           }
         *       ],
         *       "tribes": [
         *           {
         *               "id": 1,
         *               "text": "Strategy & Execution"
         *           }
         *       ],
         *       "errors": []
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost("owners")]
        public async Task<ActionResult<OwnerResponse>> PostOwners(OwnerRequest request)
        {
            CrmDeal deal = _context.CrmDeals.Find(request.DealId);
            if (deal == null || deal.Id == 0)
            {
                return NotFound();
            }

            DateTime now = DateTime.Now;

            deal.BranchId = request.BranchId;
            deal.SegmentId = request.SegmentId;
            deal.LastUpdated = now;
            deal.LastUpdatedBy = request.UseerId;
            _context.Entry(deal).State = EntityState.Modified;
            _context.SaveChanges();

            List<PercentInfo> cons = new List<PercentInfo>();
            foreach (int n in request.Consultants)
            {
                cons.Add(new PercentInfo()
                {
                    UserId = n,
                    Percent = 0.0d,
                    UsePercent = false,
                    Nominal = 0
                });
            }

            List<PercentInfo> pics = new List<PercentInfo>();
            if (request.PicId != 0)
            {
                pics.Add(new PercentInfo()
                {
                    UserId = request.PicId,
                    Percent = 100,
                    UsePercent = true,
                    Nominal = 0
                });
            }

            OwnerResponse response = new OwnerResponse();

            Error e = await AddDealTribe(request.Tribes, request.DealId, now, request.UseerId);
            if (!e.Code.Equals("ok"))
            {
                response.Errors.Add(e);
            }

            Error e1 = await AddDealOwner(request.Rms, request.DealId, "rm", now, request.UseerId);
            if (!e1.Code.Equals("ok"))
            {
                response.Errors.Add(e1);
            }

            Error e2 = await AddDealOwner(cons, request.DealId, "con", now, request.UseerId);
            if (!e2.Code.Equals("ok"))
            {
                response.Errors.Add(e2);
            }

            if (pics.Count != 0)
            {
                Error e3 = await AddDealOwner(pics, request.DealId, "pic", now, request.UseerId);
                if (!e3.Code.Equals("ok"))
                {
                    response.Errors.Add(e3);
                }
            }

            var query = from d in _context.CrmDeals
                        join segment in _context.CrmSegments
                        on d.SegmentId equals segment.Id
                        join branch in _context.CrmBranches
                        on d.BranchId equals branch.Id
                        where d.Id == request.DealId && !d.IsDeleted
                        select new
                        {
                            DealId = d.Id,
                            DealName = d.Name,
                            SegmentId = segment.Id,
                            SegmentName = segment.Segment,
                            BranchId = branch.Id,
                            BranchName = branch.Branch,
                        };
            var obj = query.FirstOrDefault();

            if (obj != null)
            {
                response.DealId = obj.DealId;
                response.Branch.Id = obj.BranchId;
                response.Branch.Text = obj.BranchName;
                response.Segment.Id = obj.SegmentId;
                response.Segment.Text = obj.SegmentName;

                var qcon = from member in _context.CrmDealInternalMembers
                           join user in _context.Users
                           on member.UserId equals user.ID
                           join role in _context.CrmDealRoles
                           on member.RoleId equals role.Id
                           where member.DealId == obj.DealId && !member.IsDeleted && role.Shortname.Equals("con")
                           select new GenericInfo()
                           {
                               Id = member.UserId,
                               Text = user.FirstName,
                           };
                response.Consultants = qcon.ToList();

                var qrm = from member in _context.CrmDealInternalMembers
                          join user in _context.Users
                          on member.UserId equals user.ID
                          join rel in _context.CrmRelManagers
                          on member.UserId equals rel.UserId
                          join role in _context.CrmDealRoles
                          on member.RoleId equals role.Id
                          where member.DealId == obj.DealId && !member.IsDeleted && role.Shortname.Equals("rm")
                          select new RMInfo()
                          {
                              Id = rel.Id,
                              UserId = member.UserId,
                              SegmentId = rel.SegmentId,
                              BranchId = rel.BranchId,
                              LeaderId = rel.LeaderId,
                              Name = user.FirstName,
                              Email = user.Email,
                              Percentage = member.Percentage,
                              UsePercent = member.UsePercent,
                              Nominal = member.Nominal
                          };
                response.Rms = qrm.ToList();

                var qtribe = from d in _context.CrmDeals
                             join dt in _context.CrmDealTribes
                             on d.Id equals dt.DealId
                             join t in _context.CoreTribes
                             on dt.TribeId equals t.Id
                             where !dt.IsDeleted && d.Id == obj.DealId
                             select new GenericInfo()
                             {
                                 Id = t.Id,
                                 Text = t.Tribe
                             };
                response.Tribes = qtribe.ToList();

            };
            return response;
        }

        /**
        * @api {get} /Pipeline/detail/{dealId} GET deal detail
        * @apiVersion 1.0.0
        * @apiName GetDealDetail
        * @apiGroup Pipeline
        * @apiPermission ApiUser
        * 
        * @apiSuccessExample Success-Response:
        * {
        *     "id": 1,
        *     "name": "Pengembangan Budaya Organisasi",
        *     "probability": 80,
        *     "age": 9,
        *     "stages": [
        *         5,
        *         0,
        *         4,
        *         0,
        *         0
        *     ],
        *     "clientCompany": {
        *         "id": 2,
        *         "text": "PT Bank Mandiri Tbk."
        *     },
        *     "clientContact": {
        *         "id": 1,
        *         "text": "Andi"
        *     },
        *     "clientMembers": [
        *         {
        *             "id": 2,
        *             "text": "Budi"
        *         }
        *     ],
        *     "tribes": 
        *     [
        *       {
        *         "id": 2,
        *         "text": "Organizational Excellence",
        *         "percent": 100,
        *         "usePercent": true,
        *         "nominal": 0
        *       }
        *     ],
        *     "segment": {
        *         "id": 2,
        *         "text": "Government"
        *     },
        *     "branch": {
        *         "id": 1,
        *         "text": "Jakarta"
        *     },
        *     "state": {
        *         "id": 1,
        *         "text": "Open"
        *     },
        *     "stage": {
        *         "id": 2,
        *         "text": "Proposal Development"
        *     },
        *     "pic": {
        *         "id": 1,
        *         "text": "Rifky"
        *     },
        *     "rms": [
        *         {
        *             "id": 1,
        *             "userId": 5,
        *             "segmentId": 3,
        *             "branchId": 1,
        *             "leaderId": 0,
        *             "name": "Leviana Wijaya",
        *             "email": "levi@gmlperformance.co.id",
        *             "percentage": 60,
        *             "usePercent": false,
        *             "nominal": 0
        *         },
        *         {
        *             "id": 2,
        *             "userId": 7,
        *             "segmentId": 3,
        *             "branchId": 1,
        *             "leaderId": 0,
        *             "name": "Grace",
        *             "email": "grace@gmlperformance.co.id",
        *             "percentage": 40,
        *             "usePercent": false,
        *             "nominal": 0
        *         }
        *     ],
        *     "consultants": [
        *         {
        *             "id": 11,
        *             "text": "Widi Widianto"
        *         }
        *     ],
        *     "proposal": {
        *         "id": 9,
        *         "sentById": 1,
        *         "sentDate": "2020-05-22T00:00:00",
        *         "contactIds": [
        *             1,
        *             2
        *         ]
        *         "proposalType": {
        *             "id": 1,
        *             "text": "Workshop"
        *         },
        *         "filename": "proposal.html",
        *         "url": "/download/deal/1/cmaxgo31.bxr.html/proposal.html",
        *         "proposalValue": 100000000,
        *         "invoices": [
        *             {
        *                 "id": 17,
        *                 "invoiceDate": "2020-07-01T00:00:00",
        *                 "invoiceAmount": 60000000,
        *                 "remarks": "Termin 1"
        *             },
        *             {
        *                 "id": 18,
        *                 "invoiceDate": "2020-09-01T00:00:00",
        *                 "invoiceAmount": 40000000,
        *                 "remarks": "Termin 2"
        *             }
        *         ],
        *         "errors": []
        *     },
        *     "pricing": {
        *         "id": 3,
        *         "dealId": 1,
        *         "filename": "proposal.html",
        *         "errors": []
        *     },
        *     "history": [
        *         {
        *             "type": "addrm",
        *             "data": {
        *                 "header1": {
        *                     "id": 0,
        *                     "text": "Relationship Manager:"
        *                 },
        *                 "header2": {
        *                     "id": 2,
        *                     "text": "Fitoni"
        *                 },
        *                 "header3": {
        *                     "id": 0,
        *                     "text": "Join to this deal"
        *                 },
        *                 "updateTime": "2020-05-24T00:00:00",
        *                 "updateBy": {
        *                     "id": 1,
        *                     "text": "Rafdi"
        *                 },
        *                 "info": {}
        *             }
        *         },
        *         {
        *             "type": "prob",
        *             "data": {
        *                 "header1": {
        *                     "id": 0,
        *                     "text": "Probability:"
        *                 },
        *                 "header2": {
        *                     "id": 0,
        *                     "text": "10"
        *                 },
        *                 "header3": {
        *                     "id": 0,
        *                     "text": "80"
        *                 },
        *                 "updateTime": "2020-05-25T11:18:08.5853986",
        *                 "updateBy": {
        *                     "id": 1,
        *                     "text": "Rafdi"
        *                 },
        *                 "info": {}
        *             }
        *         },
        *         {
        *             "type": "stage",
        *             "data": {
        *                 "header1": {
        *                     "id": 0,
        *                     "text": "Stage"
        *                 },
        *                 "header2": {
        *                     "id": 0,
        *                     "text": "1"
        *                 },
        *                 "header3": {
        *                     "id": 0,
        *                     "text": "2"
        *                 },
        *                 "updateTime": "2020-05-25T10:24:00.2043953",
        *                 "updateBy": {
        *                     "id": 1,
        *                     "text": "Rafdi"
        *                 },
        *                 "info": {}
        *             }
        *         },
        *         {
        *             "type": "prop",
        *             "data": {
        *                 "header1": {
        *                     "id": 0,
        *                     "text": "Proposal"
        *                 },
        *                 "header2": {
        *                     "id": 0,
        *                     "text": ""
        *                 },
        *                 "header3": {
        *                     "id": 0,
        *                     "text": ""
        *                 },
        *                 "updateTime": "2020-05-24T18:54:55.3683145",
        *                 "updateBy": {
        *                     "id": 1,
        *                     "text": "Rafdi"
        *                 },
        *                 "info": {
        *                     "id": 9,
        *                     "proposalType": {
        *                         "id": 1,
        *                         "text": "Workshop"
        *                     },
        *                     "filename": "proposal.html",
        *                     "url": "/download/deal/1/cmaxgo31.bxr.html/proposal.html",
        *                     "proposalValue": 100000000,
        *                     "invoices": [
        *                         {
        *                             "id": 17,
        *                             "invoiceDate": "2020-07-01T00:00:00",
        *                             "invoiceAmount": 60000000,
        *                             "remarks": "Termin 1"
        *                         },
        *                         {
        *                             "id": 18,
        *                             "invoiceDate": "2020-09-01T00:00:00",
        *                             "invoiceAmount": 40000000,
        *                             "remarks": "Termin 2"
        *                         }
        *                     ],
        *                     "errors": []
        *                 }
        *             }
        *         },
        *         {
        *             "type": "meeting",
        *             "data": {
        *                 "header1": {
        *                     "id": 0,
        *                     "text": "Sales Meeting"
        *                 },
        *                 "header2": {
        *                     "id": 0,
        *                     "text": ""
        *                 },
        *                 "header3": {
        *                     "id": 0,
        *                     "text": ""
        *                 },
        *                 "updateTime": "2020-05-23T15:14:56.4728974",
        *                 "updateBy": {
        *                     "id": 1,
        *                     "text": "Rafdi"
        *                 },
        *                 "info": {
        *                     "visitId": 4,
        *                     "clientId": 2,
        *                     "clientName": "PT Bank Mandiri Tbk.",
        *                     "location": "Virtual lewat Zoom",
        *                     "rms": [
        *                         {
        *                             "id": 1,
        *                             "text": "Rafdi"
        *                         }
        *                     ],
        *                     "contacts": [
        *                         {
        *                             "id": 1,
        *                             "text": "Budi"
        *                         }
        *                     ],
        *                     "nextStep": "Next step",
        *                     "objective": "Objective meeting",
        *                     "remark": "Remark",
        *                     "fromTime": "2020-05-20T14:00:00",
        *                     "toTime": "2020-05-20T16:00:00"
        *                 }
        *             }
        *         },
        *         {
        *             "type": "created",
        *             "data": {
        *                 "header1": {
        *                     "id": 0,
        *                     "text": "Deal Created"
        *                 },
        *                 "header2": {
        *                     "id": 0,
        *                     "text": ""
        *                 },
        *                 "header3": {
        *                     "id": 0,
        *                     "text": ""
        *                 },
        *                 "updateTime": "2020-05-19T14:50:09.7562104",
        *                 "updateBy": {
        *                     "id": 1,
        *                     "text": "Rafdi"
        *                 },
        *                 "info": {}
        *             }
        *         }
        *     ]
        * }
        */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("detail/{dealId}")]
        public async Task<ActionResult<DealDetail>> GetDealDetail(int dealId)
        {
            var query = from deal in _context.CrmDeals
                        join cl in _context.CrmClients
                        on deal.ClientId equals cl.Id
                        join stage in _context.CrmDealStages
                        on deal.StageId equals stage.Id
                        join st in _context.CrmDealStates
                        on deal.StateId equals st.Id
                        join segment in _context.CrmSegments
                        on deal.SegmentId equals segment.Id
                        join branch in _context.CrmBranches
                        on deal.BranchId equals branch.Id
                        where deal.Id == dealId && !deal.IsDeleted
                        select new
                        {
                            DealId = deal.Id,
                            DealName = deal.Name,
                            DealProbability = deal.Probability,
                            deal.DealDate,
                            CompanyId = cl.Id,
                            CompanyName = cl.Company,
                            SegmentId = segment.Id,
                            SegmentName = segment.Segment,
                            BranchId = branch.Id,
                            BranchName = branch.Branch,
                            StateId = st.Id,
                            StateName = st.State,
                            StageId = stage.Id,
                            StageName = stage.Stage
                        };
            var obj = await query.FirstOrDefaultAsync();
            if (obj == null || obj.DealId == 0)
            {
                return NotFound();
            }

            DealDetail detail = new DealDetail();
            detail.Id = obj.DealId;
            detail.Name = obj.DealName;
            detail.Probability = obj.DealProbability;
            detail.Age = (DateTime.Today - obj.DealDate).Days;

            detail.ClientCompany.Id = obj.CompanyId;
            detail.ClientCompany.Text = obj.CompanyName;
            detail.Segment.Id = obj.SegmentId;
            detail.Segment.Text = obj.SegmentName;
            detail.Branch.Id = obj.BranchId;
            detail.Branch.Text = obj.BranchName;
            detail.State.Id = obj.StateId;
            detail.State.Text = obj.StateName;
            detail.Stage.Id = obj.StageId;
            detail.Stage.Text = obj.StageName;

            var query2 = from member in _context.CrmDealExternalMembers
                         join contact in _context.CrmContacts
                         on member.ContactId equals contact.Id
                         join role in _context.CrmDealRoles
                         on member.RoleId equals role.Id
                         where member.DealId == detail.Id && !member.IsDeleted
                         select new
                         {
                             ContactId = contact.Id,
                             ContactName = contact.Name,
                             DealRole = role.Shortname
                         };
            var info = await query2.ToListAsync();
            foreach (var i in info)
            {
                if (i.DealRole.Equals("contact"))
                {
                    detail.ClientContact.Id = i.ContactId;
                    detail.ClientContact.Text = i.ContactName;
                }
                else
                {
                    GenericInfo con = new GenericInfo()
                    {
                        Id = i.ContactId,
                        Text = i.ContactName
                    };
                    detail.ClientMembers.Add(con);
                }
            }


            var query3 = from member in _context.CrmDealInternalMembers
                         join user in _context.Users
                         on member.UserId equals user.ID
                         join role in _context.CrmDealRoles
                         on member.RoleId equals role.Id
                         where member.DealId == detail.Id && !member.IsDeleted
                         select new
                         {
                             UserId = user.ID,
                             UserName = user.FirstName,
                             DealRole = role.Shortname,
                             member.Percentage,
                             member.UsePercent,
                             member.Nominal,
                             Email = user.Email
                         };
            var info3 = await query3.ToListAsync();
            foreach (var j in info3)
            {
                if (j.DealRole.Equals("pic"))
                {
                    detail.Pic = new GenericInfo()
                    {
                        Id = j.UserId,
                        Text = j.UserName
                    };
                }
                else if (j.DealRole.Equals("con"))
                {
                    GenericInfo con = new GenericInfo()
                    {
                        Id = j.UserId,
                        Text = j.UserName
                    };
                    detail.Consultants.Add(con);
                }
                else
                {
                    var query5 = from rm in _context.CrmRelManagers
                                 join branch in _context.CrmBranches
                                 on rm.BranchId equals branch.Id
                                 where rm.UserId == j.UserId
                                 select new
                                 {
                                     RelManagerId = rm.Id,
                                     SegmentId = rm.SegmentId,
                                     BranchId = branch.Id,
                                     LeaderId = rm.LeaderId
                                 };
                    var obj5 = query5.FirstOrDefault();
                    if (obj5 != null && obj5.RelManagerId > 0)
                    {
                        RMInfo rm = new RMInfo()
                        {
                            Id = obj5.RelManagerId,
                            UserId = j.UserId,
                            SegmentId = obj5.SegmentId,
                            BranchId = obj5.BranchId,
                            LeaderId = obj5.LeaderId,
                            Name = j.UserName,
                            Email = j.Email,
                            Percentage = j.Percentage,
                            UsePercent = j.UsePercent,
                            Nominal = j.Nominal
                        };
                        detail.Rms.Add(rm);
                    }
                }
            }

            var query4 = from dt in _context.CrmDealTribes
                         join t in _context.CoreTribes
                         on dt.TribeId equals t.Id
                         where dt.DealId == dealId && !dt.IsDeleted
                         select new PercentTribeResponse
                         {
                             Id = t.Id,
                             Text = t.Tribe,
                             Percent = dt.Percentage,
                             UsePercent = dt.UsePercent,
                             Nominal = dt.Nominal
                         };

            detail.Tribes = await query4.ToListAsync();

            CrmDealProposal proposal = _context.CrmDealProposals.Where(a => a.DealId == dealId && a.IsActive && !a.IsDeleted).FirstOrDefault();
            if (proposal != null && proposal.Id > 0)
            {
                detail.proposal = GetProposalInfo(proposal.Id);
            }

            List<CrmDealPNL> pnls = await _context.CrmDealPNLs.Where(a => a.DealId == dealId && a.IsActive && !a.IsDeleted).ToListAsync();
            foreach (CrmDealPNL pnl in pnls)
            {
                if (pnl != null && pnl.Id > 0)
                {
                    if (pnl.DocumentType == 1)
                    {
                        detail.pricing = GetPricingInfo(pnl.Id, pnl.DocumentType);
                    }
                    else
                    {
                        detail.agreement = GetPricingInfo(pnl.Id, pnl.DocumentType);
                    }
                }

            }

            int[] ages = { 0, 0, 0, 0, 0 };
            CrmDealHistoryType type = GetDealHistoryType("stage");

            var q = from history in _context.CrmDealHistories
                    where history.DealId == dealId && history.TypeId == type.Id
                    orderby history.ActionDate
                    select new CrmDealHistory()
                    {
                        CurData = history.CurData,
                        PrevData = history.PrevData,
                        ActionDate = history.ActionDate
                    };

            List<CrmDealHistory> changeStages = await q.ToListAsync();

            if (changeStages == null || changeStages.Count == 0)
            {
                ages[detail.Stage.Id - 1] = detail.Age;
            }
            else
            {
                DateTime lastDate = DateTime.Today;
                CrmDealHistory last = changeStages.Last();
                foreach (CrmDealHistory history in changeStages)
                {
                    int nDays = (lastDate - history.ActionDate).Days;
                    lastDate = history.ActionDate;
                    try
                    {
                        ages[Int32.Parse(history.CurData) - 1] += nDays;
                        if (history.Equals(last))
                        {
                            ages[Int32.Parse(history.PrevData) - 1] += (history.ActionDate - obj.DealDate).Days;
                        }

                    }
                    catch
                    {
                        continue;
                    }
                }

            }

            // sudah di atas detail.Pic = GetInternalMember(dealId, "pic")
            detail.Stages = ages.ToList();
            detail.history = GetDealHistory(dealId);

            return detail;
        }

        [Authorize(Policy = "ApiUser")]
        [HttpGet("deal/{clientId}/{state}")]
        public async Task<ActionResult<List<GenericInfo>>> GetDealByClientId(int clientId, int state)
        {
            var query = from a in _context.CrmDeals
                        where !a.IsDeleted && a.ClientId == clientId && a.StateId == state
                        select new GenericInfo()
                        {
                            Id = a.Id,
                            Text = a.Name
                        };

            return await query.ToListAsync();
        }


        /**
         * @api {post} /pipeline/invoice POST invoice
         * @apiVersion 1.0.0
         * @apiName PostInvoice
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} id                    Id dari to be invoiced yang ingin diubah jadi invoiced
         * @apiParam {Number} clientId              Id dari client
         * @apiParam {Number} dealId                Id dari deal yang bersangkutan
         * @apiParam {Number} userId                User Id yang mengupload
         * @apiParam {Date} invoideDate             Tanggal invoice, format YYYY-MM-DD
         * @apiParam {Number} amount                Jumlah yang diinvoice
         * @apiParam {string} remarks               Keterangan
         * @apiParam {Number} contactId             Id dari orang yang dikirimkan
         * @apiParam {String} filename              File yang diupload
         * @apiParam {String} fileBase64            Isi file dalam base64
         * @apiParam {Array} rms                    UserId dan persentase dari RM
         * @apiParam {Array} tribes                 TribeId dan persentase dari tribe
         *   
         * @apiParamExample {json} Request-Example:
         *   {
         *     "id": 2,
         *     "clientId": 2,
         *     "dealId": 1,
         *     "userId": 1,
         *     "picId": 35,
         *     "invoiceDate": "2020-06-30",
         *     "amount": 1000000,
         *     "remarks": "Invoice untuk termin 1",
         *     "contactId": 1,
         *     "filename": "data.txt",
         *     "fileBase64": "Base64 dari file... ",
         *     "rms": [
         *       {
         *         "userId": 2,
         *         "percent": 100,
         *         "usePercent": true,
         *         "nominal": 0
         *       }
         *     ],
         *     "tribes": [
         *       {
         *         "tribeId": 1,
         *         "percent": 100,
         *         "usePercent": true,
         *         "nominal": 0
         *       }
         *     ]
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   {
         *       "id": 1,
         *       "dealId": 1,
         *       "amount": 15000000,
         *       "periodId": 6,
         *       "invoiceDate": "2020-06-22T00:00:00",
         *       "filename": "eufnclwb.nl2.pdf",
         *       "originalFilename": "PNL File.pdf",
         *       "remarks": "Test dulu",
         *       "isToBe": false,
         *       "createdDate": "2020-06-04T15:55:38.2357477",
         *       "createdBy": 1,
         *       "lastUpdated": "2020-06-22T15:11:14.0496283+07:00",
         *       "lastUpdatedBy": 1,
         *       "isDeleted": false,
         *       "deletedBy": 0,
         *       "deletedDate": null
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         * @apiError NotFound id atau dealId salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost("invoice")]
        public async Task<ActionResult<CrmDealInvoice>> PostInvoice(PostInvoiceUploadRequest request)
        {
            CrmDeal deal = _context.CrmDeals.Find(request.DealId);
            if (deal == null)
            {
                return NotFound(new { error = "Deal not found. Please check dealId" });
            }

            CrmDealInvoice invoice = _context.CrmDealInvoices.Find(request.Id);
            if (invoice == null)
            {
                return NotFound(new { error = "To be invoiced not found. Please check id" });
            }

            DateTime now = DateTime.Now;

            try
            {
                deal.LastUpdated = now;
                deal.LastUpdatedBy = request.UserId;
                deal.Probability = 100;
                deal.ClientId = request.ClientId;
                if (request.BranchId != 0 && deal.BranchId != request.BranchId) deal.BranchId = request.BranchId;
                _context.Entry(deal).State = EntityState.Modified;

                invoice.DealId = deal.Id;
                invoice.Amount = request.Amount;
                invoice.PeriodId = GetPeriodId(request.InvoiceDate, request.UserId);
                invoice.InvoiceDate = request.InvoiceDate;
                invoice.Remarks = request.Remarks;
                invoice.Filename = "";
                invoice.OriginalFilename = "";
                invoice.IsToBe = false;
                invoice.LastUpdated = now;
                invoice.LastUpdatedBy = request.UserId;

                if (request.FileBase64 != null && request.Filename != null)
                {
                    var error = SaveFileUpload(request.FileBase64, request.Filename, request.DealId);
                    if (error.Code.Equals("ok"))
                    {
                        string[] names = error.Description.Split(separator);
                        if (names.Length >= 3)
                        {
                            invoice.OriginalFilename = request.Filename;
                            invoice.Filename = names[1];
                        }
                    }
                    else
                    {
                        return BadRequest(new { error = "Error in saving file" });
                    }
                }

                _context.Entry(invoice).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                await addOrUpdatePIC(request.DealId, request.PicId, request.UserId, now);

                await UpdateTribeUserInvoice(request.Tribes, request.Rms, invoice.Id, invoice.Amount);
                await UpdateDealTribes(request.Tribes, request.DealId, invoice.Amount, request.UserId);

                await AddDealHistory("invoiced", deal.Id, invoice.OriginalFilename, invoice.Id.ToString(), invoice.InvoiceDate, request.UserId, now, request.UserId, "Invoice", "", "", 0, 0, 0, request.Remarks, request.Amount, "", request.ContactId);
                return invoice;
            }
            catch
            {
                return BadRequest(new { error = "Error updating database. Please check clientId" });
            }
        }

        /**
         * @api {put} /pipeline/invoice PUT invoice
         * @apiVersion 1.0.0
         * @apiName PutInvoice
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} id                    Id dari to be invoiced yang ingin diubah jadi invoiced
         * @apiParam {Number} clientId              Id dari client
         * @apiParam {Number} dealId                Id dari deal yang bersangkutan
         * @apiParam {Number} userId                User Id yang mengupload
         * @apiParam {Date} invoideDate             Tanggal invoice, format YYYY-MM-DD
         * @apiParam {Number} amount                Jumlah yang diinvoice
         * @apiParam {string} remarks               Keterangan
         * @apiParam {Number} contactId             Id dari orang yang dikirimkan
         * @apiParam {String} filename              File yang diupload
         * @apiParam {String} fileBase64            Isi file dalam base64
         * @apiParam {Array} rms                    UserId dan persentase dari RM
         * @apiParam {Array} tribes                 TribeId dan persentase dari tribe
         *   
         * @apiParamExample {json} Request-Example:
         *   {
         *     "id": 2,
         *     "clientId": 2,
         *     "dealId": 1,
         *     "userId": 1,
         *     "invoiceDate": "2020-06-30",
         *     "amount": 1000000,
         *     "remarks": "Invoice untuk termin 1",
         *     "contactId": 1,
         *     "filename": "data.txt",
         *     "fileBase64": "Base64 dari file... ",
         *     "rms": [
         *       {
         *         "userId": 2,
         *         "percent": 100,
         *         "usePercent": true,
         *         "nominal": false
         *       }
         *     ],
         *     "tribes": [
         *       {
         *         "tribeId": 1,
         *         "percent": 100,
         *         "usePercent": true,
         *         "nominal": false
         *       }
         *     ]
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   {
         *       "id": 1,
         *       "dealId": 1,
         *       "amount": 15000000,
         *       "periodId": 6,
         *       "invoiceDate": "2020-06-22T00:00:00",
         *       "filename": "eufnclwb.nl2.pdf",
         *       "originalFilename": "PNL File.pdf",
         *       "remarks": "Test dulu",
         *       "isToBe": false,
         *       "createdDate": "2020-06-04T15:55:38.2357477",
         *       "createdBy": 1,
         *       "lastUpdated": "2020-06-22T15:11:14.0496283+07:00",
         *       "lastUpdatedBy": 1,
         *       "isDeleted": false,
         *       "deletedBy": 0,
         *       "deletedDate": null
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         * @apiError NotFound id atau dealId salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPut("invoice")]
        public async Task<ActionResult<CrmDealInvoice>> PutInvoice(PostInvoiceUploadRequest request)
        {
            CrmDeal deal = _context.CrmDeals.Find(request.DealId);
            if (deal == null)
            {
                return NotFound(new { error = "Deal not found. Please check dealId" });
            }

            CrmDealInvoice invoice = _context.CrmDealInvoices.Find(request.Id);
            if (invoice == null)
            {
                return NotFound(new { error = "To be invoiced not found. Please check id" });
            }

            DateTime now = DateTime.Now;

            try
            {
                if (request.BranchId != 0 && deal.BranchId != request.BranchId)
                {
                    deal.BranchId = request.BranchId;
                    _context.Entry(deal).State = EntityState.Modified;
                }
                if (deal.ClientId != request.ClientId)
                {
                    deal.ClientId = request.ClientId;
                    deal.LastUpdated = now;
                    deal.LastUpdatedBy = request.UserId;
                    _context.Entry(deal).State = EntityState.Modified;
                }

                invoice.DealId = deal.Id;
                invoice.Amount = request.Amount;
                invoice.PeriodId = GetPeriodId(request.InvoiceDate, request.UserId);
                invoice.InvoiceDate = request.InvoiceDate;
                invoice.Remarks = request.Remarks;
                invoice.IsToBe = false;
                invoice.LastUpdated = now;
                invoice.LastUpdatedBy = request.UserId;

                if (request.FileBase64 != null && request.Filename != null)
                {
                    var error = SaveFileUpload(request.FileBase64, request.Filename, request.DealId);
                    if (error.Code.Equals("ok"))
                    {
                        string[] names = error.Description.Split(separator);
                        if (names.Length >= 3)
                        {
                            invoice.OriginalFilename = request.Filename;
                            invoice.Filename = names[1];
                        }
                    }
                    else
                    {
                        return BadRequest(new { error = "Error in saving file" });
                    }
                }

                _context.Entry(invoice).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                await addOrUpdatePIC(request.DealId, request.PicId, request.UserId, now);

                await UpdateTribeInvoice(request.Tribes, request.DealId, request.Amount, now, request.UserId);
                await UpdateUserInvoice(request.Rms, request.DealId, request.Amount, now, request.UserId);
                await UpdateTribeUserInvoice(request.Tribes, request.Rms, invoice.Id, invoice.Amount);
                await UpdateDealTribes(request.Tribes, request.DealId, invoice.Amount, request.UserId);

                await EditDealHistory("invoiced", deal.Id, invoice.OriginalFilename, invoice.Id.ToString(), invoice.InvoiceDate, request.UserId, now, request.UserId, "Invoice", "", "", 0, 0, 0, request.Remarks, request.Amount, "", request.ContactId);
                return invoice;
            }
            catch
            {
                return BadRequest(new { error = "Error updating database. Please check clientId" });
            }
        }


        /**
         * @api {post} /pipeline/invoicenodeal POST invoice no deal
         * @apiVersion 1.0.0
         * @apiName PostInvoiceNoDeal
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} id                    0 untuk POST
         * @apiParam {Number} clientId              Id dari client
         * @apiParam {Number} branchId              Id dari cabang
         * @apiParam {string} dealName              Nama deal yang bersangkutan
         * @apiParam {Number} userId                User Id yang mengupload
         * @apiParam {Date} invoideDate             Tanggal invoice, format YYYY-MM-DD
         * @apiParam {Number} amount                Jumlah yang diinvoice
         * @apiParam {string} remarks               Keterangan
         * @apiParam {Number} contactId             Id dari orang yang dikirimkan, 0 Kalau ngga ada
         * @apiParam {String} filename              File yang diupload
         * @apiParam {String} fileBase64            Isi file dalam base64
         * @apiParam {Array} rms                    UserId dan persentase dari RM
         * @apiParam {Array} tribes                 TribeId dan persentase dari tribe
         *   
         * @apiParamExample {json} Request-Example:
         *   {
         *     "id": 2,
         *     "clientId": 2,
         *     "branchId": 1
         *     "dealName": "Budaya Organisasi",
         *     "userId": 1,
         *     "picId": 35,
         *     "invoiceDate": "2020-06-30",
         *     "amount": 1000000,
         *     "remarks": "Invoice untuk termin 1",
         *     "contactId": 1,
         *     "filename": "data.txt",
         *     "fileBase64": "Base64 dari file... ",
         *     "rms": [
         *       {
         *         "userId": 2,
         *         "percent": 100
         *       }
         *     ],
         *     "tribes": [
         *       {
         *         "tribeId": 1,
         *         "percent": 100
         *       }
         *     ]
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   {
         *       "id": 1,
         *       "dealId": 1,
         *       "amount": 15000000,
         *       "periodId": 6,
         *       "invoiceDate": "2020-06-22T00:00:00",
         *       "filename": "eufnclwb.nl2.pdf",
         *       "originalFilename": "PNL File.pdf",
         *       "remarks": "Test dulu",
         *       "isToBe": false,
         *       "createdDate": "2020-06-04T15:55:38.2357477",
         *       "createdBy": 1,
         *       "lastUpdated": "2020-06-22T15:11:14.0496283+07:00",
         *       "lastUpdatedBy": 1,
         *       "isDeleted": false,
         *       "deletedBy": 0,
         *       "deletedDate": null
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         * @apiError NotFound id atau dealId salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost("invoicenodeal")]
        public async Task<ActionResult<CrmDealInvoice>> PostInvoiceNoDeal(PostInvoiceNoDeal request)
        {
            CrmDealState state = GetDealState("won");

            // TODO beberapa dari ID ini di hard code.
            DateTime now = DateTime.Now;

            CrmDeal deal = new CrmDeal()
            {
                Name = request.DealName,
                DealDate = DateTime.Now,
                Probability = 100,
                ClientId = request.ClientId,
                StageId = 5,
                SegmentId = 4,
                BranchId = request.BranchId,
                StateId = state.Id,
                CreatedDate = now,
                CreatedBy = request.UserId,
                LastUpdated = now,
                LastUpdatedBy = request.UserId,
                IsDeleted = false
            };
            _context.CrmDeals.Add(deal);
            await _context.SaveChangesAsync();

            try
            {
                await addOrUpdatePIC(deal.Id, request.PicId, request.UserId, now);

                CrmDealInvoice invoice = new CrmDealInvoice();

                invoice.DealId = deal.Id;
                invoice.Amount = request.Amount;
                invoice.PeriodId = GetPeriodId(request.InvoiceDate, request.UserId);
                invoice.InvoiceDate = request.InvoiceDate;
                invoice.Remarks = request.Remarks;
                invoice.Filename = "";
                invoice.OriginalFilename = "";
                invoice.RootFolder = _options.DataRootDirectory;
                invoice.IsToBe = false;
                invoice.CreatedDate = now;
                invoice.CreatedBy = request.UserId;
                invoice.LastUpdated = now;
                invoice.LastUpdatedBy = request.UserId;

                if (request.FileBase64 != null && request.Filename != null)
                {
                    var error = SaveFileUpload(request.FileBase64, request.Filename, deal.Id);
                    if (error.Code.Equals("ok"))
                    {
                        string[] names = error.Description.Split(separator);
                        if (names.Length >= 3)
                        {
                            invoice.OriginalFilename = request.Filename;
                            invoice.Filename = names[1];
                        }
                    }
                    else
                    {
                        return BadRequest(new { error = "Error in saving file" });
                    }
                }
                _context.CrmDealInvoices.Add(invoice);
                await _context.SaveChangesAsync();

                await UpdateTribeInvoice(request.Tribes, invoice.DealId, invoice.Amount, now, request.UserId);
                await UpdateUserInvoice(request.Rms, invoice.DealId, invoice.Amount, now, request.UserId);
                await UpdateTribeUserInvoice(request.Tribes, request.Rms, invoice.Id, invoice.Amount);
                await UpdateDealTribes(request.Tribes, invoice.DealId, invoice.Amount, request.UserId);

                await AddDealHistory("created", deal.Id, "", "", deal.DealDate, request.UserId, now, request.UserId, "Deal Created", "", "", 0, 0, 0);
                await AddDealHistory("invoiced", deal.Id, invoice.OriginalFilename, invoice.Id.ToString(), invoice.InvoiceDate, request.UserId, now, request.UserId, "Invoice", "", "", 0, 0, 0, request.Remarks, request.Amount, "", request.ContactId);

                return invoice;
            }
            catch
            {
                return BadRequest(new { error = "Error updating database. Please check clientId" });
            }
        }

        /**
         * @api {get} /pipeline/invoice/cancel/{invoiceId}/{userId} GET ubah invoiced jadi to-be-invoiced
         * @apiVersion 1.0.0
         * @apiName CancelInvoice
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("invoice/cancel/{invoiceId}/{userId}")]
        public async Task<ActionResult<CrmDealInvoice>> CancelInvoice(int invoiceId, int userId)
        {
            CrmDealInvoice invoice = _context.CrmDealInvoices.Find(invoiceId);
            if (invoice == null)
            {
                return NotFound(new { error = "To be invoiced not found. Please check id" });
            }

            DateTime now = DateTime.Now;

            try
            {
                invoice.Filename = "";
                invoice.OriginalFilename = "";
                invoice.RootFolder = _options.DataRootDirectory;
                invoice.IsToBe = true;
                invoice.LastUpdated = now;
                invoice.LastUpdatedBy = userId;

                _context.Entry(invoice).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                await DeleteTribeUserInvoice(invoiceId);

                await AddDealHistory("invoicedcanceled", invoice.DealId, invoice.OriginalFilename, invoice.Id.ToString(), invoice.InvoiceDate, userId, now, userId, "Invoice", "", "", 0, 0, 0, "", 0, "", 0);
                return invoice;
            }
            catch
            {
                return BadRequest(new { error = "Error updating database. Please check clientId" });
            }
        }


        /**
         * @api {get} /Pipeline/target/status/{year}/{id} GET target status
         * @apiVersion 1.0.0
         * @apiName GetTargetStatus
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiSuccessExample Success-Response:
         * {
         *     "id": 5,
         *     "text": "Targets not set."
         * }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("target/status/{year}/{id}")]
        public ActionResult<GenericInfo> GetTargetStatus(int year, int id)
        {
            if (!_context.CrmRelManagers.Where(a => a.UserId == id && a.isActive && !a.IsDeleted).Any()) return NotFound();

            return DoGetTargetStatus(year, id);
        }

        /**
         * @api {get} /Pipeline/target GET target
         * @apiVersion 1.0.0
         * @apiName GetTarget
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiSuccessExample Success-Response:
         * [
         *     {
         *         "id": 1,
         *         "text": "# of Proposals"
         *     },
         *     {
         *         "id": 2,
         *         "text": "Proposal Value"
         *     },
         *     {
         *         "id": 3,
         *         "text": "# of Sales Visits"
         *     },
         *     {
         *         "id": 4,
         *         "text": "Sales"
         *     }
         * ]
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("target")]
        public async Task<ActionResult<List<GenericInfo>>> GetTarget()
        {
            var query = from kpi in _context.CrmKpis
                        where !kpi.IsDeleted
                        select new GenericInfo()
                        {
                            Id = kpi.Id,
                            Text = kpi.Indicator
                        };

            return await query.ToListAsync();
        }

        // Ngga yakin ini masih dipakai apa ngga
        [Authorize(Policy = "ApiUser")]
        [HttpGet("target/{year}/{id}/{userId}")]
        public async Task<ActionResult<PostTargetRequest>> GetTarget(int year, int id, int userId)
        {
            if (!_context.CrmRelManagers.Where(a => a.isActive && !a.IsDeleted && a.UserId == id).Any()) return NotFound(new { error = "RM not found. Please check Id" });

            PostTargetRequest response = new PostTargetRequest();
            response.Id = id;
            response.Type = "rm";
            response.UserId = userId;
            response.Reject = false;
            response.Approve = false;
            response.items = new List<TargetItem>();

            var query = from target in _context.CrmDealTargets
                        join period in _context.CrmPeriods on target.PeriodId equals period.Id
                        join kpi in _context.CrmKpis on target.KpiId equals kpi.Id
                        where target.LinkedId == id && period.Year == year && target.Type.Equals("rm") && !target.IsDeleted
                        select new
                        {
                            TargetId = target.Id,
                            TargetAmount = target.Target,
                            KpiId = kpi.Id,
                            KpiText = kpi.Indicator,
                            target.Status,
                            period.Year,
                            period.Month
                        };
            var objs = await query.OrderBy(a => a.Month).ToListAsync();
            foreach (var obj in objs)
            {
                if (!response.items.Where(a => a.Year == obj.Year && a.Month == obj.Month).Any())
                {
                    TargetItem newItem = new TargetItem()
                    {
                        Month = obj.Month,
                        Year = obj.Year,
                        Targets = new List<GenericAmount>()
                    };
                    response.items.Add(newItem);
                }
                TargetItem item = response.items.Where(a => a.Year == obj.Year && a.Month == obj.Month).FirstOrDefault();
                if (item != null)
                {
                    if (item.Targets != null)
                    {
                        item.Targets.Add(new GenericAmount()
                        {
                            Id = obj.TargetId,
                            Amount = obj.TargetAmount
                        });
                    }
                }
                if (obj.Status.Equals("Approved")) response.Approve = true;
                else if (obj.Status.Equals("Rejected")) response.Reject = true;
            }

            return response;
        }

        /**
         * @api {get} /Pipeline/target/{year}/{id} GET target by userId
         * @apiVersion 1.0.0
         * @apiName GetTargetShort
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiSuccessExample Success-Response:
         * {
         *     "id": 3,
         *     "type": "rm",
         *     "status": "Need approval",
         *     "items": [
         *         {
         *             "month": 1,
         *             "year": 2021,
         *             "targets": [
         *                 {
         *                     "id": 1,
         *                     "amount": 2000
         *                 },
         *                 {
         *                     "id": 2,
         *                     "amount": 2000
         *                 },
         *                 {
         *                     "id": 3,
         *                     "amount": 2000
         *                 },
         *                 {
         *                     "id": 4,
         *                     "amount": 2000
         *                 }
         *             ]
         *         }
         *     ]
         * }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("target/{year}/{id}")]
        public async Task<ActionResult<GetTargetResponse>> GetTargetShort(int year, int id)
        {
            if (!_context.CrmRelManagers.Where(a => a.isActive && !a.IsDeleted && a.UserId == id).Any()) return NotFound(new { error = "RM not found. Please check Id" });

            GetTargetResponse response = new GetTargetResponse();
            response.Id = id;
            response.Type = "rm";
            response.Status = "Need approval";
            response.items = new List<TargetItem>();

            var query = from target in _context.CrmDealTargets
                        join period in _context.CrmPeriods on target.PeriodId equals period.Id
                        where target.LinkedId == id && period.Year == year && target.Type.Equals("rm") && !target.IsDeleted
                        select new
                        {
                            TargetId = target.Id,
                            TargetAmount = target.Target,
                            KpiId = target.KpiId,
                            target.Status,
                            period.Year,
                            period.Month
                        };
            var objs = await query.OrderBy(a => a.Month).ThenBy(a => a.KpiId).ToListAsync();
            foreach (var obj in objs)
            {
                if (!response.items.Where(a => a.Year == obj.Year && a.Month == obj.Month).Any())
                {
                    TargetItem newItem = new TargetItem()
                    {
                        Month = obj.Month,
                        Year = obj.Year,
                        Targets = new List<GenericAmount>()
                    };
                    response.items.Add(newItem);
                }
                TargetItem item = response.items.Where(a => a.Year == obj.Year && a.Month == obj.Month).FirstOrDefault();
                if (item != null)
                {
                    if (item.Targets != null)
                    {
                        item.Targets.Add(new GenericAmount()
                        {
                            Id = obj.KpiId,
                            Amount = obj.TargetAmount
                        });
                    }
                }
                if (obj.Status.Equals("Approved")) response.Status = "Approved";
                else if (obj.Status.Equals("Rejected")) response.Status = "Rejected";
            }

            return response;
        }

        /**
         * @api {post} /pipeline/target POST target 
         * @apiVersion 1.0.0
         * @apiName PostTarget
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * @apiDescription Untuk list targets, Idnya: 1 - Target no proposal, 3 - target sales visit, 2 - proposal value, 4 - sales
         *  
         * @apiParam {Number} ID        Id dari tribe/segment/branch/userId dari RM
         * @apiParam {String} Type      "tribe", "segment", "branch", atau "rm"
         * @apiParam {Number} userId    userId yang login
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "id": 1,
         *     "type": "branch",
         *     "userId": 1,
         *     "Reject": false,
         *     "Approve": true,
         *     "items": [
         *       {
         *         "month": 1,
         *         "year": 2020,
         *         "targets": [
         *           {
         *             "id": 1,
         *             "amount": 100
         *           },
         *           {
         *             "id": 2,
         *             "amount": 200
         *           },
         *           {
         *             "id": 3,
         *             "amount": 300
         *           },
         *           {
         *             "id": 4,
         *             "amount": 400
         *           }
         *         ]
         *       },
         *       {
         *         "month": 2,
         *         "year": 2020,
         *         "targets": [
         *           {
         *             "id": 1,
         *             "amount": 100
         *           },
         *           {
         *             "id": 2,
         *             "amount": 200
         *           },
         *           {
         *             "id": 3,
         *             "amount": 300
         *           },
         *           {
         *             "id": 4,
         *             "amount": 400
         *           }
         *         ]
         *       }
         *     ]
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost("target")]
        public async Task<ActionResult> PostTarget(PostTargetRequest request)
        {
            DateTime now = DateTime.Now;

            string status = "Need approval";
            if (request.Reject) status = "Rejected";
            else if (request.Approve) status = "Approved";

            request.Type = request.Type.StartsWith("relationship man") ? "rm" : request.Type;

            try
            {
                foreach (TargetItem item in request.items)
                {
                    int periodId = GetPeriodId(new DateTime(item.Year, item.Month, 1), request.UserId); // GetPeriod(item.Month, item.Year);
                    if (periodId != 0)
                    {
                        foreach (GenericAmount t in item.Targets)
                        {
                            CrmDealTarget curTarget = _context.CrmDealTargets.Where(a => a.KpiId == t.Id && a.Type == request.Type && a.LinkedId == request.Id && a.PeriodId == periodId).FirstOrDefault();
                            if (curTarget == null)
                            {
                                CrmDealTarget target = new CrmDealTarget()
                                {
                                    Target = t.Amount,
                                    PeriodId = periodId,
                                    KpiId = t.Id,
                                    LinkedId = request.Id,
                                    Type = request.Type,
                                    Status = status,
                                    CreatedDate = now,
                                    CreatedBy = request.UserId,
                                    LastUpdated = now,
                                    LastUpdatedBy = request.UserId,
                                    IsDeleted = false,
                                    DeletedBy = 0
                                };
                                _context.CrmDealTargets.Add(target);
                            }
                            else
                            {
                                curTarget.Target = t.Amount;
                                curTarget.Status = status;
                                curTarget.LastUpdated = now;
                                curTarget.LastUpdatedBy = request.UserId;
                                _context.Entry(curTarget).State = EntityState.Modified;
                            }
                        }
                    }
                }
                await _context.SaveChangesAsync();

            }
            catch
            {
                return BadRequest(new { error = "Error updating database" });
            }

            return NoContent();
        }

        /**
         * @api {get} /Pipeline/report/summary GET report summary
         * @apiVersion 1.0.0
         * @apiName GetReportSummary
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiSuccessExample Success-Response:
         * [
         *     {
         *         "title": "Projected Revenue in Jul 2020",
         *         "amount": 25000003,
         *         "percent": 69.05,
         *         "note": "Compare to Jul 2019"
         *     },
         *     {
         *         "title": "Invoice in Jul 2020",
         *         "amount": 25000000,
         *         "percent": 69.05,
         *         "note": "Compare to Jul 2019"
         *     },
         *     {
         *         "title": "Invoice in Jan - Jul 2020",
         *         "amount": 43000000,
         *         "percent": 17.0,
         *         "note": "Compare to Jan t0 Jul 2019"
         *     }
         * ]
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("report/summary")]
        public async Task<List<SummaryHistory>> GetReportSummary()
        {
            int curMonth = DateTime.Today.Month;
            int curYear = DateTime.Today.Year;

            List<SummaryHistory> response = new List<SummaryHistory>();

            DoubleLong d1 = await GetSummaryHistoryItem1(curMonth, curYear);
            DoubleLong d2 = await GetSummaryHistoryItem2(curMonth, curYear);
            DoubleLong d3 = await GetSummaryHistoryItem3(curMonth, curYear);

            // Get total projected of current month
            SummaryTypeReport projection = await GetProjectedInvoice("tribe", curMonth, curYear);
            long totalProjection = 0;
            foreach (SummaryReportRow row in projection.Items)
            {
                totalProjection += row.Amount1;
            }


            SummaryHistory h1 = new SummaryHistory();
            h1.Title = string.Join(" ", new[] { "Projected Revenue in", String.Format("{0:MMM yyyy}", new DateTime(curYear, curMonth, 1)) });
            h1.Amount = d1.Amount1;
            h1.Percent = d1.Amount2 == 0 ? 0 : Convert.ToInt32(Math.Round(Convert.ToSingle(Convert.ToSingle(d1.Amount1) / Convert.ToSingle(d1.Amount2) - 1.00) * 100));
            h1.Note = string.Join(" ", new[] { "Compare to", String.Format("{0:MMM yyyy}", new DateTime(curYear - 1, curMonth, 1)) });

            SummaryHistory h2 = new SummaryHistory();
            h2.Title = string.Join(" ", new[] { "Invoice in", String.Format("{0:MMM yyyy}", new DateTime(curYear, curMonth, 1)) });
            h2.Amount = d2.Amount1;
            h2.Percent = d2.Amount2 == 0 ? 0 : Convert.ToInt32(Math.Round(Convert.ToSingle(Convert.ToSingle(d2.Amount1) / Convert.ToSingle(d2.Amount2) - 1.00) * 100));
            h2.Note = string.Join(" ", new[] { "Compare to", String.Format("{0:MMM yyyy}", new DateTime(curYear - 1, curMonth, 1)) });

            SummaryHistory h3 = new SummaryHistory();
            h3.Title = string.Join(" ", new[] { "Invoice in Jan -", String.Format("{0:MMM yyyy}", new DateTime(curYear, curMonth, 1)) });
            h3.Amount = d3.Amount1 + totalProjection;
            h3.Percent = d3.Amount2 == 0 ? 0 : Convert.ToInt32(Math.Round(Convert.ToSingle(Convert.ToSingle(d3.Amount1) / Convert.ToSingle(d3.Amount2) - 1.00) * 100));

            if (curMonth == 1)
            {
                h3.Note = string.Join(" ", new[] { "Compare to", String.Format("{0:MMM yyyy}", new DateTime(curYear - 1, curMonth, 1)) });
            }
            else
            {
                h3.Note = string.Join(" ", new[] { "Compare to Jan -", String.Format("{0:MMM yyyy}", new DateTime(curYear - 1, curMonth, 1)) });
            }


            response.AddRange(new[] { h1, h2, h3 });
            return response;
        }

        /**
         * @api {get} /Pipeline/report/projected/{type}/{month}/{year} GET report projected
         * @apiVersion 1.0.0
         * @apiName GetProjectedInvoice
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * @apiParam {String} type      "tribe", "segment", "branch", atau "rm"
         * @apiParam {Number} month     bulan yang diinginkan
         * @apiParam {Number} year      tahun yang diinginkan
         * 
         * @apiSuccessExample Success-Response:
         * {
         *     "header": {
         *         "type": "tribe",
         *         "headers": [
         *             "To be invoiced",
         *             "Invoiced",
         *             "Total"
         *         ]
         *     },
         *     "items": [
         *         {
         *             "id": 2,
         *             "text": "Organizational Excellence",
         *             "amount1": 1000000,
         *             "amount2": 25000000,
         *             "amount3": 26000000
         *         },
         *         {
         *             "id": 1,
         *             "text": "Strategy & Execution",
         *             "amount1": 33000000,
         *             "amount2": 3000000,
         *             "amount3": 36000000
         *         }
         *     ]
         * }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("report/projected/{type}/{month}/{year}")]
        public async Task<SummaryTypeReport> GetProjectedInvoice(string type, int month, int year)
        {
            SummaryTypeReport response = new SummaryTypeReport();
            response.Header.Type = type;
            response.Header.Headers.AddRange(new[] { "To be invoiced", "Invoiced", "Total" });

            if (type.Trim().Equals("tribe"))
            {
                response.Items = await GetTribeProjectedInvoice("=", month, year);
            }
            else if (type.Trim().Equals("branch"))
            {
                response.Items = await GetBranchProjectedInvoice("=", month, year);
            }
            else if (type.Trim().Equals("segment"))
            {
                response.Items = await GetSegmentProjectedInvoice("=", month, year);
            }
            else if (type.Trim().Equals("rm"))
            {
                response.Items = await GetRmProjectedInvoice("=", month, year);
            }


            return response;
        }

        /**
         * @api {get} /Pipeline/report/invoice/{type}/{year1}/{year2}/{year3}/{fromMonth}/{toMonth} GET report invoice
         * @apiVersion 1.0.0
         * @apiName GetInvoiceReport
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * @apiParam {String} type      "tribe", "segment", "branch", atau "rm"
         * @apiParam {Number} year1     Tahun pertama, contoh 2018
         * @apiParam {Number} year2     Tahun kedua, contoh 2019
         * @apiParam {Number} year3     Tahun ketiga, contoh 2020
         * @apiParam {Number} fromMonth Mulai dari bulan kapan, misalnya 1 untuk Januari, 2 untuk Feb, dst
         * @apiParam {Number} toMonth   Sampai bulan kapan. Jadi kalau fromMonth sama dengan toMonth, maka hanya untuk bulan ybs. Jadi contohnya, kalau mau data untuk bulan Januari sampai Juli untuk tahun 2018, 2019, dan 2020, maka parameternya 2018/2019/2020/1/7
         * 
         * @apiSuccessExample Success-Response:
         * {
         *     "header": {
         *         "type": "tribe",
         *         "headers": [
         *             "2018",
         *             "2019",
         *             "2020"
         *         ]
         *     },
         *     "items": [
         *         {
         *             "id": 1,
         *             "text": "Strategy & Execution",
         *             "amount1": 8120,
         *             "amount2": 8960,
         *             "amount3": 2000000
         *         },
         *         {
         *             "id": 2,
         *             "text": "Organizational Excellence",
         *             "amount1": 14120,
         *             "amount2": 15960,
         *             "amount3": 1000000
         *         }
         *     ]
         * }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("report/invoice/{type}/{year1}/{year2}/{year3}/{fromMonth}/{toMonth}")]
        public async Task<ActionResult<SummaryTypeReport>> GetInvoiceReport(string type, int year1, int year2, int year3, int fromMonth, int toMonth)
        {
            SummaryTypeReport response = new SummaryTypeReport();
            response.Header.Type = type;
            response.Header.Headers.AddRange(new[] { year1.ToString(), year2.ToString(), year3.ToString() });

            string sql = "";
            if (type.Trim().Equals("tribe"))
            {
                sql = string.Join(" ", new[] {
                    "select ISNULL(a.Id, ISNULL(b.Id, c.Id)) as Id, ISNULL(a.Tribe, ISNULL(b.Tribe, c.Tribe)) as Text, ISNULL(a.Amount, 0) as Amount1, ISNULL(b.Amount, 0) as Amount2, ISNULL(c.Amount, 0) as Amount3 FROM (",
                    GetTribeInvoiceSql("Amount", year1, fromMonth, toMonth),
                    ") as a FULL OUTER JOIN (",
                    GetTribeInvoiceSql("Amount", year2, fromMonth, toMonth),
                    ") as b on a.Id = b.Id FULL OUTER JOIN (",
                    GetTribeInvoiceSql("Amount", year3, fromMonth, toMonth),
                    ") as c on c.id = b.Id"
                });
            }
            else if (type.Trim().Equals("segment"))
            {
                sql = string.Join(" ", new[] {
                    "select ISNULL(a.Id, ISNULL(b.Id, c.Id)) as Id, ISNULL(a.Segment, ISNULL(b.Segment, c.Segment)) as Text, ISNULL(a.Amount, 0) as Amount1, ISNULL(b.Amount, 0) as Amount2, ISNULL(c.Amount, 0) as Amount3 FROM (",
                    GetSegmentInvoiceSql("Amount", year1, fromMonth, toMonth),
                    ") as a FULL OUTER JOIN (",
                    GetSegmentInvoiceSql("Amount", year2, fromMonth, toMonth),
                    ") as b on a.Id = b.Id FULL OUTER JOIN (",
                    GetSegmentInvoiceSql("Amount", year3, fromMonth, toMonth),
                    ") as c on c.id = b.Id"
                });
            }
            else if (type.Trim().Equals("branch"))
            {
                sql = string.Join(" ", new[] {
                    "select ISNULL(a.Id, ISNULL(b.Id, c.Id)) as Id, ISNULL(a.Branch, ISNULL(b.Branch, c.Branch)) as Text, ISNULL(a.Amount, 0) as Amount1, ISNULL(b.Amount, 0) as Amount2, ISNULL(c.Amount, 0) as Amount3 FROM (",
                    GetBranchInvoiceSql("Amount", year1, fromMonth, toMonth),
                    ") as a FULL OUTER JOIN (",
                    GetBranchInvoiceSql("Amount", year2, fromMonth, toMonth),
                    ") as b on a.Id = b.Id FULL OUTER JOIN (",
                    GetBranchInvoiceSql("Amount", year3, fromMonth, toMonth),
                    ") as c on c.id = b.Id"
                });
            }
            else if (type.Trim().Equals("rm"))
            {
                sql = string.Join(" ", new[] {
                    "select z.Id, z.Text, sum(z.Amount1) as Amount1, sum(z.Amount2) as Amount2, sum(z.Amount3) as Amount3 from (",
                    "select ISNULL(a.Id, ISNULL(b.Id, c.Id)) as Id, ISNULL(a.Firstname, ISNULL(b.Firstname, c.Firstname)) as Text, CAST(ISNULL(a.Amount, 0) AS bigint) as Amount1, CAST(ISNULL(b.Amount, 0) as bigint) as Amount2, CAST(ISNULL(c.Amount, 0) AS bigint) as Amount3 FROM (",
                    GetRmInvoiceSql("Amount", year1, fromMonth, toMonth),
                    ") as a FULL OUTER JOIN (",
                    GetRmInvoiceSql("Amount", year2, fromMonth, toMonth),
                    ") as b on a.Id = b.Id FULL OUTER JOIN (",
                    GetRmInvoiceSql("Amount", year3, fromMonth, toMonth),
                    ") as c on c.id = b.Id",
                    ") as z group by z.Id, z.Text order by z.Id"
                });

            }
            if (sql.Length > 0)
            {
                SummaryTypeReport projected = await GetProjectedInvoice(type, toMonth, year3);
                response.Items = await _context.SummaryReportRows.FromSql(sql).ToListAsync();

                foreach (SummaryReportRow row in projected.Items)
                {
                    SummaryReportRow invoiceRow = response.Items.Where(a => a.Text.Equals(row.Text)).FirstOrDefault();
                    if (invoiceRow == null)
                    {
                        response.Items.Add(row);
                    }
                    else
                    {
                        invoiceRow.Amount3 += row.Amount1;      // Add the projection
                    }
                }

                return response;
            }

            return NotFound();
        }

        /**
         * @api {get} /Pipeline/report/chart/{type}/{id}/{fromMonth}/{toMonth}/{year} GET report chart
         * @apiVersion 1.0.0
         * @apiName GetChartReport
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * @apiParam {String} type      "tribe", "segment", "branch", atau "rm"
         * @apiParam {Number} id        Id dari tribe, segment, branch, atau userId dari rm
         * @apiParam {Number} fromMonth Mulai dari bulan kapan, misalnya 1 untuk Januari, 2 untuk Feb, dst
         * @apiParam {Number} toMonth   Sampai bulan kapan. Jadi kalau fromMonth sama dengan toMonth, maka hanya untuk bulan ybs. 
         * @apiParam {Number} year      Tahun yang diinginkan. Harus tahun 2020 ke atas. Tahun 2019 ke bawah ga ada data.
         * 
         * @apiSuccessExample Success-Response:
         * [
         *     {
         *         "title": "# of Proposals",
         *         "xaxis": [
         *             "January",
         *             "February",
         *             "March",
         *             "April",
         *             "May",
         *             "June",
         *             "July"
         *         ],
         *         "series": [
         *             {
         *                 "id": 1,
         *                 "target": 0,
         *                 "actual": 26,
         *                 "month": 6,
         *                 "achievement": 0
         *             }
         *         ]
         *     },
         *     {
         *         "title": "Proposal Value",
         *         "xaxis": [
         *             "January",
         *             "February",
         *             "March",
         *             "April",
         *             "May",
         *             "June",
         *             "July"
         *         ],
         *         "series": [
         *             {
         *                 "id": 1,
         *                 "target": 0,
         *                 "actual": 127423056971,
         *                 "month": 6,
         *                 "achievement": 0
         *             }
         *         ]
         *     },
         *     {
         *         "title": "# of Sales Visits",
         *         "xaxis": [
         *             "January",
         *             "February",
         *             "March",
         *             "April",
         *             "May",
         *             "June",
         *             "July"
         *         ],
         *         "series": [
         *             {
         *                 "id": 1,
         *                 "target": 0,
         *                 "actual": 2,
         *                 "month": 3,
         *                 "achievement": 0
         *             },
         *             {
         *                 "id": 1,
         *                 "target": 0,
         *                 "actual": 7,
         *                 "month": 5,
         *                 "achievement": 0
         *             },
         *             {
         *                 "id": 1,
         *                 "target": 0,
         *                 "actual": 50,
         *                 "month": 6,
         *                 "achievement": 0
         *             }
         *         ]
         *     },
         *     {
         *         "title": "Sales",
         *         "xaxis": [
         *             "January",
         *             "February",
         *             "March",
         *             "April",
         *             "May",
         *             "June",
         *             "July"
         *         ],
         *         "series": [
         *             {
         *                 "id": 1,
         *                 "target": 0,
         *                 "actual": 22,
         *                 "month": 6,
         *                 "achievement": 0
         *             },
         *             {
         *                 "id": 1,
         *                 "target": 0,
         *                 "actual": 900000,
         *                 "month": 7,
         *                 "achievement": 0
         *             }
         *         ]
         *     }
         * ]
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("report/chart/{type}/{id}/{fromMonth}/{toMonth}/{year}")]
        public async Task<ActionResult<List<ChartData>>> GetChartReport(string type, int id, int fromMonth, int toMonth, int year)
        {

            var query = from kpi in _context.CrmKpis
                        where !kpi.IsDeleted
                        select new GenericInfo()
                        {
                            Id = kpi.Id,
                            Text = kpi.Indicator
                        };

            // type ada 4, graph ada 4, total 16
            List<GenericInfo> kpis = await query.ToListAsync();

            List<ChartData> response = new List<ChartData>();

            foreach (GenericInfo kpi in kpis)
            {
                ChartData data = await GetChartData(type, id, kpi, fromMonth, toMonth, year);
                if (data.Series == null) return BadRequest();
                foreach (ChartSeriesItem item in data.Series)
                {
                    if (year == 2020 && fromMonth < 6 && kpi.Id < 4)
                    {
                        var q = from actual in _context.CrmDealActualHistories
                                join per in _context.CrmPeriods
                                on actual.PeriodId equals per.Id
                                where actual.Type.Equals(type) && per.Month == item.Month && per.Year == year && actual.KpiId == kpi.Id && actual.LinkedId == id && actual.IsDeleted == false
                                select new { Actual = actual.Actual };
                        var o = q.FirstOrDefault();
                        if (o != null)
                        {
                            item.Actual += o.Actual;
                            if (item.Target != 0)
                            {
                                item.Achievement = Convert.ToInt32(Math.Round(Convert.ToSingle(Convert.ToSingle(item.Actual) / Convert.ToSingle(item.Target)) * 100));
                            }

                        }
                    }

                }
                response.Add(data);
            }

            return response;
        }

        /**
         * @api {get} /Pipeline/export/chart/{type}/{id}/{fromMonth}/{toMonth}/{year} GET export chart
         * @apiVersion 1.0.0
         * @apiName GetExportChartData
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * @apiParam {String} type      "tribe", "segment", "branch", atau "rm"
         * @apiParam {Number} id        Id dari tribe, segment, branch, atau userId dari rm
         * @apiParam {Number} fromMonth Mulai dari bulan kapan, misalnya 1 untuk Januari, 2 untuk Feb, dst
         * @apiParam {Number} toMonth   Sampai bulan kapan. Jadi kalau fromMonth sama dengan toMonth, maka hanya untuk bulan ybs. 
         * @apiParam {Number} year      Tahun yang diinginkan. Harus tahun 2020 ke atas. Tahun 2019 ke bawah ga ada data.
         * 
         * @apiSuccessExample Success-Response:
         * {
         *     "sheet1": {
         *         "headers": [
         *             "No.",
         *             "Company",
         *             "Tribe",
         *             "Segment",
         *             "RM",
         *             "Deal Name",
         *             "Amount"
         *         ],
         *         "items": [
         *             {
         *                 "no": 1,
         *                 "company": "DANONE INDONESIA, PT",
         *                 "tribe": "",
         *                 "segment": "Private",
         *                 "rm": "Muhammad Iman Rizki, Grace Louise Harsa",
         *                 "dealName": "XR GAME SIMULATION FOR AQUA DANONE MANIFESTO",
         *                 "amount": 165000000
         *             },
         *             {
         *                 "no": 2,
         *                 "company": "ASURANSI WAHANA TATA, PT",
         *                 "tribe": "OE",
         *                 "segment": "Private",
         *                 "rm": "Muhammad Iman Rizki, GML",
         *                 "dealName": "Job Profile Development",
         *                 "amount": 294000000
         *             },
         *             {
         *                 "no": 3,
         *                 "company": "ASURANSI WAHANA TATA, PT",
         *                 "tribe": "OE",
         *                 "segment": "Private",
         *                 "rm": "Muhammad Iman Rizki, GML",
         *                 "dealName": "Job Profile Development",
         *                 "amount": 240000000
         *             },
         *             {
         *                 "no": 4,
         *                 "company": "BPJS KESEHATAN",
         *                 "tribe": "SNE",
         *                 "segment": "BUMN",
         *                 "rm": "Muhammad Iman Rizki",
         *                 "dealName": "Jasa Penyedia Fasilitator Pelatihan CSEP",
         *                 "amount": 409090909
         *             },
         *             {
         *                 "no": 5,
         *                 "company": "BPJS KESEHATAN",
         *                 "tribe": "SNE",
         *                 "segment": "BUMN",
         *                 "rm": "Muhammad Iman Rizki",
         *                 "dealName": "Jasa Penyedia Fasilitator Pelatihan CSEP",
         *                 "amount": 377272728
         *             },
         *             {
         *                 "no": 6,
         *                 "company": "ASURANSI ASEI INDONESIA, PT",
         *                 "tribe": "LDS",
         *                 "segment": "Private",
         *                 "rm": "Grace Louise Harsa",
         *                 "dealName": "Leadership In Digital Era",
         *                 "amount": 60000000
         *             },
         *             {
         *                 "no": 7,
         *                 "company": "ASTRA INTERNATIONAL Tbk, PT",
         *                 "tribe": "DLS",
         *                 "segment": "Private",
         *                 "rm": "",
         *                 "dealName": "LMS Enhancement",
         *                 "amount": 200000000
         *             },
         *             {
         *                 "no": 8,
         *                 "company": "ASTRA INTERNATIONAL Tbk, PT",
         *                 "tribe": "DLS",
         *                 "segment": "Private",
         *                 "rm": "",
         *                 "dealName": "LMS Enhancement",
         *                 "amount": 200000000
         *             },
         *             {
         *                 "no": 9,
         *                 "company": "BHINNEKA LIFE INDONESIA, PT",
         *                 "tribe": "OE",
         *                 "segment": "Private",
         *                 "rm": "Grace Louise Harsa",
         *                 "dealName": "Employee Engagement Survey",
         *                 "amount": 125000000
         *             },
         *             {
         *                 "no": 10,
         *                 "company": "COCA COLA AMATIL INDONESIA, PT",
         *                 "tribe": "LDS",
         *                 "segment": "Private",
         *                 "rm": "Grace Louise Harsa",
         *                 "dealName": "Effective Training Delivery",
         *                 "amount": 50000000
         *             }
         *         ]
         *     },
         *     "sheet2": {
         *         "headers": [
         *             "No.",
         *             "Amount",
         *             "Segment",
         *             "Month",
         *             "Year"
         *         ],
         *         "items": [
         *             {
         *                 "no": 1,
         *                 "amount": 1,
         *                 "segment": "BUMN",
         *                 "month": 2,
         *                 "year": 2020
         *             },
         *             {
         *                 "no": 2,
         *                 "amount": 1,
         *                 "segment": "Government",
         *                 "month": 2,
         *                 "year": 2020
         *             },
         *             {
         *                 "no": 3,
         *                 "amount": 1,
         *                 "segment": "BUMN",
         *                 "month": 3,
         *                 "year": 2020
         *             }
         *         ]
         *     }
         * }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("export/chart/{type}/{id}/{fromMonth}/{toMonth}/{year}")]
        public async Task<ActionResult<ChartDataExport>> GetExportChartData(string type, int id, int fromMonth, int toMonth, int year)
        {
            ChartDataExport response = new ChartDataExport();

            int fromId = GetPeriodId(new DateTime(year, fromMonth, 1), 1);          // Gpp dihard code userId = 1 karena ngga akan buat periode baru
            int toId = GetPeriodId(new DateTime(year, toMonth, 1), 1);

            if (type.ToLower().Equals("tribe"))
            {
                response.Sheet1 = new ProposalDetailSheet();
                response.Sheet1.Headers = new List<string>(new[] { "No.", "Company", "Tribe", "Segment", "RM", "Deal Name", "Amount" });
                response.Sheet1.Items = await GetListProposalDetails(id, fromMonth, toMonth, year);


                response.Sheet2 = new SalesCallSummarySheet();
                response.Sheet2.Headers = new List<string>(new[] { "No.", "Amount", "Segment", "Month", "Year" });
                response.Sheet2.Items = new List<SegmentSalesCallSummary>();

                int fm = fromMonth;

                List<CrmSegment> segments = await _context.CrmSegments.Where(a => !a.IsDeleted).ToListAsync();
                int n = 1;

                do
                {
                    foreach (CrmSegment segment in segments)
                    {
                        SegmentSalesCallSummary summary = new SegmentSalesCallSummary()
                        {
                            No = n++,
                            Amount = 0,
                            Segment = segment.Segment,
                            Month = fm,
                            Year = year
                        };
                        response.Sheet2.Items.Add(summary);
                    }
                    List<VisitByTribe> visits = await _crmReportService.GetActualVisitsByTribe(id, fm, fm, year);
                    //visits.AddRange(await _crmReportService.GetActualVisitsNoTribe(fm, fm, year));
                    foreach (VisitByTribe visit in visits)
                    {
                        var q = from rm in _context.CrmRelManagers
                                join segment in _context.CrmSegments on rm.SegmentId equals segment.Id
                                where rm.UserId == visit.Id
                                select segment;
                        CrmSegment cs = q.FirstOrDefault();
                        if (cs != null)
                        {
                            SegmentSalesCallSummary item = response.Sheet2.Items.Where(a => a.Segment.Equals(cs.Segment) && a.Month == fm).FirstOrDefault();
                            if (item != null)
                            {
                                item.Amount += visit.Visit;
                            }
                        }
                        else
                        {
                            SegmentSalesCallSummary item = response.Sheet2.Items.Where(a => a.Segment.Equals("Others") && a.Month == fm).FirstOrDefault();
                            if (item != null)
                            {
                                item.Amount += visit.Visit;
                            }
                            else
                            {
                                SegmentSalesCallSummary s = new SegmentSalesCallSummary()
                                {
                                    No = n++,
                                    Amount = visit.Visit,
                                    Segment = "Others",
                                    Month = fm,
                                    Year = year
                                };
                                response.Sheet2.Items.Add(s);
                            }


                        }
                    }
                    fm++;
                } while (fm <= toMonth);

                /*
                var sql = string.Join(" ", new[] {
                    "select count(visit.Id) as Amount, segment.Segment, p.Month, p.Year, p.Month as No from dbo.CrmDealVisits as visit",
                    "join dbo.CrmDeals as deal on visit.DealId = deal.Id",
                    "join dbo.CrmSegments as segment on deal.SegmentId = segment.Id",
                    "join dbo.CrmPeriods as p on visit.PeriodId = p.Id",
                    "where visit.IsDeleted = 0 and visit.PeriodId >=", fromId.ToString(), "and visit.PeriodId <=", toId.ToString(),
                    "group by p.Year, p.Month, segment.Segment",
                    "order by p.Year, p.Month, segment.Segment"
                });

                response.Sheet2.Items = await _context.SegmentSalesCallSummaries.FromSql(sql).ToListAsync<SegmentSalesCallSummary>();
                int n = 1;
                foreach(SegmentSalesCallSummary s in response.Sheet2.Items)
                {
                    s.No = n++;
                }
                */
            }

            return response;

        }

        private async Task<List<ProposalDetail>> GetListProposalDetails(int id, int fromMonth, int toMonth, int year)
        {
            List<ProposalDetail> response = new List<ProposalDetail>();

            CrmDealRole role = GetDealRole("rm");

            var query = from proposal in _context.CrmDealProposals
                        join deal in _context.CrmDeals
                        on proposal.DealId equals deal.Id
                        join dealTribe in _context.CrmDealTribes
                        on deal.Id equals dealTribe.DealId
                        join segment in _context.CrmSegments
                        on deal.SegmentId equals segment.Id
                        join client in _context.CrmClients
                        on deal.ClientId equals client.Id
                        join period in _context.CrmPeriods
                        on proposal.PeriodId equals period.Id
                        where !deal.IsDeleted && !proposal.IsDeleted && period.Month >= fromMonth && period.Month <= toMonth && period.Year == year && dealTribe.TribeId == id
                        select new
                        {
                            proposal.Id,
                            DealId = deal.Id,
                            Company = client.Company,
                            Segment = segment.Segment,
                            DealName = deal.Name,
                            Amount = proposal.ProposalValue
                        };

            var objs = await query.ToListAsync();

            List<int> propIds = new List<int>();

            int n = 1;
            foreach (var obj in objs)
            {
                if (propIds.Contains(obj.Id)) continue;
                propIds.Add(obj.Id);

                ProposalDetail detail = new ProposalDetail()
                {
                    No = n++,
                    Company = obj.Company,
                    Tribe = "",
                    Segment = obj.Segment,
                    RM = "",
                    DealName = obj.DealName,
                    Amount = obj.Amount
                };
                var q1 = from dealTribe in _context.CrmDealTribes
                         join tribe in _context.CoreTribes
                         on dealTribe.TribeId equals tribe.Id
                         where !dealTribe.IsDeleted && !tribe.IsDeleted && dealTribe.DealId == obj.DealId
                         select new GenericInfo()
                         {
                             Id = tribe.Id,
                             Text = tribe.Shortname.ToUpper()
                         };

                List<GenericInfo> tribes = await q1.ToListAsync();
                foreach (GenericInfo tribe in tribes)
                {
                    detail.Tribe += detail.Tribe.Equals("") ? tribe.Text : ", " + tribe.Text;
                }

                var q2 = from member in _context.CrmDealInternalMembers
                         join user in _context.Users
                         on member.UserId equals user.ID
                         where !member.IsDeleted && member.DealId == obj.DealId
                         select new GenericInfo()
                         {
                             Id = user.ID,
                             Text = user.FirstName
                         };
                List<GenericInfo> users = await q2.ToListAsync();
                foreach (GenericInfo user in users)
                {
                    detail.RM += detail.RM.Equals("") ? user.Text : ", " + user.Text;
                }

                response.Add(detail);
            }
            return response;
        }

        /**
         * @api {get} /Pipeline/team/{role}/{userId}/{fromDate}/{toDate}/{tribeFilter} GET individual summary by date
         * @apiVersion 1.0.0
         * @apiName GetTeamReportByDate
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * @apiParam {String} role      "mentor" atau "leader" atau "admin"
         * @apiParam {String} tribeFilter  Comma-delimited string dari tribeIds, atau 0 kalau tidak menggunakan filter.
         * 
         * @apiSuccessExample Success-Response:
         * [
         *     {
         *         "title": "Total No Proposals in 2020",
         *         "amount": 21,
         *         "percent": 0,
         *         "note": "Compare to Jan-Jul 2019"
         *     },
         *     {
         *         "title": "Total Sales Visits in 2020",
         *         "amount": 7,
         *         "percent": 0,
         *         "note": "Compare to Jan-Jul 2019"
         *     },
         *     {
         *         "title": "Total invoice in Jul 2020",
         *         "amount": 86809416,
         *         "percent": 0,
         *         "note": "Compare to Jul 2019"
         *     },
         *     {
         *         "title": "Total invoice in 2020",
         *         "amount": 336700232,
         *         "percent": 0,
         *         "note": "Compare to Jan-Jul 2019"
         *     }
         * ]
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("team/{role}/{userId}/{fromDate}/{toDate}/{tribeFilter}")]
        public async Task<ActionResult<TeamAchievementByDate>> GetTeamReportByDate(string role, int userId, string fromDate, string toDate, string tribeFilter)
        {
            DateTime fr = DateTime.ParseExact(fromDate,
                              "yyyyMMdd",
                               CultureInfo.InvariantCulture);
            DateTime to = DateTime.ParseExact(toDate,
                              "yyyyMMdd",
                              CultureInfo.InvariantCulture).AddDays(1);      // Add to next day mid night;


            List<int> tribeIds = new List<int>();
            if (!tribeFilter.Trim().Equals("0"))
            {
                foreach (string s in tribeFilter.Trim().Split(","))
                {
                    try
                    {
                        int n = Convert.ToInt32(s);
                        tribeIds.Add(n);
                    }
                    catch
                    {
                        return BadRequest(new { error = "Error in converting tribe filters to integer." });
                    }
                }
            }

            string nm = GetTeamName(role, userId);

            TeamAchievementByDate achievement = new TeamAchievementByDate();
            achievement.UserId = userId;
            achievement.TeamName = string.IsNullOrEmpty(nm) ? "" : nm;
            achievement.Items = new List<TeamAchievementItemByDate>();

            if (string.IsNullOrEmpty(nm)) return achievement;


            List<GenericURL> members = await GetTeamMemberUserIds(role, userId);

            List<string> roles = new List<string>();
            roles.Add("rm");

            foreach (GenericURL member in members)
            {
                GenericInfo status = DoGetTargetStatus(fr.Year, member.Id);

                TeamAchievementItemByDate item = new TeamAchievementItemByDate();
                item.User = new GenericInfo();
                item.User.Id = member.Id;
                item.User.Text = member.Text;
                item.Authority = member.URL;

                // Update 2025-03-14
                // IndividualExportProposalItemResponse r = await _crmReportService.GetIndividualExportProposalItems(roles, item.User.Id, fromMonth, toMonth, year, 0, 0, "*");
                IndividualExportProposalItemResponse r = await _crmReportService.GetIndividualExportProposalItemsByDate(roles, item.User.Id, fr, to, 0, 0, "*");
                // End Update

                int nproposal = 0;
                long proposalValue = 0;
                foreach (IndividualExportProposalItem i in r.Items)
                {
                    if (tribeIds.Count == 0)
                    {
                        nproposal++;
                        proposalValue += i.ProposalValue;
                    }
                    else
                    {
                        var q = from proposal in _context.CrmDealProposals
                                join deal in _context.CrmDeals on proposal.DealId equals deal.Id
                                join dealTribe in _context.CrmDealTribes on deal.Id equals dealTribe.DealId
                                where proposal.Id == i.ProposalId && !deal.IsDeleted && !dealTribe.IsDeleted && tribeIds.Contains(dealTribe.TribeId)
                                select proposal.Id;
                        if (q.Count() > 0)
                        {
                            nproposal++;
                            proposalValue += i.ProposalValue;
                        }
                    }
                }

                // Update 2025-03-14
                // IndividualExportVisit visits = await _crmReportService.GetListActualVisitsByUserId(member.Id, fromMonth, toMonth, year, 0, 0, "*");
                IndividualExportVisit visits = await _crmReportService.GetListActualVisitsByUserIdByDate(member.Id, fr, to, 0, 0, "*");
                // End update

                int visitTotal = 0;
                if (tribeIds.Count() == 0)
                {
                    visitTotal = visits.Items.Count();
                }
                else
                {
                    foreach (IndividualExportVisitItem visit in visits.Items)
                    {
                        var q = from vt in _context.CrmDealVisitTribes
                                where vt.VisitId == visit.VisitId && tribeIds.Contains(vt.TribeId)
                                select vt.Id;
                        if (q.Count() > 0) visitTotal++;
                    }
                }

                item.NProposals = nproposal; // GetIndividualSummaryNProposal(member.Id, fromMonth, toMonth, year, tribeIds);
                item.Visits = visitTotal; // GetIndividualSummaryVisit(member.Id, fromMonth, toMonth, year, tribeIds);
                item.ProposalValue = proposalValue; // await GetIndividualSummaryProposalValue(member.Id, fromMonth, toMonth, year, tribeIds);

                var q1 = from rm in _context.CrmRelManagers
                         join segment in _context.CrmSegments on rm.SegmentId equals segment.Id
                         where rm.UserId == item.User.Id && !rm.IsDeleted && rm.isActive
                         select new GenericInfo()
                         {
                             Id = segment.Id,
                             Text = segment.Segment
                         };
                item.Segment = q1.FirstOrDefault();

                // Update 2025-03-14
                // item.Sales = await _crmReportService.GetActualSalesByUserIdFilterTribe(member.Id, fromMonth, toMonth, year, tribeIds); // GetIndividualSummarySalesOnePeriod(member.Id, fromMonth, toMonth, year);
                item.Sales = await _crmReportService.GetActualSalesByUserIdFilterTribeByDate(member.Id, fr, to, tribeIds); // GetIndividualSummarySalesOnePeriod(member.Id, fromMonth, toMonth, year);
                item.Status = status.Text;
                item.FromDate = fr;
                item.ToDate = to.AddDays(-1);
                achievement.Items.Add(item);
            }

            return achievement;
        }



        /**
         * @api {get} /Pipeline/team/{role}/{userId}/{fromMonth}/{toMonth}/{year}/{tribeFilter} GET individual summary
         * @apiVersion 1.0.0
         * @apiName GetIndividualReportSummary
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * @apiParam {String} role      "mentor" atau "leader"
         * @apiParam {String} tribeFilter  Comma-delimited string dari tribeIds, atau 0 kalau tidak menggunakan filter.
         * 
         * @apiSuccessExample Success-Response:
         * [
         *     {
         *         "title": "Total No Proposals in 2020",
         *         "amount": 21,
         *         "percent": 0,
         *         "note": "Compare to Jan-Jul 2019"
         *     },
         *     {
         *         "title": "Total Sales Visits in 2020",
         *         "amount": 7,
         *         "percent": 0,
         *         "note": "Compare to Jan-Jul 2019"
         *     },
         *     {
         *         "title": "Total invoice in Jul 2020",
         *         "amount": 86809416,
         *         "percent": 0,
         *         "note": "Compare to Jul 2019"
         *     },
         *     {
         *         "title": "Total invoice in 2020",
         *         "amount": 336700232,
         *         "percent": 0,
         *         "note": "Compare to Jan-Jul 2019"
         *     }
         * ]
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("team/{role}/{userId}/{fromMonth}/{toMonth}/{year}/{tribeFilter}")]
        public async Task<ActionResult<TeamAchievement>> GetTeamReport(string role, int userId, int fromMonth, int toMonth, int year, string tribeFilter)
        {
            List<int> tribeIds = new List<int>();
            if (!tribeFilter.Trim().Equals("0"))
            {
                foreach (string s in tribeFilter.Trim().Split(","))
                {
                    try
                    {
                        int n = Convert.ToInt32(s);
                        tribeIds.Add(n);
                    }
                    catch
                    {
                        return BadRequest(new { error = "Error in converting tribe filters to integer." });
                    }
                }
            }

            string nm = GetTeamName(role, userId);

            TeamAchievement achievement = new TeamAchievement();
            achievement.UserId = userId;
            achievement.TeamName = string.IsNullOrEmpty(nm) ? "" : nm;
            achievement.Items = new List<TeamAchievementItem>();

            if (string.IsNullOrEmpty(nm)) return achievement;


            List<GenericURL> members = await GetTeamMemberUserIds(role, userId);

            List<string> roles = new List<string>();
            roles.Add("rm");

            foreach (GenericURL member in members)
            {
                GenericInfo status = DoGetTargetStatus(year, member.Id);

                TeamAchievementItem item = new TeamAchievementItem();
                item.User = new GenericInfo();
                item.User.Id = member.Id;
                item.User.Text = member.Text;
                item.Authority = member.URL;

                IndividualExportProposalItemResponse r = await _crmReportService.GetIndividualExportProposalItems(roles, item.User.Id, fromMonth, toMonth, year, 0, 0, "*");
                int nproposal = 0;
                long proposalValue = 0;
                foreach (IndividualExportProposalItem i in r.Items)
                {
                    if (tribeIds.Count == 0)
                    {
                        nproposal++;
                        proposalValue += i.ProposalValue;
                    }
                    else
                    {
                        var q = from proposal in _context.CrmDealProposals
                                join deal in _context.CrmDeals on proposal.DealId equals deal.Id
                                join dealTribe in _context.CrmDealTribes on deal.Id equals dealTribe.DealId
                                where proposal.Id == i.ProposalId && !deal.IsDeleted && !dealTribe.IsDeleted && tribeIds.Contains(dealTribe.TribeId)
                                select proposal.Id;
                        if (q.Count() > 0)
                        {
                            nproposal++;
                            proposalValue += i.ProposalValue;
                        }
                    }
                }

                IndividualExportVisit visits = await _crmReportService.GetListActualVisitsByUserId(member.Id, fromMonth, toMonth, year, 0, 0, "*");
                int visitTotal = 0;
                if (tribeIds.Count() == 0)
                {
                    visitTotal = visits.Items.Count();
                }
                else
                {
                    foreach (IndividualExportVisitItem visit in visits.Items)
                    {
                        var q = from vt in _context.CrmDealVisitTribes
                                where vt.VisitId == visit.VisitId && tribeIds.Contains(vt.TribeId)
                                select vt.Id;
                        if (q.Count() > 0) visitTotal++;
                    }
                }

                item.NProposals = nproposal; // GetIndividualSummaryNProposal(member.Id, fromMonth, toMonth, year, tribeIds);
                item.Visits = visitTotal; // GetIndividualSummaryVisit(member.Id, fromMonth, toMonth, year, tribeIds);
                item.ProposalValue = proposalValue; // await GetIndividualSummaryProposalValue(member.Id, fromMonth, toMonth, year, tribeIds);

                var q1 = from rm in _context.CrmRelManagers
                         join segment in _context.CrmSegments on rm.SegmentId equals segment.Id
                         where rm.UserId == item.User.Id && !rm.IsDeleted && rm.isActive
                         select new GenericInfo()
                         {
                             Id = segment.Id,
                             Text = segment.Segment
                         };
                item.Segment = q1.FirstOrDefault();


                item.Sales = await _crmReportService.GetActualSalesByUserIdFilterTribe(member.Id, fromMonth, toMonth, year, tribeIds); // GetIndividualSummarySalesOnePeriod(member.Id, fromMonth, toMonth, year);
                item.Status = status.Text;
                item.FromMonth = fromMonth;
                item.ToMonth = toMonth;
                item.Year = year;
                achievement.Items.Add(item);
            }

            return achievement;
        }

        private GenericInfo DoGetTargetStatus(int year, int id)
        {

            string str = "";

            var query = from t in _context.CrmDealTargets
                        join p in _context.CrmPeriods on t.PeriodId equals p.Id
                        where t.LinkedId == id && t.Type.Equals("rm") && p.Year == year
                        select new
                        {
                            Id = id,
                            Status = t.Status
                        };

            var obj = query.FirstOrDefault();
            if (obj == null) str = "Targets not set.";
            else if (obj.Status.ToLower().StartsWith("need")) str = "Need approval";
            else if (obj.Status.ToLower().StartsWith("reject")) str = "Targets rejected";
            else if (obj.Status.ToLower().StartsWith("approve")) str = "Targets approved";

            return new GenericInfo()
            {
                Id = id,
                Text = str
            };
        }

        private string GetTeamName(string role, int userId)
        {
            if (role.Trim().Equals("leader"))
            {
                CrmRelManager rm = _context.CrmRelManagers.Where(a => a.UserId == userId && a.isActive && !a.IsDeleted && a.IsTeamLeader).FirstOrDefault();
                if (rm == null) return "";
                return rm.TeamName;
            }
            else if (role.Trim().Equals("mentor"))
            {
                CrmRelManager leader = _context.CrmRelManagers.Where(a => a.LeaderId == userId && a.isActive && !a.IsDeleted && a.IsTeamLeader).FirstOrDefault();
                if (leader == null) return "";
                return leader.TeamName;
            }
            else if (role.Trim().Equals("admin"))
            {
                return "All Team Members";
            }
            return "";

        }
        private async Task<List<GenericURL>> GetTeamMemberUserIds(string role, int userId)
        {
            List<GenericURL> list = new List<GenericURL>();

            if (role.Trim().Equals("admin"))
            {
                var queryAdmin = from r in _context.CrmRelManagers
                                 join u in _context.Users on r.UserId equals u.ID
                                 where r.isActive && !r.IsDeleted && r.IsTeamLeader
                                 select new GenericURL()
                                 {
                                     Id = u.ID,
                                     Text = u.FirstName,
                                     URL = "Leader",
                                     Time = DateTime.Now
                                 };
                list.AddRange(await queryAdmin.ToListAsync());

                var queryAdmin2 = from r in _context.CrmRelManagers
                                  join u in _context.Users on r.UserId equals u.ID
                                  where r.isActive && !r.IsDeleted && !r.IsTeamLeader
                                  select new GenericURL()
                                  {
                                      Id = u.ID,
                                      Text = u.FirstName,
                                      URL = "Member",
                                      Time = DateTime.Now
                                  };
                list.AddRange(await queryAdmin2.ToListAsync());

            }
            else if (role.Trim().Equals("leader"))
            {
                var queryLeader = from r in _context.CrmRelManagers
                                  join u in _context.Users on r.UserId equals u.ID
                                  where r.UserId == userId && r.isActive && !r.IsDeleted && r.IsTeamLeader
                                  select new GenericURL()
                                  {
                                      Id = u.ID,
                                      Text = u.FirstName,
                                      URL = "Leader",
                                      Time = DateTime.Now
                                  };
                GenericURL leader = queryLeader.FirstOrDefault();
                if (leader == null) return list;
                list.Add(leader);

                var queryMembers = from r in _context.CrmRelManagers
                                   join u in _context.Users on r.UserId equals u.ID
                                   where r.LeaderId == leader.Id && r.isActive && !r.IsDeleted
                                   select new GenericURL()
                                   {
                                       Id = u.ID,
                                       Text = u.FirstName,
                                       URL = "Member",
                                       Time = DateTime.Now
                                   };
                list.AddRange(await queryMembers.ToListAsync());
            }
            else if (role.Trim().Equals("mentor"))
            {
                var queryLeader = from r in _context.CrmRelManagers
                                  join u in _context.Users on r.UserId equals u.ID
                                  where r.LeaderId == userId && r.isActive && !r.IsDeleted && r.IsTeamLeader
                                  select new GenericURL()
                                  {
                                      Id = u.ID,
                                      Text = u.FirstName,
                                      URL = "Leader",
                                      Time = DateTime.Now
                                  };
                List<GenericURL> leaders = queryLeader.ToList();
                if (leaders == null) return list;
                foreach (GenericURL leader in leaders)
                {
                    list.Add(leader);

                    var queryMembers = from r in _context.CrmRelManagers
                                       join u in _context.Users on r.UserId equals u.ID
                                       where r.LeaderId == leader.Id && r.isActive && !r.IsDeleted
                                       select new GenericURL()
                                       {
                                           Id = u.ID,
                                           Text = u.FirstName,
                                           URL = "Member",
                                           Time = DateTime.Now
                                       };
                    list.AddRange(await queryMembers.ToListAsync());

                }
            }
            return list;
        }
        /**
         * @api {get} /Pipeline/ind/summary/{userId}/{month}/{year} GET individual summary
         * @apiVersion 1.0.0
         * @apiName GetIndividualReportSummary
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiSuccessExample Success-Response:
         * [
         *     {
         *         "title": "Total No Proposals in 2020",
         *         "amount": 21,
         *         "percent": 0,
         *         "note": "Compare to Jan-Jul 2019"
         *     },
         *     {
         *         "title": "Total Sales Visits in 2020",
         *         "amount": 7,
         *         "percent": 0,
         *         "note": "Compare to Jan-Jul 2019"
         *     },
         *     {
         *         "title": "Total invoice in Jul 2020",
         *         "amount": 86809416,
         *         "percent": 0,
         *         "note": "Compare to Jul 2019"
         *     },
         *     {
         *         "title": "Total invoice in 2020",
         *         "amount": 336700232,
         *         "percent": 0,
         *         "note": "Compare to Jan-Jul 2019"
         *     }
         * ]
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("ind/summary/{userId}/{month}/{year}")]
        public async Task<List<SummaryHistory>> GetIndividualReportSummary(int userId, int month, int year)
        {
            List<SummaryHistory> response = new List<SummaryHistory>();

            IndividualReportVisit visitCurYear = await DoGetIndividualReportVisit(userId, 1, 12, year, 1, 1000, "*");
            IndividualReportVisit visitLastYear = await DoGetIndividualReportVisit(userId, 1, 12, year - 1, 1, 1000, "*");

            DoubleLong d1 = await GetIndividualSummaryNProposal(userId, year);
            // DoubleLong d2 = GetIndividualSummaryVisit(userId, year);
            DoubleLong d3 = GetIndividualSummarySales(userId, month, month, year);
            DoubleLong d4 = GetIndividualSummarySales(userId, 1, month, year);

            SummaryHistory h1 = new SummaryHistory();
            h1.Title = string.Join(" ", new[] { "Total No Proposals in", year.ToString() });
            h1.Amount = d1.Amount1;
            h1.Percent = d1.Amount2 == 0 ? 0 : Convert.ToInt32(Math.Round(Convert.ToSingle(Convert.ToSingle(d1.Amount1) / Convert.ToSingle(d1.Amount2)) * 100));
            h1.Note = string.Join("-", new[] { "Compare to Jan", String.Format("{0:MMM yyyy}", new DateTime(year - 1, month, 1)) });

            SummaryHistory h2 = new SummaryHistory();
            h2.Title = string.Join(" ", new[] { "Total Sales Visits in", year.ToString() });
            h2.Amount = visitCurYear.Items.Count(); // d2.Amount1;
            h2.Percent = (visitCurYear.Items == null || visitLastYear.Items.Count() == 0) ? 0 : Convert.ToInt32(Math.Round(Convert.ToSingle(Convert.ToSingle(visitCurYear.Items.Count()) / Convert.ToSingle(visitLastYear.Items.Count())) * 100)); // .Amount2 == 0 ? 0 : Convert.ToInt32(Math.Round(Convert.ToSingle(Convert.ToSingle(d2.Amount1) / Convert.ToSingle(d2.Amount2)) * 100));
            h2.Note = string.Join("-", new[] { "Compare to Jan", String.Format("{0:MMM yyyy}", new DateTime(year - 1, month, 1)) });

            SummaryHistory h3 = new SummaryHistory();
            h3.Title = string.Join(" ", new[] { "Total invoice in", String.Format("{0:MMM yyyy}", new DateTime(year, month, 1)) });
            h3.Amount = d3.Amount1;
            h3.Percent = d3.Amount2 == 0 ? 0 : Convert.ToInt32(Math.Round(Convert.ToSingle(Convert.ToSingle(d3.Amount1) / Convert.ToSingle(d3.Amount2)) * 100));
            h3.Note = string.Join(" ", new[] { "Compare to", String.Format("{0:MMM yyyy}", new DateTime(year - 1, month, 1)) });

            SummaryHistory h4 = new SummaryHistory();
            h4.Title = string.Join(" ", new[] { "Total invoice in", year.ToString() });
            h4.Amount = d4.Amount1;
            h4.Percent = d4.Amount2 == 0 ? 0 : Convert.ToInt32(Math.Round(Convert.ToSingle(Convert.ToSingle(d4.Amount1) / Convert.ToSingle(d4.Amount2)) * 100));
            h4.Note = string.Join("-", new[] { "Compare to Jan", String.Format("{0:MMM yyyy}", new DateTime(year - 1, month, 1)) });

            response.AddRange(new[] { h1, h2, h3, h4 });

            return response;
        }


        /**
         * @api {get} /pipeline/ind/visit/{userId}/{year}/{page}/{perPage}/{search} GET individual visit
         * @apiVersion 1.0.0
         * @apiName GetIndividualReportVisit
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} userId                id dari user yang datanya mau diambil
         * @apiParam {Number} year                  Tahun yang datanya ingin diambil, misal 2020
         * @apiParam {Number} page                  Halaman yang ditampilkan.
         * @apiParam {Number} perPage               Jumlah data per halaman.
         * @apiParam {String} search                * untuk tidak menggunakan search, atau kata yang dicari.
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "items": [
         *           {
         *               "visitId": 11,
         *               "dealId": 440,
         *               "companyId": 1032,
         *               "company": "BADAN KEPENDUDUKAN DAN KELUARGA BERENCANA NASIONAL (BKKBN)",
         *               "visitDate": "2020-06-03T15:30:00",
         *               "location": "Jakarta",
         *               "objective": "Penyusunan BSC ",
         *               "nextStep": "Follow up project",
         *               "remarks": ""
         *               "contacts": [
         *                  {
         *                      "id": 38310,
         *                      "text": "Peki"
         *                  }
         *               ]
         *               "rms": [
         *                  {
         *                      "id": 18,
         *                      "text": "Santi Susanti",
         *                      "percent": 100.0
         *                  }
         *               ],
         *               "cons": [
         *                  {
         *                      "id": 1,
         *                      "text": "Rifky",
         *                      "percent": 0
         *                  }
         *               ],
         *           },
         *           {
         *               "visitId": 12,
         *               "dealId": 441,
         *               "companyId": 1032,
         *               "company": "BADAN KEPENDUDUKAN DAN KELUARGA BERENCANA NASIONAL (BKKBN)",
         *               "visitDate": "2020-06-03T15:30:00",
         *               "location": "Jakarta",
         *               "objective": "In-house training",
         *               "nextStep": "Follow up ",
         *               "remarks": ""
         *               "contacts": [
         *                  {
         *                      "id": 38310,
         *                      "text": "Peki"
         *                  }
         *               ]
         *               "rms": [
         *                  {
         *                      "id": 18,
         *                      "text": "Santi Susanti",
         *                      "percent": 100.0
         *                  }
         *               ],
         *               "cons": [
         *                  {
         *                      "id": 1,
         *                      "text": "Rifky",
         *                      "percent": 0
         *                  }
         *               ],
         *           }
         *       ],
         *       "info": {
         *           "page": 1,
         *           "perPage": 2,
         *           "total": 7
         *       }
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("ind/visit/{userId}/{year}/{page}/{perPage}/{search}")]
        public async Task<ActionResult<IndividualReportVisit>> GetIndividualReportVisit(int userId, int year, int page, int perPage, string search)
        {
            return await DoGetIndividualReportVisit(userId, 1, 12, year, page, perPage, search);
        }

        /**
         * @api {get} /pipeline/ind/visit/{userId}/{fromMonth}/{toMonth}/{year}/{page}/{perPage}/{search} GET individual visit filter
         * @apiVersion 1.0.0
         * @apiName GetIndividualReportVisitFilter
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} userId                id dari user yang datanya mau diambil
         * @apiParam {Number} fromMonth             Mulaid ari bulan apa, misalnya 1 untuk mulai dari bulan Januari
         * @apiParam {Number} toMonth               Sampai bulan apa, misalnya 5 untuk sampai bulan Mei
         * @apiParam {Number} year                  Tahun yang datanya ingin diambil, misal 2020
         * @apiParam {Number} page                  Halaman yang ditampilkan.
         * @apiParam {Number} perPage               Jumlah data per halaman.
         * @apiParam {String} search                * untuk tidak menggunakan search, atau kata yang dicari.
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "items": [
         *           {
         *               "visitId": 11,
         *               "dealId": 440,
         *               "companyId": 1032,
         *               "company": "BADAN KEPENDUDUKAN DAN KELUARGA BERENCANA NASIONAL (BKKBN)",
         *               "visitDate": "2020-06-03T15:30:00",
         *               "location": "Jakarta",
         *               "objective": "Penyusunan BSC ",
         *               "nextStep": "Follow up project",
         *               "remarks": ""
         *               "contacts": [
         *                  {
         *                      "id": 38310,
         *                      "text": "Peki"
         *                  }
         *               ]
         *               "rms": [
         *                  {
         *                      "id": 18,
         *                      "text": "Santi Susanti",
         *                      "percent": 100.0
         *                  }
         *               ],
         *               "cons": [
         *                  {
         *                      "id": 1,
         *                      "text": "Rifky",
         *                      "percent": 0
         *                  }
         *               ],
         *           },
         *           {
         *               "visitId": 12,
         *               "dealId": 441,
         *               "companyId": 1032,
         *               "company": "BADAN KEPENDUDUKAN DAN KELUARGA BERENCANA NASIONAL (BKKBN)",
         *               "visitDate": "2020-06-03T15:30:00",
         *               "location": "Jakarta",
         *               "objective": "In-house training",
         *               "nextStep": "Follow up ",
         *               "remarks": ""
         *               "contacts": [
         *                  {
         *                      "id": 38310,
         *                      "text": "Peki"
         *                  }
         *               ]
         *               "rms": [
         *                  {
         *                      "id": 18,
         *                      "text": "Santi Susanti",
         *                      "percent": 100.0
         *                  }
         *               ],
         *               "cons": [
         *                  {
         *                      "id": 1,
         *                      "text": "Rifky",
         *                      "percent": 0
         *                  }
         *               ],
         *           }
         *       ],
         *       "info": {
         *           "page": 1,
         *           "perPage": 2,
         *           "total": 7
         *       }
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("ind/visit/{userId}/{fromMonth}/{toMonth}/{year}/{page}/{perPage}/{search}")]
        public async Task<ActionResult<IndividualReportVisit>> GetIndividualReportVisitFilter(int userId, int fromMonth, int toMonth, int year, int page, int perPage, string search)
        {
            return await DoGetIndividualReportVisit(userId, fromMonth, toMonth, year, page, perPage, search);
        }

        private async Task<IndividualReportVisit> DoGetIndividualReportVisit(int userId, int fromMonth, int toMonth, int year, int page, int perPage, string search)
        {
            IndividualExportVisit visit1 = await _crmReportService.GetListActualVisitsByUserId(userId, fromMonth, toMonth, year, page, perPage, search);
            // IndividualExportVisit visit1 = await _crmReportService.GetListActualVisitsByUserId(userId, 1, DateTime.Now.Month, year, page, perPage, search);

            int total = visit1.Info.total;

            IndividualReportVisit response = new IndividualReportVisit();
            response.Info = new PaginationInfo(page, perPage, total);
            response.Items = new List<IndividualReportVisitItem>();
            foreach (IndividualExportVisitItem item in visit1.Items)
            {
                IndividualReportVisitItem vi = new IndividualReportVisitItem()
                {
                    VisitId = item.VisitId,
                    DealId = item.DealId,
                    CompanyId = item.ClientId,
                    Company = item.Company,
                    VisitDate = item.VisitDate,
                    Location = item.Location,
                    Objective = item.Objective,
                    NextStep = item.NextStep,
                    Remarks = item.Remarks
                };
                response.Items.Add(vi);
            }

            CrmDealRole rm = GetDealRole("rm");
            CrmDealRole con = GetDealRole("con");

            foreach (IndividualReportVisitItem item in response.Items)
            {
                if (item.DealId != 0)
                {
                    item.Rms = await GetInternalMembers(item.DealId, rm.Id);
                    item.Cons = await GetInternalMembers(item.DealId, con.Id);
                }
                else
                {
                    item.Rms = new List<PercentTribeResponse>();
                    item.Cons = new List<PercentTribeResponse>();
                }

                var q = from visitContact in _context.CrmDealVisitContacts
                        join contact in _context.CrmContacts
                        on visitContact.ContactId equals contact.Id
                        where visitContact.VisitId == item.VisitId
                        select new GenericInfo()
                        {
                            Id = contact.Id,
                            Text = contact.Name
                        };

                item.Contacts = await q.ToListAsync();
            }

            return response;
        }

        /**
         * @api {get} /pipeline/export/visit/{userId}/{year}/{search} GET export visit
         * @apiVersion 1.0.0
         * @apiName GetIndividualExportVisit
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} userId                id dari user yang datanya mau diambil
         * @apiParam {Number} year                  Tahun yang datanya ingin diambil, misal 2020
         * @apiParam {String} search                * untuk tidak menggunakan search, atau kata yang dicari.
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "headers": [
         *           "No",
         *           "Company",
         *           "Visit date",
         *           "Location",
         *           "Visit objective",
         *           "Next step",
         *           "Remarks"
         *       ],
         *       "items": [
         *           {
         *               "no": 1,
         *               "company": "BADAN KEPENDUDUKAN DAN KELUARGA BERENCANA NASIONAL (BKKBN)",
         *               "visitDate": "2020-06-03T15:30:00",
         *               "location": "Jakarta",
         *               "objective": "Penyusunan BSC ",
         *               "nextStep": "Follow up project",
         *               "remarks": ""
         *           },
         *           {
         *               "no": 2,
         *               "company": "BADAN KEPENDUDUKAN DAN KELUARGA BERENCANA NASIONAL (BKKBN)",
         *               "visitDate": "2020-06-03T15:30:00",
         *               "location": "Jakarta",
         *               "objective": "In-house training",
         *               "nextStep": "Follow up ",
         *               "remarks": ""
         *           }
         *       ]
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("export/visit/{userId}/{year}/{search}")]
        public async Task<ActionResult<IndividualExportVisit>> GetIndividualExportVisit(int userId, int year, string search)
        {
            return await _crmReportService.GetListActualVisitsByUserId(userId, 1, 12, year, 0, 0, search);
            //return await _crmReportService.GetListActualVisitsByUserId(userId, 1, DateTime.Now.Month, year, 0, 0, search);
        }


        /**
         * @api {get} /pipeline/export/visit/{userId}/{fromMonth}/{toMonth}/{year}/{search} GET export visit filter
         * @apiVersion 1.0.0
         * @apiName GetIndividualExportVisitFilter
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} userId                id dari user yang datanya mau diambil
         * @apiParam {Number} fromMonth             Mulaid ari bulan apa, misalnya 1 untuk mulai dari bulan Januari
         * @apiParam {Number} toMonth               Sampai bulan apa, misalnya 5 untuk sampai bulan Mei
         * @apiParam {Number} year                  Tahun yang datanya ingin diambil, misal 2020
         * @apiParam {String} search                * untuk tidak menggunakan search, atau kata yang dicari.
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "headers": [
         *           "No",
         *           "Company",
         *           "Visit date",
         *           "Location",
         *           "Visit objective",
         *           "Next step",
         *           "Remarks"
         *       ],
         *       "items": [
         *           {
         *               "no": 1,
         *               "company": "BADAN KEPENDUDUKAN DAN KELUARGA BERENCANA NASIONAL (BKKBN)",
         *               "visitDate": "2020-06-03T15:30:00",
         *               "location": "Jakarta",
         *               "objective": "Penyusunan BSC ",
         *               "nextStep": "Follow up project",
         *               "remarks": ""
         *           },
         *           {
         *               "no": 2,
         *               "company": "BADAN KEPENDUDUKAN DAN KELUARGA BERENCANA NASIONAL (BKKBN)",
         *               "visitDate": "2020-06-03T15:30:00",
         *               "location": "Jakarta",
         *               "objective": "In-house training",
         *               "nextStep": "Follow up ",
         *               "remarks": ""
         *           }
         *       ]
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("export/visit/{userId}/{fromMonth}/{toMonth}/{year}/{search}")]
        public async Task<ActionResult<IndividualExportVisit>> GetIndividualExportVisitFilter(int userId, int fromMonth, int toMonth, int year, string search)
        {
            return await _crmReportService.GetListActualVisitsByUserId(userId, fromMonth, toMonth, year, 0, 0, search);
            //return await _crmReportService.GetListActualVisitsByUserId(userId, 1, DateTime.Now.Month, year, 0, 0, search);
        }




        /*
        private async Task<IndividualExportVisit> GetListIndividualVisits(int userId, int year, string search)
        {
            IQueryable<IndividualExportVisitItem> query;
            if (search.Equals("*"))
            {
                query = from visitUser in _context.CrmDealVisitUsers
                        join visit in _context.CrmDealVisits
                        on visitUser.VisitId equals visit.Id
                        join client in _context.CrmClients
                        on visit.ClientId equals client.Id
                        join period in _context.CrmPeriods
                        on visit.PeriodId equals period.Id
                        where visitUser.Userid == userId && !visit.IsDeleted && period.Year == year
                        select new IndividualExportVisitItem()
                        {
                            No = 0,
                            Company = client.Company,
                            VisitDate = visit.VisitFromTime,
                            Location = visit.Location,
                            Objective = visit.Objective,
                            NextStep = visit.NextStep,
                            Remarks = visit.Remark
                        };

            }
            else
            {
                query = from visitUser in _context.CrmDealVisitUsers
                        join visit in _context.CrmDealVisits
                        on visitUser.VisitId equals visit.Id
                        join client in _context.CrmClients
                        on visit.ClientId equals client.Id
                        join period in _context.CrmPeriods
                        on visit.PeriodId equals period.Id
                        where visitUser.Userid == userId && !visit.IsDeleted && period.Year == year && client.Company.Contains(search)
                        select new IndividualExportVisitItem()
                        {
                            No = 0,
                            Company = client.Company,
                            VisitDate = visit.VisitFromTime,
                            Location = visit.Location,
                            Objective = visit.Objective,
                            NextStep = visit.NextStep,
                            Remarks = visit.Remark
                        };
            }

            List<IndividualExportVisitItem> items = await query.ToListAsync();
            int n = 1;
            foreach (IndividualExportVisitItem item in items)
            {
                item.No = n++;
            }

            IndividualExportVisit response = new IndividualExportVisit();
            response.Headers = new List<string>(new string[] { "No", "Company", "Visit date", "Location", "Visit objective", "Next step", "Remarks" });
            response.Items = items;

            return response;
        }
*/

        /**
         * @api {get} /pipeline/ind/proposal/{userId}/{year}/{page}/{perPage}/{search} GET individual proposal
         * @apiVersion 1.0.0
         * @apiName GetIndividualReportProposal
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} userId                id dari user yang datanya mau diambil
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
        [Authorize(Policy = "ApiUser")]
        [HttpGet("ind/proposal/{userId}/{year}/{page}/{perPage}/{search}")]
        public async Task<ActionResult<IndividualReportProposal>> GetIndividualReportProposal(int userId, int year, int page, int perPage, string search)
        {
            // Tambahkan yang PIC, tetapi usah
            List<String> roles = new List<string>();
            roles.Add("rm");
            // roles.Add("pic");
            return await DoGetIndividualReportProposal(roles, userId, 1, 12, year, page, perPage, search);
        }

        /**
         * @api {get} /pipeline/ind/proposal/{userId}/{fromMonth}/{toMonth}/{year}/{page}/{perPage}/{search} GET individual proposal filter
         * @apiVersion 1.0.0
         * @apiName GetIndividualReportProposalFilter
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
        [Authorize(Policy = "ApiUser")]
        [HttpGet("ind/proposal/{userId}/{fromMonth}/{toMonth}/{year}/{page}/{perPage}/{search}")]
        public async Task<ActionResult<IndividualReportProposal>> GetIndividualReportProposalFilter(int userId, int fromMonth, int toMonth, int year, int page, int perPage, string search)
        {
            //IndividualExportProposal ep = await _crmReportService.GetListProposalByUserId("rm", userId, 1, 12, year, page, perPage, search);

            // Tambahkan yang PIC, tetapi usah
            List<String> roles = new List<string>();
            roles.Add("rm");
            // roles.Add("pic");
            return await DoGetIndividualReportProposal(roles, userId, fromMonth, toMonth, year, page, perPage, search);
        }

        /**
         * @api {get} /pipeline/pic/proposal/{userId}/{fromMonth}/{toMonth}/{year}/{page}/{perPage}/{search} GET pic proposal filter
         * @apiVersion 1.0.0
         * @apiName GetPicReportProposalFilter
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
        [Authorize(Policy = "ApiUser")]
        [HttpGet("pic/proposal/{userId}/{fromMonth}/{toMonth}/{year}/{page}/{perPage}/{search}")]
        public async Task<ActionResult<IndividualReportPicProposal>> GetPicReportProposalFilter(int userId, int fromMonth, int toMonth, int year, int page, int perPage, string search)
        {
            //IndividualExportProposal ep = await _crmReportService.GetListProposalByUserId("rm", userId, 1, 12, year, page, perPage, search);
            //List<String> roles = new List<string>();
            //roles.Add("pic");
            //return await DoGetIndividualREportProposal(roles, userId, fromMonth, toMonth, year, page, perPage, search);

            // Sebagai PIC
            // Harus ambil dulu semua
            List<string> roles = new List<string>();
            roles.Add("pic");
            IndividualExportProposalItemResponse r = await _crmReportService.GetIndividualExportProposalItems(roles, userId, fromMonth, toMonth, year, 0, 0, search);

            // Sebagai Leader
            // Harus ambil dulu semua
            List<string> roleRM = new List<string>();
            roleRM.Add("rm");
            List<int> userIds = await _context.CrmRelManagers.Where(a => a.LeaderId == userId && !a.IsDeleted && a.isActive).Select(a => a.UserId).ToListAsync();
            foreach (int id in userIds)
            {
                IndividualExportProposalItemResponse t = await _crmReportService.GetIndividualExportProposalItems(roleRM, id, fromMonth, toMonth, year, 0, 0, search);
                foreach (IndividualExportProposalItem item in t.Items)
                {
                    if (r.Items.Where(a => a.ProposalId == item.ProposalId).Count() == 0)
                    {
                        r.Items.Add(item);
                        r.Total++;
                    }
                }
            }

            r.Items = r.Items.Skip(perPage * (page - 1)).Take(perPage).ToList();

            IndividualReportPicProposal response = new IndividualReportPicProposal();
            response.Info = new PaginationInfo(page, perPage, r.Total);
            response.Items = new List<IndividualReportPICProposalItem>();

            CrmDealRole role = GetDealRole("rm");

            foreach (IndividualExportProposalItem i in r.Items)
            {
                IndividualReportPICProposalItem pi = new IndividualReportPICProposalItem()
                {
                    ProposalId = i.ProposalId,
                    ProposalValue = i.ProposalValue,
                    Name = i.Name,
                    Type = i.Type,
                    SentById = i.SentById,
                    Filename = i.Filename,
                    SentBy = i.SentBy,
                    SentDate = i.SentDate,
                    Rms = new List<GenericInfo>(),
                    Segments = new List<GenericInfo>()
                };

                var query = from prop in _context.CrmDealProposals
                            join deal in _context.CrmDeals on prop.DealId equals deal.Id
                            join member in _context.CrmDealInternalMembers on deal.Id equals member.DealId
                            join user in _context.Users on member.UserId equals user.ID
                            join rm in _context.CrmRelManagers on user.ID equals rm.UserId
                            join segment in _context.CrmSegments on rm.SegmentId equals segment.Id
                            where prop.Id == i.ProposalId && !member.IsDeleted && !rm.IsDeleted && !segment.IsDeleted && member.RoleId == role.Id
                            select new
                            {
                                UserId = user.ID,
                                user.FirstName,
                                SegmentId = segment.Id,
                                segment.Segment
                            };

                var objs = await query.ToListAsync();
                foreach (var obj in objs)
                {
                    pi.Rms.Add(new GenericInfo()
                    {
                        Id = obj.UserId,
                        Text = obj.FirstName
                    });
                    if (pi.Segments.Where(a => a.Text.Equals(obj.Segment)).Count() == 0)
                    {
                        pi.Segments.Add(new GenericInfo()
                        {
                            Id = obj.SegmentId,
                            Text = obj.Segment
                        });
                    }
                }

                var q = from invs in _context.CrmDealProposalInvoices
                        where invs.ProposalId == i.ProposalId
                        select new InvoicePeriodInfo()
                        {
                            Id = invs.Id,
                            InvoiceDate = invs.InvoiceDate,
                            InvoiceAmount = invs.InvoiceAmount,
                            Remarks = invs.Remarks
                        };
                pi.Invoices = q.ToList();

                var q2 = from rec in _context.CrmDealProposalSentContacts
                         join contact in _context.CrmContacts
                         on rec.ContactId equals contact.Id
                         where rec.ProposalId == i.ProposalId
                         select new GenericInfo()
                         {
                             Id = rec.ContactId,
                             Text = contact.Name
                         };
                pi.ReceiverClients = q2.ToList();


                response.Items.Add(pi);
            }

            return response;

        }

        private async Task<IndividualReportProposal> DoGetIndividualReportProposal(List<String> roles, int userId, int fromMonth, int toMonth, int year, int page, int perPage, string search)
        {
            IndividualExportProposal ep = await _crmReportService.GetListProposalByUserIdRoles(roles, userId, fromMonth, toMonth, year, page, perPage, search);

            int total = ep.Info.total;

            IndividualReportProposal response = new IndividualReportProposal();
            response.Info = new PaginationInfo(page, perPage, total);
            response.Items = new List<IndividualReportProposalItem>();

            foreach (IndividualExportProposalItem i in ep.Items)
            {
                IndividualReportProposalItem pi = new IndividualReportProposalItem()
                {
                    ProposalId = i.ProposalId,
                    ProposalValue = i.ProposalValue,
                    Name = i.Name,
                    Type = i.Type,
                    SentById = i.SentById,
                    Filename = i.Filename,
                    SentBy = i.SentBy,
                    SentDate = i.SentDate
                };
                response.Items.Add(pi);
            }
            foreach (IndividualReportProposalItem item in response.Items)
            {
                var q = from invs in _context.CrmDealProposalInvoices
                        where invs.ProposalId == item.ProposalId
                        select new InvoicePeriodInfo()
                        {
                            Id = invs.Id,
                            InvoiceDate = invs.InvoiceDate,
                            InvoiceAmount = invs.InvoiceAmount,
                            Remarks = invs.Remarks
                        };
                item.Invoices = q.ToList();

                var q2 = from rec in _context.CrmDealProposalSentContacts
                         join contact in _context.CrmContacts
                         on rec.ContactId equals contact.Id
                         where rec.ProposalId == item.ProposalId
                         select new GenericInfo()
                         {
                             Id = rec.ContactId,
                             Text = contact.Name
                         };
                item.ReceiverClients = q2.ToList();
            }

            return response;

        }
        /**
         * @api {get} /pipeline/export/proposal/{userId}/{year}/{search} GET export proposal
         * @apiVersion 1.0.0
         * @apiName GetIndividualExportProposal
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} userId                id dari user yang datanya mau diambil
         * @apiParam {Number} year                  Tahun yang datanya ingin diambil, misal 2020
         * @apiParam {String} search                * untuk tidak menggunakan search, atau kata yang dicari.
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "headers": [
         *           "No.",
         *           "Proposal name",
         *           "Deal type",
         *           "Sent by",
         *           "Delivery date",
         *           "Proposal value"
         *       ],
         *       "items": [
         *           {
         *               "no": 1,
         *               "name": "License VirtualAC",
         *               "type": "Project",
         *               "sentBy": "Santi Susanti",
         *               "sentDate": "2020-04-22T00:00:00",
         *               "proposalValue": 72500000
         *           },
         *           {
         *               "no": 2,
         *               "name": "Roadmap BKKBN 2020",
         *               "type": "Workshop",
         *               "sentBy": "Santi Susanti",
         *               "sentDate": "2020-06-18T00:00:00",
         *               "proposalValue": 153500000
         *           }
         *       ]
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("export/proposal/{userId}/{year}/{search}")]
        public async Task<ActionResult<IndividualExportProposal>> GetIndividualExportProposal(int userId, int year, string search)
        {
            return await _crmReportService.GetListProposalByUserId("rm", userId, 1, 12, year, 0, 0, search);
            //return await _crmReportService.GetListProposalByUserId(userId, 1, DateTime.Now.Month, year, 0, 0, search);
        }

        /**
         * @api {get} /pipeline/export/proposal/{userId}/{fromMonth}/{toMonth}/{year}/{search} GET export proposal filter
         * @apiVersion 1.0.0
         * @apiName GetIndividualExportProposalFilter
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} userId                id dari user yang datanya mau diambil
         * @apiParam {Number} fromMonth             Mulai dari bulan berapa, misalnya 1 untuk bulan Januari
         * @apiParam {Number} toMonth               Sampai bulan berapa, misalnya 5 untuk bulan Mei
         * @apiParam {Number} year                  Tahun yang datanya ingin diambil, misal 2020
         * @apiParam {String} search                * untuk tidak menggunakan search, atau kata yang dicari.
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "headers": [
         *           "No.",
         *           "Proposal name",
         *           "Deal type",
         *           "Sent by",
         *           "Delivery date",
         *           "Proposal value"
         *       ],
         *       "items": [
         *           {
         *               "no": 1,
         *               "name": "License VirtualAC",
         *               "type": "Project",
         *               "sentBy": "Santi Susanti",
         *               "sentDate": "2020-04-22T00:00:00",
         *               "proposalValue": 72500000
         *           },
         *           {
         *               "no": 2,
         *               "name": "Roadmap BKKBN 2020",
         *               "type": "Workshop",
         *               "sentBy": "Santi Susanti",
         *               "sentDate": "2020-06-18T00:00:00",
         *               "proposalValue": 153500000
         *           }
         *       ]
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("export/proposal/{userId}/{fromMonth}/{toMonth}/{year}/{search}")]
        public async Task<ActionResult<IndividualExportProposal>> GetIndividualExportProposalFilter(int userId, int fromMonth, int toMonth, int year, string search)
        {
            return await _crmReportService.GetListProposalByUserId("rm", userId, fromMonth, toMonth, year, 0, 0, search);
            //return await _crmReportService.GetListProposalByUserId(userId, 1, DateTime.Now.Month, year, 0, 0, search);
        }

        /**
         * @api {get} /pipeline/export/pic/proposal/{userId}/{fromMonth}/{toMonth}/{year}/{search} GET export proposal PIC filter
         * @apiVersion 1.0.0
         * @apiName GetPicExportProposalFilter
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} userId                id dari user yang datanya mau diambil
         * @apiParam {Number} fromMonth             Mulai dari bulan berapa, misalnya 1 untuk bulan Januari
         * @apiParam {Number} toMonth               Sampai bulan berapa, misalnya 5 untuk bulan Mei
         * @apiParam {Number} year                  Tahun yang datanya ingin diambil, misal 2020
         * @apiParam {String} search                * untuk tidak menggunakan search, atau kata yang dicari.
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "headers": [
         *           "No.",
         *           "Proposal name",
         *           "Deal type",
         *           "Sent by",
         *           "Delivery date",
         *           "Proposal value"
         *       ],
         *       "items": [
         *           {
         *               "no": 1,
         *               "name": "License VirtualAC",
         *               "type": "Project",
         *               "sentBy": "Santi Susanti",
         *               "sentDate": "2020-04-22T00:00:00",
         *               "proposalValue": 72500000
         *           },
         *           {
         *               "no": 2,
         *               "name": "Roadmap BKKBN 2020",
         *               "type": "Workshop",
         *               "sentBy": "Santi Susanti",
         *               "sentDate": "2020-06-18T00:00:00",
         *               "proposalValue": 153500000
         *           }
         *       ]
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("export/pic/proposal/{userId}/{fromMonth}/{toMonth}/{year}/{search}")]
        public async Task<ActionResult<IndividualExportPICProposal>> GetPicExportProposalFilter(int userId, int fromMonth, int toMonth, int year, string search)
        {
            // Sebagai PIC
            List<string> roles = new List<string>();
            roles.Add("pic");
            IndividualExportProposalItemResponse r = await _crmReportService.GetIndividualExportProposalItems(roles, userId, fromMonth, toMonth, year, 0, 0, search);

            // Sebagai Leader
            List<string> roleRM = new List<string>();
            roleRM.Add("rm");
            List<int> userIds = await _context.CrmRelManagers.Where(a => a.LeaderId == userId && !a.IsDeleted && a.isActive).Select(a => a.UserId).ToListAsync();
            foreach (int id in userIds)
            {
                IndividualExportProposalItemResponse t = await _crmReportService.GetIndividualExportProposalItems(roleRM, id, fromMonth, toMonth, year, 0, 0, search);
                foreach (IndividualExportProposalItem item in t.Items)
                {
                    if (r.Items.Where(a => a.ProposalId == item.ProposalId).Count() == 0)
                    {
                        r.Items.Add(item);
                        r.Total++;
                    }
                }
            }

            IndividualExportPICProposal response = new IndividualExportPICProposal();

            response.Items = new List<IndividualExportPICProposalItem>();
            int n = 1;

            CrmDealRole role = GetDealRole("rm");
            foreach (IndividualExportProposalItem i in r.Items)
            {
                // IndividualExportPICProposalItem pi = new IndividualExportPICProposalItem(i, i.ProposalId);
                IndividualExportPICProposalItem pi = new IndividualExportPICProposalItem(i, n++);

                var query = from prop in _context.CrmDealProposals
                            join deal in _context.CrmDeals on prop.DealId equals deal.Id
                            join member in _context.CrmDealInternalMembers on deal.Id equals member.DealId
                            join user in _context.Users on member.UserId equals user.ID
                            join rm in _context.CrmRelManagers on user.ID equals rm.UserId
                            join segment in _context.CrmSegments on rm.SegmentId equals segment.Id
                            where prop.Id == i.ProposalId && !member.IsDeleted && !rm.IsDeleted && !segment.IsDeleted && member.RoleId == role.Id
                            select new
                            {
                                UserId = user.ID,
                                user.FirstName,
                                SegmentId = segment.Id,
                                segment.Segment
                            };

                var objs = await query.ToListAsync();
                if (objs != null)
                {
                    pi.Rms = String.Join(", ", objs.Select(a => a.FirstName).ToList());

                    pi.Segments = String.Join(", ", objs.Select(a => a.Segment).Distinct().ToList());
                }

                response.Items.Add(pi);
            }

            response.Headers = new List<string>(new string[] { "No.", "Proposal name", "Deal type", "Sent by", "Delivery date", "Proposal value", "RM", "Segment" });
            response.Info = new PaginationInfo(0, 0, r.Total);

            return response;


            //return await _crmReportService.GetListProposalByUserId("pic", userId, fromMonth, toMonth, year, 0, 0, search);
            //return await _crmReportService.GetListProposalByUserId(userId, 1, DateTime.Now.Month, year, 0, 0, search);
        }



        /**
         * @api {get} /pipeline/list/rm/{segmentIds}/{branchIds}/{page}/{perPage}/{search} GET list RM
         * @apiVersion 1.0.0
         * @apiName GetListRM
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {String} segmentIds            0 kalau tidak gunakan filer, atau comma-separated values dari segmentId, misal 1,2 
         * @apiParam {String} branchIds             0 kalau tidak gunakan filer, atau comma-separated values dari branchId, misal 1,2 
         * @apiParam {Number} page                  Halaman yang ditampilkan
         * @apiParam {Number} perPage               Jumlah item per halaman
         * @apiParam {String} search                * untuk tidak menggunakan search, atau kata yang dicari.
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "items": [
         *           {
         *               "id": 1,
         *               "userId": 3,
         *               "name": "Leviana Wijaya",
         *               "nik": "",
         *               "jobTitle": "",
         *               "platform": {
         *                   "id": 2,
         *                   "text": "Sales"
         *               },
         *               "segment": {
         *                   "id": 3,
         *                   "text": "Private"
         *               },
         *               "branch": {
         *                   "id": 1,
         *                   "text": "Jakarta"
         *               },
         *               "email": "",
         *               "phone": "",
         *               "address": "",
         *               "profileURL": ""
         *           }
         *       ],
         *       "info": {
         *           "page": 1,
         *           "perPage": 10,
         *           "total": 1
         *       }
         *   }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("list/rm/{segmentIds}/{branchIds}/{page}/{perPage}/{search}")]
        public async Task<ActionResult<RMList>> GetListRM(string segmentIds, string branchIds, int page, int perPage, string search)
        {
            RMList list = new RMList();

            List<int> sids = SplitString(segmentIds);
            List<int> bids = SplitString(branchIds);

            if (sids == null || bids == null) return BadRequest(new { error = "Error in converting Id to integer." });

            Func<ViewUserRM, bool> WherePredicate = v =>
            {
                bool sb = sids.Count() == 0 || sids.Contains(v.SegmentId);
                bool bb = bids.Count() == 0 || bids.Contains(v.BranchId);
                bool cb = search.Trim().Equals("*") || v.FirstName.Contains(search);

                return sb && bb && cb;
            };

            var query = from vu in _context.ViewUserRMs
                        where WherePredicate(vu)
                        select new RMListItem()
                        {
                            Id = vu.Id,
                            UserId = vu.UserId,
                            Name = vu.FirstName,
                            NIK = vu.IdNumber,
                            JobTitle = vu.JobTitle,
                            Platform = new GenericInfo()
                            {
                                Id = vu.PlatformId,
                                Text = vu.Platform
                            },
                            Segment = new GenericInfo()
                            {
                                Id = vu.SegmentId,
                                Text = vu.Segment
                            },
                            Branch = new GenericInfo()
                            {
                                Id = vu.BranchId,
                                Text = vu.Branch
                            },
                            Email = vu.Email,
                            Phone = vu.Phone,
                            Address = vu.Address,
                            ProfileURL = vu.FileURL + vu.Filename
                        };


            int total = query.Count();
            list.Items = await query.Skip(perPage * (page - 1)).Take(perPage).ToListAsync<RMListItem>();
            list.Info = new PaginationInfo(page, perPage, total);

            return list;
        }

        /**
         * @api {get} /pipeline/rm/{id} GET detail RM
         * @apiVersion 1.0.0
         * @apiName GetDetailRM
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "id": 3,
         *       "name": "Raja Pamungkas",
         *       "nik": "001008",
         *       "email": "raja@gmlperformance.co.id",
         *       "phone": "089898989898",
         *       "address": "",
         *       "jobTitle": "",
         *       "platform": {
         *           "id": 2,
         *           "text": "Sales"
         *       },
         *       "segment": {
         *           "id": 3,
         *           "text": "Private"
         *       },
         *       "branch": {
         *           "id": 1,
         *           "text": "Jakarta"
         *       },
         *       "fileUrl": "https://www.onegml.com/assettest/profiles/4btpbtyh.xgs.png"
         *   }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("rm/{id}")]
        public async Task<ActionResult<GetRMResponse>> GetDetailRM(int id)
        {
            var query = from rm in _context.CrmRelManagers
                        join u in _context.Users on rm.UserId equals u.ID
                        join segment in _context.CrmSegments on rm.SegmentId equals segment.Id
                        join branch in _context.CrmBranches on rm.BranchId equals branch.Id
                        join platform in _context.CorePlatforms on rm.PlatformId equals platform.Id
                        where rm.UserId == id && rm.isActive && !rm.IsDeleted && u.IsActive
                        select new GetRMResponse()
                        {
                            Id = u.ID,
                            Name = u.FirstName,
                            NIK = u.IdNumber == null ? "" : u.IdNumber,
                            Email = u.Email == null ? u.UserName : u.Email,
                            Phone = u.Phone == null ? "" : u.Phone,
                            Address = u.Address == null ? "" : u.Address,
                            JobTitle = rm.JobTitle,
                            Platform = new GenericInfo()
                            {
                                Id = platform.Id,
                                Text = platform.Platform
                            },
                            Segment = new GenericInfo()
                            {
                                Id = segment.Id,
                                Text = segment.Segment
                            },
                            Branch = new GenericInfo()
                            {
                                Id = branch.Id,
                                Text = branch.Branch
                            },
                            FileUrl = ""
                        };
            var response = query.FirstOrDefault();
            if (response == null) return NotFound();

            vProfileImage image = _context.vProfileImage.Find(id);
            if (image != null) response.FileUrl = image.FileURL + @"/" + image.FileName;

            return response;
        }

        /**
         * @api {post} /pipeline/rm POST RM 
         * @apiVersion 1.0.0
         * @apiName PostRM
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "id": 0,
         *     "name": "Arif",
         *     "nik": "08090999",
         *     "email": "arif@gmail.com",
         *     "phone": "090909090909",
         *     "address": "Bogor",
         *     "jobTitle": "RM Corporate",
         *     "platformId": 2,
         *     "segmentId": 1,
         *     "branchId": 1,
         *     "fileBase64": "data:image/jpeg;base64,...",
         *     "filename": "",
         *     "userId": 35
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   NoContent
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost("rm")]
        public async Task<ActionResult<GenericInfo>> PostRM(PostRMRequest request)
        {
            // Superadmin bisa menambah user.
            // User biasa cuma bisa mengedit punya sendiri dan terbatas field yang bisa di-editnya.
            /*
             *  1. Superadmin
                2. Chief of tribe
                3. Admin
                4. Consultant
                5. Sales
                6. Regular
                7. KCA admin
             */

            GenericInfo info = new GenericInfo();

            User user = _context.Users.Find(request.UserId);
            if (user == null)
            {
                return NotFound(new { error = "User not found." });
            }

            int userId = user.ID;
            DateTime now = DateTime.Now;

            if (user.RoleID == 1)
            {
                // superadmin
                // Bisa nambah user
                User curUser = _context.Users.Where(a => a.UserName.Equals(request.Email.Trim())).FirstOrDefault();
                if (curUser == null)
                {
                    userId = await _userService.AddUser(request.Name, request.Email, request.Phone, "123456", 3);
                    if (userId == 0) return BadRequest(new { error = "Error registering user." });

                    curUser = _context.Users.Find(userId);
                    if (curUser == null) return BadRequest(new { error = "Error registering user." });      // Unlikely
                }
                userId = curUser.ID;
                curUser.RoleID = 5;         // RM
                curUser.IsActive = true;
                curUser.IsDeleted = false;
                if (!string.IsNullOrEmpty(request.Name)) curUser.FirstName = request.Name;
                if (!string.IsNullOrEmpty(request.NIK)) curUser.IdNumber = request.NIK;
                if (!string.IsNullOrEmpty(request.Email)) curUser.Email = request.Email;
                if (!string.IsNullOrEmpty(request.Phone)) curUser.Phone = request.Phone;
                if (!string.IsNullOrEmpty(request.Address)) curUser.Address = request.Address;
                _context.Entry(curUser).State = EntityState.Modified;

                CrmRelManager rm = _context.CrmRelManagers.Where(a => a.UserId == curUser.ID && a.isActive && !a.IsDeleted).FirstOrDefault();
                if (rm == null)
                {
                    rm = new CrmRelManager()
                    {
                        JobTitle = request.JobTitle,
                        UserId = curUser.ID,
                        SegmentId = request.SegmentId,
                        BranchId = request.BranchId,
                        PlatformId = request.PlatformId,
                        LeaderId = 0,
                        TeamName = "",
                        CreatedDate = now,
                        CreatedBy = request.UserId,
                        LastUpdated = now,
                        LastUpdatedBy = request.UserId,
                        IsDeleted = false,
                        DeletedBy = 0,
                        DeletedDate = new DateTime(1970, 1, 1),
                        isActive = true,
                        DeactivatedBy = 0,
                        DeactivatedDate = new DateTime(1970, 1, 1)
                    };
                    _context.CrmRelManagers.Add(rm);
                }
                else
                {
                    rm.JobTitle = request.JobTitle;
                    rm.PlatformId = request.PlatformId;
                    rm.SegmentId = request.SegmentId;
                    rm.BranchId = request.BranchId;

                    _context.Entry(rm).State = EntityState.Modified;
                }
                await _context.SaveChangesAsync();

                info.Id = rm.Id;
                info.Text = curUser.FirstName;
            }
            else
            {
                if (!string.IsNullOrEmpty(request.Name)) user.FirstName = request.Name;
                if (!string.IsNullOrEmpty(request.Email)) user.Email = request.Email;
                if (!string.IsNullOrEmpty(request.Phone)) user.Phone = request.Phone;
                if (!string.IsNullOrEmpty(request.Address)) user.Address = request.Address;
                _context.Entry(user).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                CrmRelManager rm = _context.CrmRelManagers.Where(a => a.UserId == user.ID && a.isActive && !a.IsDeleted).FirstOrDefault();
                if (rm != null)
                {
                    info.Id = rm.Id;
                    info.Text = user.FirstName;
                }
                else
                {
                    rm = new CrmRelManager()
                    {
                        JobTitle = request.JobTitle,
                        UserId = user.ID,
                        SegmentId = request.SegmentId,
                        BranchId = request.BranchId,
                        PlatformId = request.PlatformId,
                        LeaderId = 0,
                        TeamName = "",
                        CreatedDate = now,
                        CreatedBy = request.UserId,
                        LastUpdated = now,
                        LastUpdatedBy = request.UserId,
                        IsDeleted = false,
                        DeletedBy = 0,
                        DeletedDate = new DateTime(1970, 1, 1),
                        isActive = true,
                        DeactivatedBy = 0,
                        DeactivatedDate = new DateTime(1970, 1, 1)
                    };
                    _context.CrmRelManagers.Add(rm);
                    await _context.SaveChangesAsync();
                    if (rm != null)
                    {
                        info.Id = rm.Id;
                        info.Text = user.FirstName;
                    }
                }

            }

            if (!string.IsNullOrEmpty(request.FileBase64))
            {
                int n = 0;
                string fileExt = "jpg";
                ImageFormat format = System.Drawing.Imaging.ImageFormat.Jpeg;

                if (request.FileBase64.StartsWith("data:image/jpeg;base64,"))
                {
                    n = 23;
                }
                else if (request.FileBase64.StartsWith("data:image/png;base64,"))
                {
                    n = 22;
                    format = System.Drawing.Imaging.ImageFormat.Png;
                    fileExt = "png";
                }
                if (n != 0)
                {
                    try
                    {
                        string baseDir = Path.Combine(new[] { _options.AssetsRootDirectory, "profiles" });
                        string base64String = request.FileBase64.Substring(n);

                        string randomName = Path.GetRandomFileName() + "." + fileExt;

                        if (_fileService.CheckAndCreateDirectory(baseDir))
                        {
                            var fileName = Path.Combine(baseDir, randomName);
                            _fileService.SaveByteArrayAsImage(fileName, base64String, format);

                            vProfileImage image = _context.vProfileImage.Where(a => a.Id == userId).FirstOrDefault();
                            if (image != null)
                            {
                                image.Id = userId;
                                image.IsDeleted = false;
                                image.FileURL = _options.AssetsBaseURL + "profiles/";
                                image.FileName = randomName;
                                image.Modified = now;
                                _context.Entry(image).State = EntityState.Modified;
                            }
                            else
                            {
                                image = new vProfileImage()
                                {
                                    Id = userId,
                                    IsDeleted = false,
                                    FileURL = _options.AssetsBaseURL + "profiles/",
                                    FileName = randomName,
                                    Created = now,
                                    Modified = now
                                };
                                _context.vProfileImage.Add(image);
                            }
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            // TODO Sewaktu deploy jangan lupa update assets virtual directory di IIS
                            return BadRequest(new { error = "Error in saving file" });
                        }


                    }
                    catch
                    {
                        return BadRequest(new { error = "Error in saving file" });
                    }
                }
                else
                {
                    return BadRequest(new { error = "Please upload file JPG or PNG only" });
                }

            }

            return info;
        }

        /**
         * @api {put} /pipeline/rm/{id} PUT RM 
         * @apiVersion 1.0.0
         * @apiName PutRM
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "id": 25,
         *     "name": "Arif",
         *     "nik": "08090999",
         *     "email": "arif@gmail.com",
         *     "phone": "090909090909",
         *     "address": "Bogor",
         *     "jobTitle": "RM Corporate",
         *     "platformId": 2,
         *     "segmentId": 1,
         *     "branchId": 1,
         *     "fileBase64": "data:image/jpeg;base64,...",
         *     "filename": "",
         *     "userId": 35
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   NoContent
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPut("rm/{id}")]
        public async Task<ActionResult> PutRM(PostRMRequest request)
        {
            User user = _context.Users.Find(request.UserId);
            if (user == null)
            {
                return NotFound(new { error = "User not found." });
            }

            int userId = user.ID;
            DateTime now = DateTime.Now;

            if (user.RoleID == 1)
            {
                CrmRelManager rm = _context.CrmRelManagers.Where(a => a.Id == request.Id && a.isActive && !a.IsDeleted).FirstOrDefault();
                if (rm == null) return NotFound(new { error = "RM not found." });

                User curUser = _context.Users.Find(rm.UserId);
                if (curUser == null)
                {
                    return NotFound(new { error = "User not found." });
                }
                userId = curUser.ID;
                curUser.IsActive = true;
                curUser.IsDeleted = false;
                curUser.RoleID = 5;         // RM

                if (!string.IsNullOrEmpty(request.Name)) curUser.FirstName = request.Name;
                if (!string.IsNullOrEmpty(request.NIK)) curUser.IdNumber = request.NIK;
                if (!string.IsNullOrEmpty(request.Email)) curUser.Email = request.Email;
                if (!string.IsNullOrEmpty(request.Phone)) curUser.Phone = request.Phone;
                if (!string.IsNullOrEmpty(request.Address)) curUser.Address = request.Address;
                _context.Entry(curUser).State = EntityState.Modified;

                rm.JobTitle = request.JobTitle;
                rm.PlatformId = request.PlatformId;
                rm.SegmentId = request.SegmentId;
                rm.BranchId = request.BranchId;

                _context.Entry(rm).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            else
            {
                if (request.Id != request.UserId) return BadRequest(new { error = "Can only upload own data." });

                if (!string.IsNullOrEmpty(request.Email)) user.Email = request.Email;
                if (!string.IsNullOrEmpty(request.Phone)) user.Phone = request.Phone;
                if (!string.IsNullOrEmpty(request.Address)) user.Address = request.Address;
                _context.Entry(user).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }

            if (!string.IsNullOrEmpty(request.FileBase64))
            {
                int n = 0;
                string fileExt = "jpg";
                ImageFormat format = System.Drawing.Imaging.ImageFormat.Jpeg;

                if (request.FileBase64.StartsWith("data:image/jpeg;base64,"))
                {
                    n = 23;
                }
                else if (request.FileBase64.StartsWith("data:image/png;base64,"))
                {
                    n = 22;
                    format = System.Drawing.Imaging.ImageFormat.Png;
                    fileExt = "png";
                }
                if (n != 0)
                {
                    try
                    {
                        string baseDir = Path.Combine(new[] { _options.AssetsRootDirectory, "profiles" });
                        string base64String = request.FileBase64.Substring(n);

                        string randomName = Path.GetRandomFileName() + "." + fileExt;

                        if (_fileService.CheckAndCreateDirectory(baseDir))
                        {
                            var fileName = Path.Combine(baseDir, randomName);
                            _fileService.SaveByteArrayAsImage(fileName, base64String, format);

                            vProfileImage image = _context.vProfileImage.Where(a => a.Id == userId).FirstOrDefault();
                            if (image != null)
                            {
                                image.Id = userId;
                                image.IsDeleted = false;
                                image.FileURL = _options.AssetsBaseURL + "profiles/";
                                image.FileName = randomName;
                                image.Modified = now;
                                _context.Entry(image).State = EntityState.Modified;
                            }
                            else
                            {
                                image = new vProfileImage()
                                {
                                    Id = userId,
                                    IsDeleted = false,
                                    FileURL = _options.AssetsBaseURL + "profiles/",
                                    FileName = randomName,
                                    Created = now,
                                    Modified = now
                                };
                                _context.vProfileImage.Add(image);
                            }
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            // TODO Sewaktu deploy jangan lupa update assets virtual directory di IIS
                            return BadRequest(new { error = "Error in saving file" });
                        }


                    }
                    catch
                    {
                        return BadRequest(new { error = "Error in saving file" });
                    }
                }
                else
                {
                    return BadRequest(new { error = "Please upload file JPG or PNG only" });
                }

            }

            return NoContent();
        }

        /**
         * @api {delete} /pipeline/rm/{Id}/{userId} DELETE RM
         * @apiVersion 1.0.0
         * @apiName DeleteRM
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {invoiceId} id         Id dari RM yang ingin dihapus
         * @apiParam {Number} userId        Id dari user yang sedang login
         */
        [Authorize(Policy = "ApiUser")]
        [HttpDelete("rm/{Id}/{userId}")]
        public async Task<ActionResult<CrmRelManager>> DeleteRM(int id, int userId)
        {
            User user = _context.Users.Find(userId);
            if (user == null) return NotFound(new { error = "User not found." });

            if (user.RoleID != 1) return Unauthorized();

            CrmRelManager rm = _context.CrmRelManagers.Where(a => a.Id == id).FirstOrDefault();
            if (rm == null) return NotFound(new { error = "RM not found." });

            rm.IsDeleted = true;
            rm.DeletedBy = userId;
            rm.DeletedDate = DateTime.Now;
            _context.Entry(rm).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return rm;
        }

        /**
         * @api {post} /pipeline/team POST team
         * @apiVersion 1.0.0
         * @apiName PostTeam
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "id": 0,
         *     "name": "Group Arif",
         *     "leaderId": 27,
         *     "mentorId": 1,
         *     "members": [
         *       25, 26
         *     ],
         *     "userId": 1
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   {
         *       "id": 23,
         *       "jobTitle": "",
         *       "userId": 27,
         *       "segmentId": 0,
         *       "branchId": 2,
         *       "platformId": 2,
         *       "leaderId": 1,
         *       "isTeamLeader": true,
         *       "teamName": "Group Arif",
         *       "createdDate": "2020-06-17T00:00:00",
         *       "createdBy": 1,
         *       "lastUpdated": "2021-08-02T12:35:04.9263782+07:00",
         *       "lastUpdatedBy": 0,
         *       "isDeleted": false,
         *       "deletedBy": 0,
         *       "deletedDate": "1970-01-01T00:00:00",
         *       "isActive": true,
         *       "deactivatedBy": 0,
         *       "deactivatedDate": "1970-01-01T00:00:00"
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost("team")]
        public async Task<ActionResult<CrmRelManager>> PostTeam(PostTeamRequest request)
        {
            CrmRelManager newLeader = _context.CrmRelManagers.Where(a => a.UserId == request.LeaderId && a.isActive && !a.IsDeleted).FirstOrDefault();
            if (newLeader == null) return NotFound(new { error = "Leader not found. Please check leaderId." });

            DateTime now = DateTime.Now;

            newLeader.IsTeamLeader = true;
            newLeader.TeamName = request.Name;
            if (request.MentorId != 0) newLeader.LeaderId = request.MentorId;
            newLeader.LastUpdated = now;
            newLeader.LastUpdatedBy = request.UserId;
            _context.Entry(newLeader).State = EntityState.Modified;

            foreach (int m in request.Members)
            {
                CrmRelManager member = _context.CrmRelManagers.Where(a => a.UserId == m && a.isActive && !a.IsDeleted).FirstOrDefault();
                if (member != null)
                {
                    member.LeaderId = newLeader.UserId;
                    member.LastUpdated = now;
                    member.LastUpdatedBy = request.UserId;
                    _context.Entry(member).State = EntityState.Modified;
                }
            }

            User user = _context.Users.Find(newLeader.UserId);
            if (user != null)
            {
                user.RoleID = 2;            // Chief of tribe
                _context.Entry(user).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();

            return newLeader;
        }

        /**
         * @api {put} /pipeline/team/{id} PUT team
         * @apiVersion 1.0.0
         * @apiName PutTeam
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "id": 23,
         *     "name": "Group Arif Bijak",
         *     "leaderId": 27,
         *     "mentorId": 1,
         *     "members": [
         *       25, 26
         *     ],
         *     "userId": 1
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   {
         *       "id": 27,
         *       "jobTitle": "RM Corporate",
         *       "userId": 109,
         *       "segmentId": 1,
         *       "branchId": 1,
         *       "platformId": 2,
         *       "leaderId": 1,
         *       "isTeamLeader": true,
         *       "teamName": "Group Arif Bijak",
         *       "createdDate": "2021-07-27T12:51:09.5237621",
         *       "createdBy": 35,
         *       "lastUpdated": "2021-08-02T12:28:48.9359339+07:00",
         *       "lastUpdatedBy": 0,
         *       "isDeleted": true,
         *       "deletedBy": 35,
         *       "deletedDate": "2021-07-27T20:35:55.7794262",
         *       "isActive": true,
         *       "deactivatedBy": 0,
         *       "deactivatedDate": "1970-01-01T00:00:00"
         *   }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPut("team/{id}")]
        public async Task<ActionResult<CrmRelManager>> PutTeam(int id, PostTeamRequest request)
        {
            if (id != request.Id) return BadRequest(new { error = "Please check Id." });

            DateTime now = DateTime.Now;

            CrmRelManager curLeader = _context.CrmRelManagers.Find(request.Id);
            if (curLeader == null) return NotFound(new { error = "Team not found. Please check Id." });

            if (request.LeaderId == 0)
            {
                await DeleteTeam(curLeader, now, request.UserId);
                return curLeader;
            }
            CrmRelManager newLeader = _context.CrmRelManagers.Where(a => a.UserId == request.LeaderId).FirstOrDefault();
            if (newLeader == null) return NotFound(new { error = "Leader not found. Please check leaderId." });

            if (curLeader.Id != newLeader.Id)
            {
                await DeleteTeam(curLeader, now, request.UserId);
            }

            newLeader.IsTeamLeader = true;
            newLeader.TeamName = request.Name;
            newLeader.LeaderId = request.MentorId;
            newLeader.LastUpdated = now;
            newLeader.LastUpdatedBy = request.UserId;
            _context.Entry(newLeader).State = EntityState.Modified;

            List<CrmRelManager> currentMembers = await _context.CrmRelManagers.Where(a => a.LeaderId == newLeader.UserId && a.isActive && !a.IsDeleted).ToListAsync();
            foreach (CrmRelManager curmember in currentMembers)
            {
                if (!request.Members.Contains(curmember.UserId))
                {
                    curmember.LeaderId = 0;
                    curmember.LastUpdated = now;
                    curmember.LastUpdatedBy = request.UserId;
                    _context.Entry(curmember).State = EntityState.Modified;
                }
            }
            foreach (int m in request.Members)
            {
                if (m != 0)
                {
                    if (!currentMembers.Where(a => a.UserId == m).Any())
                    {
                        CrmRelManager member = _context.CrmRelManagers.Where(a => a.UserId == m && a.isActive && !a.IsDeleted).FirstOrDefault();
                        if (member != null)
                        {
                            member.LeaderId = newLeader.UserId;
                            member.LastUpdated = now;
                            member.LastUpdatedBy = request.UserId;
                            _context.Entry(member).State = EntityState.Modified;
                        }
                    }
                }
            }

            User user = _context.Users.Find(newLeader.UserId);
            if (user != null)
            {
                user.RoleID = 2;            // Chief of tribe
                _context.Entry(user).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();

            return newLeader;
        }

        private async Task<int> DeleteTeam(CrmRelManager curLeader, DateTime now, int userId)
        {
            curLeader.IsTeamLeader = false;
            curLeader.TeamName = "";
            curLeader.LeaderId = 0;
            curLeader.LastUpdated = now;
            curLeader.LastUpdatedBy = userId;
            _context.Entry(curLeader).State = EntityState.Modified;

            List<CrmRelManager> curMembers = await _context.CrmRelManagers.Where(a => a.LeaderId == curLeader.UserId && a.isActive && !a.IsDeleted).ToListAsync();
            foreach (CrmRelManager curmember in curMembers)
            {
                curmember.LeaderId = 0;
                curmember.LastUpdated = now;
                curmember.LastUpdatedBy = userId;
                _context.Entry(curmember).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();

            User user = _context.Users.Find(curLeader.UserId);
            if (user != null)
            {
                user.RoleID = 5;            // RM
                _context.Entry(user).State = EntityState.Modified;
            }

            return curLeader.Id;
        }

        /**
         * @api {get} /Pipeline/team GET team
         * @apiVersion 1.0.0
         * @apiName GetTeams
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiSuccessExample Success-Response:
         * [
         *     {
         *         "id": 21,
         *         "text": "Cabang Surabaya",
         *         "leader": {
         *             "id": 24,
         *             "text": "Adhitya Ernas"
         *         },
         *         "mentor": {
         *             "id": 3,
         *             "text": "Leviana Wijaya LV"
         *         },
         *         "members": [
         *             {
         *                 "id": 25,
         *                 "text": "Dekik Tanti Sukmandari"
         *             }
         *         ]
         *     }
         * ]
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("team")]
        public async Task<ActionResult<List<Team>>> GetTeams()
        {
            List<Team> teams = new List<Team>();

            var query = _context.CrmRelManagers.Where(a => a.isActive && !a.IsDeleted && a.IsTeamLeader).Select(a => new
            {
                a.Id,
                a.TeamName,
                a.LeaderId,
                a.UserId
            });
            var objs = await query.ToListAsync();
            foreach (var obj in objs)
            {
                GenericInfo leader = _context.Users.Where(a => a.ID == obj.UserId).Select(a => new GenericInfo()
                {
                    Id = a.ID,
                    Text = a.FirstName
                }).FirstOrDefault();
                if (leader != null)
                {
                    Team nt = new Team();
                    nt.Id = obj.Id;
                    nt.Text = obj.TeamName;
                    nt.Leader = new GenericInfo()
                    {
                        Id = leader.Id,
                        Text = leader.Text
                    };
                    if (obj.LeaderId != 0)
                    {
                        GenericInfo mentor = _context.Users.Where(a => a.ID == obj.LeaderId).Select(a => new GenericInfo()
                        {
                            Id = a.ID,
                            Text = a.FirstName
                        }).FirstOrDefault();
                        if (mentor != null)
                        {
                            nt.Mentor = new GenericInfo()
                            {
                                Id = mentor.Id,
                                Text = mentor.Text
                            };
                        }
                    }
                    var q = from rm in _context.CrmRelManagers
                            join u in _context.Users on rm.UserId equals u.ID
                            where rm.isActive && !rm.IsDeleted && rm.LeaderId == obj.UserId
                            select new GenericInfo()
                            {
                                Id = u.ID,
                                Text = u.FirstName
                            };
                    nt.Members = await q.ToListAsync();
                    teams.Add(nt);
                }
            }
            return teams;
        }

        /**
         * @api {get} /Pipeline/export/all/{fromMonth}/{toMonth}/{year} GET export all
         * @apiVersion 1.0.0
         * @apiName GetExportAll
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiSuccessExample Success-Response:
         * {
         *     "sheet1": {
         *         "title": "Sales Activity Report by Tribe",
         *         "period": "Period: Jan - Aug",
         *         "items": [
         *             {
         *                 "id": 3,
         *                 "name": "Leviana Wijaya",
         *                 "tribe": "Digital Learning Solutions",
         *                 "actualNProposal": 0,
         *                 "actualProposalValue": 0,
         *                 "actualSalesVisit": 1
         *             }
         *         ]
         *     },
         *     "sheet2": {
         *         "title": "Sales Activity Report",
         *         "period": "Period: Jan - Aug",
         *         "items": [
         *             {
         *                 "id": 3,
         *                 "name": "Leviana Wijaya",
         *                 "targetNProposal": 0,
         *                 "targetProposalValue": 0,
         *                 "targetSalesVisit": 0,
         *                 "targetSales": 0,
         *                 "actualNProposal": 0,
         *                 "actualProposalValue": 0,
         *                 "actualSalesVisit": 12,
         *                 "actualSales": 0,
         *                 "achNProposal": 0.0,
         *                 "achProposalValue": 0.0,
         *                 "achSalesVisit": 0.0,
         *                 "achSales": 0.0,
         *                 "aveAch": 0.0
         *             }
         *         ]
         *     }
         * }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("export/all/{fromMonth}/{toMonth}/{year}")]
        public async Task<ActionResult<Achievement>> GetExportAll(int fromMonth, int toMonth, int year)
        {
            DateTime from = new DateTime(year, fromMonth, 1);
            DateTime to = new DateTime(year, toMonth, 1);
            string period = "Period: " + from.ToString("MMM", CultureInfo.CreateSpecificCulture("en")) + @" - " + to.ToString("MMM", CultureInfo.CreateSpecificCulture("en")) + " " + to.ToString("yyyy", CultureInfo.CreateSpecificCulture("en"));

            Achievement achievement = new Achievement();
            achievement.Sheet1 = new AchievementSheetByTribe();
            achievement.Sheet1.Title = "Sales Activity Report by Tribe";
            achievement.Sheet1.Period = period;
            achievement.Sheet1.Items = new List<AchievementItemByTribe>();

            CrmDealRole role = GetDealRole("rm");

            // by TRIBE 
            List<CoreTribe> tribes = await _context.CoreTribes.Where(a => !a.IsDeleted).ToListAsync();

            foreach (CoreTribe tribe in tribes)
            {
                var query = from proposal in _context.CrmDealProposals
                            join deal in _context.CrmDeals
                            on proposal.DealId equals deal.Id
                            join dealTribe in _context.CrmDealTribes
                            on deal.Id equals dealTribe.DealId
                            join segment in _context.CrmSegments
                            on deal.SegmentId equals segment.Id
                            join client in _context.CrmClients
                            on deal.ClientId equals client.Id
                            join per in _context.CrmPeriods
                            on proposal.PeriodId equals per.Id
                            where !deal.IsDeleted && !proposal.IsDeleted && per.Month >= fromMonth && per.Month <= toMonth && per.Year == year && dealTribe.TribeId == tribe.Id
                            select new
                            {
                                proposal.Id,
                                DealId = deal.Id,
                                Company = client.Company,
                                Segment = segment.Segment,
                                DealName = deal.Name,
                                Amount = proposal.ProposalValue
                            };

                var objs = await query.ToListAsync();

                List<int> propIds = new List<int>();

                foreach (var obj in objs)
                {
                    if (propIds.Contains(obj.Id)) continue;
                    propIds.Add(obj.Id);

                    var q1 = from dealTribe in _context.CrmDealTribes
                             join t in _context.CoreTribes
                             on dealTribe.TribeId equals t.Id
                             where !dealTribe.IsDeleted && !t.IsDeleted && dealTribe.DealId == obj.DealId
                             orderby dealTribe.Percentage descending
                             select new
                             {
                                 Id = tribe.Id,
                                 Text = tribe.Shortname.ToUpper(),
                                 Percent = dealTribe.Percentage
                             };

                    var objs2 = await q1.ToListAsync();
                    string curTribe = "";
                    if (objs2 != null && objs2.Count() >= 1)
                    {
                        curTribe = objs2[0].Text;
                    }

                    var q2 = from member in _context.CrmDealInternalMembers
                             join user in _context.Users
                             on member.UserId equals user.ID
                             where !member.IsDeleted && member.DealId == obj.DealId
                             select new
                             {
                                 Id = user.ID,
                                 Text = user.FirstName,
                                 Percent = member.Percentage
                             };
                    var obj3 = await q2.ToListAsync();
                    foreach (var o in obj3)
                    {
                        AchievementItemByTribe item = achievement.Sheet1.Items.Where(a => a.Id == o.Id && a.Tribe.Equals(curTribe)).FirstOrDefault();
                        if (item == null)
                        {
                            item = new AchievementItemByTribe()
                            {
                                Id = o.Id,
                                Name = o.Text,
                                Tribe = curTribe,
                                ActualNProposal = 1,
                                ActualProposalValue = Convert.ToInt64(Convert.ToDouble(obj.Amount) * Convert.ToDouble(o.Percent) / 100.0d),
                            };
                            achievement.Sheet1.Items.Add(item);
                        }
                        else
                        {
                            item.ActualNProposal++;
                            item.ActualProposalValue += Convert.ToInt64(Convert.ToDouble(obj.Amount) * Convert.ToDouble(o.Percent) / 100.0d);
                        }
                    }

                }
                List<VisitByTribe> visits = await _crmReportService.GetActualVisitsByTribe(tribe.Id, fromMonth, toMonth, year);
                foreach (VisitByTribe visit in visits)
                {
                    AchievementItemByTribe item = achievement.Sheet1.Items.Where(a => a.Id == visit.Id && a.Tribe.Equals(tribe.Shortname.ToUpper())).FirstOrDefault();
                    if (item == null)
                    {
                        item = new AchievementItemByTribe()
                        {
                            Id = visit.Id,
                            Name = visit.Firstname,
                            Tribe = tribe.Shortname.ToUpper(),
                            ActualNProposal = 0,
                            ActualProposalValue = 0,
                            ActualSalesVisit = visit.Visit
                        };
                        achievement.Sheet1.Items.Add(item);
                    }
                    else
                    {
                        item.ActualSalesVisit = visit.Visit;
                    }
                }
            }

            List<VisitByTribe> notribes = await _crmReportService.GetActualVisitsNoTribe(fromMonth, toMonth, year);
            foreach (VisitByTribe nt in notribes)
            {
                AchievementItemByTribe item = achievement.Sheet1.Items.Where(a => a.Id == nt.Id && a.Tribe.Equals("OTHERS")).FirstOrDefault();
                if (item == null)
                {
                    item = new AchievementItemByTribe()
                    {
                        Id = nt.Id,
                        Name = nt.Firstname,
                        Tribe = "OTHERS",
                        ActualNProposal = 0,
                        ActualProposalValue = 0,
                        ActualSalesVisit = nt.Visit
                    };
                    achievement.Sheet1.Items.Add(item);
                }
                else
                {
                    item.ActualSalesVisit += nt.Visit;
                }
            }

            achievement.Sheet2 = new AchievementSheet();
            achievement.Sheet2.Title = "Sales Activity Report";
            achievement.Sheet2.Period = period;
            achievement.Sheet2.Items = new List<AchievementItem>();

            foreach (AchievementItemByTribe item in achievement.Sheet1.Items)
            {
                AchievementItem i = achievement.Sheet2.Items.Where(a => a.Id == item.Id).FirstOrDefault();
                if (i == null)
                {
                    i = new AchievementItem()
                    {
                        Id = item.Id,
                        Name = item.Name,
                        ActualNProposal = item.ActualNProposal,
                        ActualProposalValue = item.ActualProposalValue,
                        ActualSalesVisit = item.ActualSalesVisit,
                        ActualSales = _crmReportService.GetActualSalesByUserId(item.Id, fromMonth, toMonth, year)
                    };
                    achievement.Sheet2.Items.Add(i);
                }
                else
                {
                    i.ActualNProposal += item.ActualNProposal;
                    i.ActualProposalValue += item.ActualProposalValue;
                    i.ActualSalesVisit += item.ActualSalesVisit;
                }
            }

            achievement.GeneratedDate = DateTime.Now;

            return achievement;
        }

        /**
         * @api {delete} /pipeline/team/{id}/{userId} DELETE team
         * @apiVersion 1.0.0
         * @apiName DeleteTeam
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {Number} id            Id dari team yang ingin dihapus
         * @apiParam {Number} userId        Id dari user yang sedang login
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *       "id": 27,
         *       "jobTitle": "RM Corporate",
         *       "userId": 109,
         *       "segmentId": 1,
         *       "branchId": 1,
         *       "platformId": 2,
         *       "leaderId": 1,
         *       "isTeamLeader": true,
         *       "teamName": "Group Arif Bijak",
         *       "createdDate": "2021-07-27T12:51:09.5237621",
         *       "createdBy": 35,
         *       "lastUpdated": "2021-08-02T12:28:48.9359339+07:00",
         *       "lastUpdatedBy": 0,
         *       "isDeleted": true,
         *       "deletedBy": 35,
         *       "deletedDate": "2021-07-27T20:35:55.7794262",
         *       "isActive": true,
         *       "deactivatedBy": 0,
         *       "deactivatedDate": "1970-01-01T00:00:00"
         *   }
         */
        [Authorize(Policy = "ApiUser")]
        [HttpDelete("team/{id}/{userId}")]
        public async Task<ActionResult<CrmRelManager>> DeleteTeam(int id, int userId)
        {
            CrmRelManager curLeader = _context.CrmRelManagers.Find(id);
            if (curLeader == null) return NotFound(new { error = "Team not found. Please check Id." });

            DateTime now = DateTime.Now;

            await DeleteTeam(curLeader, now, userId);

            return curLeader;
        }


        private List<int> SplitString(string ids)
        {
            List<int> nids = new List<int>();
            if (ids.Trim().Equals("0")) return nids;

            foreach (string s in ids.Trim().Split(","))
            {
                try
                {
                    nids.Add(Convert.ToInt32(s));
                }
                catch
                {
                    return null;
                }
            }
            return nids;
        }
        /*
        [AllowAnonymous]
        [HttpGet("update")]
        public async Task<ActionResult> UpdateDatabase()
        {
            if (Request.Headers["Authorization"].ToString() != "" && Request.Headers["Authorization"].ToString().StartsWith("Basic "))
            {
                var authHeader = Request.Headers["Authorization"].ToString();
                authHeader = authHeader.Trim();
                string encodedCredentials = authHeader.Substring(6);
                var credentialBytes = Convert.FromBase64String(encodedCredentials);
                var credentials = System.Text.Encoding.UTF8.GetString(credentialBytes).Split(':');
                var username = credentials[0];
                var password = credentials[1];
                if (username == "onegmlapi" && password == "O1n6e0G4M7L")
                {
                    */
        /*
        List<CrmDealVisit> visits = await _context.CrmDealVisits.ToListAsync();
        foreach(CrmDealVisit visit in visits)
        {
            CrmDeal deal = _context.CrmDeals.Find(visit.DealId);
            if(deal != null)
            {
                visit.ClientId = deal.ClientId;
                _context.Entry(visit).State = EntityState.Modified;
            }
        }
        await _context.SaveChangesAsync();
        */
        /*
        List<CrmDealTribeInvoice> tribeInvoices = await _context.CrmDealTribeInvoices.Where(a => a.Amount == 0).ToListAsync();
        foreach(CrmDealTribeInvoice invoice in tribeInvoices)
        {
            CrmDealInvoice dealInvoice = _context.CrmDealInvoices.Find(invoice.InvoiceId);
            if(dealInvoice != null)
            {
                invoice.Amount = Convert.ToInt64(Math.Round(Convert.ToSingle(dealInvoice.Amount) * Convert.ToSingle(Convert.ToSingle(invoice.Percentage) / Convert.ToSingle(100))));
                _context.Entry(invoice).State = EntityState.Modified;
            }
        }

        List<CrmDealUserInvoice> userInvoices = await _context.CrmDealUserInvoices.Where(a => a.Amount == 0).ToListAsync();
        foreach (CrmDealUserInvoice invoice in userInvoices)
        {
            CrmDealInvoice dealInvoice = _context.CrmDealInvoices.Find(invoice.InvoiceId);
            if (dealInvoice != null)
            {
                invoice.Amount = Convert.ToInt64(Math.Round(Convert.ToSingle(dealInvoice.Amount) * Convert.ToSingle(Convert.ToSingle(invoice.Percentage) / Convert.ToSingle(100))));
                _context.Entry(invoice).State = EntityState.Modified;
            }
        }
        await _context.SaveChangesAsync();
        */
        /*
        List<CrmDealTribeInvoice> tribeInvoices = await _context.CrmDealTribeInvoices.ToListAsync();
        foreach (CrmDealTribeInvoice invoice in tribeInvoices)
        {
            CrmDealInvoice dealInvoice = _context.CrmDealInvoices.Find(invoice.InvoiceId);
            if (dealInvoice != null)
            {
                long newAmount = Convert.ToInt64(Math.Round(Convert.ToSingle(dealInvoice.Amount) * Convert.ToSingle(Convert.ToSingle(invoice.Percentage) / Convert.ToSingle(100))));
                if(invoice.Amount != newAmount)
                {
                    invoice.Amount = newAmount;
                    _context.Entry(invoice).State = EntityState.Modified;
                }
            }
        }

        List<CrmDealUserInvoice> userInvoices = await _context.CrmDealUserInvoices.Where(a => a.Amount == 0).ToListAsync();
        foreach (CrmDealUserInvoice invoice in userInvoices)
        {
            CrmDealInvoice dealInvoice = _context.CrmDealInvoices.Find(invoice.InvoiceId);
            if (dealInvoice != null)
            {
                long newAmount2 = Convert.ToInt64(Math.Round(Convert.ToSingle(dealInvoice.Amount) * Convert.ToSingle(Convert.ToSingle(invoice.Percentage) / Convert.ToSingle(100))));
                if(invoice.Amount != newAmount2)
                {
                    invoice.Amount = newAmount2;
                    _context.Entry(invoice).State = EntityState.Modified;
                }
            }
        }
        await _context.SaveChangesAsync();

        return NoContent();

    }
}

return Unauthorized();
}
*/
        // Private functions start here
        private async Task<ChartData> GetChartData(string type, int id, GenericInfo kpi, int fromMonth, int toMonth, int year)
        {
            ChartData data = new ChartData();

            data.Title = kpi.Text;

            for (int i = fromMonth; i <= toMonth; i++)
            {
                data.Xaxis.Add(new DateTime(year, i, 1).ToString("MMMM", CultureInfo.CreateSpecificCulture("en")));
            }

            if (kpi.Text.Equals("# of Proposals"))
            {
                data.Series = await GetNProposalSeries(type, id, kpi, fromMonth, toMonth, year);
            }
            else if (kpi.Text.Equals("Proposal Value"))
            {
                data.Series = await GetProposalValueSeries(type, id, kpi, fromMonth, toMonth, year);
            }
            else if (kpi.Text.Equals("# of Sales Visits"))
            {
                data.Series = await GetNSalesSeries(type, id, kpi, fromMonth, toMonth, year);
            }
            else if (kpi.Text.Equals("Sales"))
            {
                data.Series = await GetSalesSeries(type, id, kpi, fromMonth, toMonth, year);
            }

            return data;
        }

        private async Task<List<ChartSeriesItem>> GetNProposalSeries(string type, int id, GenericInfo kpi, int fromMonth, int toMonth, int year)
        {
            string targetSql = GetTargetSql(type, id, kpi, fromMonth, toMonth, year);
            string actualSql = GetActualNProposalSql(type, id, fromMonth, toMonth, year);

            if (targetSql == null || actualSql == null)
            {
                return null;
            }
            string sql = string.Join(" ", new[]
            {
                "SELECT ISNULL(a.Id, b.Id) AS Id, CAST(ISNULL(a.Target, 0) as bigint) AS Target, CAST(ISNULL(b.Actual, 0) AS bigint) AS Actual, ISNULL(a.Month, b.Month) AS Month,",
                "CASE",
                "WHEN Target IS NULL THEN 0",
                "WHEN Target = 0 THEN 0",
                "ELSE ISNULL(CAST((Actual * 100)/Target as bigint), 0) END AS Achievement FROM (",
                targetSql,
                ") AS a FULL OUTER JOIN (",
                actualSql,
                ") as b on a.Month = b.Month ORDER BY Month"
            });

            return await _context.ChartSeriesItems.FromSql(sql).ToListAsync();
        }
        private async Task<List<ChartSeriesItem>> GetProposalValueSeries(string type, int id, GenericInfo kpi, int fromMonth, int toMonth, int year)
        {
            string targetSql = GetTargetSql(type, id, kpi, fromMonth, toMonth, year);
            string actualSql = GetActualProposalValueSql(type, id, fromMonth, toMonth, year);

            if (targetSql == null || actualSql == null)
            {
                return null;
            }
            string sql = string.Join(" ", new[]
            {
                "SELECT ISNULL(a.Id, b.Id) AS Id, CAST(ISNULL(a.Target, 0) as bigint) AS Target, CAST(ISNULL(b.Actual, 0) AS bigint) AS Actual, ISNULL(a.Month, b.Month) AS Month,",
                "CASE",
                "WHEN Target IS NULL THEN 0",
                "WHEN Target = 0 THEN 0",
                "ELSE ISNULL(CAST((Actual * 100)/Target as bigint), 0) END AS Achievement FROM (",
                targetSql,
                ") AS a FULL OUTER JOIN (",
                actualSql,
                ") as b on a.Month = b.Month ORDER BY Month"
            });

            return await _context.ChartSeriesItems.FromSql(sql).ToListAsync();

        }

        private async Task<List<ChartSeriesItem>> GetNSalesSeries(string type, int id, GenericInfo kpi, int fromMonth, int toMonth, int year)
        {
            string targetSql = GetTargetSql(type, id, kpi, fromMonth, toMonth, year);
            string actualSql = GetActualNSalesSql(type, id, fromMonth, toMonth, year);

            if (targetSql == null || actualSql == null)
            {
                return null;
            }
            string sql = string.Join(" ", new[]
            {
                "SELECT ISNULL(a.Id, b.Id) AS Id, CAST(ISNULL(a.Target, 0) as bigint) AS Target, CAST(ISNULL(b.Actual, 0) AS bigint) AS Actual, ISNULL(a.Month, b.Month) AS Month,",
                "CASE",
                "WHEN Target IS NULL THEN 0",
                "WHEN Target = 0 THEN 0",
                "ELSE ISNULL(CAST((Actual * 100)/Target as bigint), 0) END AS Achievement FROM (",
                targetSql,
                ") AS a FULL OUTER JOIN (",
                actualSql,
                ") as b on a.Month = b.Month ORDER BY Month"
            });

            return await _context.ChartSeriesItems.FromSql(sql).ToListAsync();


        }
        private async Task<List<AchievementItemByTribe>> GetAchievementItemByTribes(int fromMonth, int toMonth, int year)
        {
            string sql = string.Join(" ", new[]
                        {
                            "select y.*, u.FirstName as Name from",
                            "(select ",
                            "CASE",
                            "WHEN x.AId is null THEN ",
                            "CASE",
                            "WHEN x.BId is null then x.cId",
                            "ELSE x.BId",
                            "END",
                            "ELSE x.AId",
                            "END AS Id, ",
                            "CASE",
                            "WHEN x.ATribe is null THEN ",
                            "CASE",
                            "WHEN x.BTribe is null then x.CTribe",
                            "ELSE x.BTribe",
                            "END",
                            "ELSE x.ATribe",
                            "END AS Tribe, ",
                            "ISNULL(x.ActualNProposal, 0) as ActualNProposal, ISNULL(x.ActualProposalValue, 0) as ActualProposalValue, ISNULL(x.ActualSalesVisit, 0) as ActualSalesVisit",
                            "from",
                            "(select a.*, b.*, c.* from",
                            "(SELECT Count(Distinct(proposal.Id)) AS ActualNProposal, member.UserId as AId, tribe.Id as ATribeId, tribe.Tribe as ATribe",
                            "FROM dbo.CrmDealProposals as proposal ",
                            "JOIN dbo.CrmDeals AS deal ON proposal.DealId = deal.Id ",
                            "join dbo.CrmDealTribes as dt on deal.Id = dt.DealId",
                            "join dbo.CoreTribes as tribe on dt.TribeId = tribe.Id",
                            "JOIN dbo.CrmPeriods as per ON proposal.PeriodId = per.Id ",
                            "JOIN dbo.CrmDealInternalMembers AS member ON member.DealId = deal.Id ",
                            "WHERE per.Month >= " + fromMonth.ToString() + " AND per.Month <= " + toMonth.ToString() + " AND per.Year =  " + year.ToString() + " ",
                            "AND member.RoleId = 1  AND proposal.IsDeleted = 0 AND deal.IsDeleted = 0  ",
                            "GROUP BY member.UserId, tribe.Id, tribe.Tribe) as a",
                            "full outer join",
                            "(SELECT CAST(Sum(CAST(proposal.ProposalValue as float)*CAST(member.Percentage as float)/CAST(100 as float)) as bigint) AS ActualProposalValue, member.UserId as BId, tribe.Id as BTribeId, tribe.Tribe as BTribe",
                            "FROM dbo.CrmDealProposals as proposal ",
                            "JOIN dbo.CrmDeals AS deal ON proposal.DealId = deal.Id ",
                            "join dbo.CrmDealTribes as dt on deal.Id = dt.DealId",
                            "join dbo.CoreTribes as tribe on dt.TribeId = tribe.Id",
                            "JOIN dbo.CrmPeriods as per ON proposal.PeriodId = per.Id ",
                            "JOIN dbo.CrmDealInternalMembers AS member ON member.DealId = deal.Id ",
                            "WHERE per.Month >= " + fromMonth.ToString() + " AND per.Month <= " + toMonth.ToString() + " AND per.Year =  " + year.ToString() + " ",
                            "AND member.RoleId = 1  AND proposal.IsDeleted = 0 AND deal.IsDeleted = 0  ",
                            "GROUP BY member.UserId, tribe.Id, tribe.Tribe) as b on b.BId = a.AId and b.BTribeId = a.ATribeId",
                            "full outer join",
                            "(SELECT count(visit.Id) AS ActualSalesVisit, visitUser.Userid as cId, tribe.Id as CTribeId, tribe.Tribe as CTribe ",
                            "FROM dbo.CrmDealVisitUsers AS visitUser ",
                            "JOIN dbo.CrmDealVisits AS visit ON visitUser.VisitId = visit.Id ",
                            "join dbo.CrmDealVisitTribes as vt on vt.VisitId = visit.Id",
                            "join dbo.CoreTribes as tribe on tribe.Id = vt.TribeId",
                            "JOIN dbo.CrmPeriods as per ON visit.PeriodId = per.Id ",
                            "WHERE per.Month >= " + fromMonth.ToString() + " AND per.Month <= " + toMonth.ToString() + " AND per.Year = " + year.ToString() + " ",
                            "GROUP BY visitUser.Userid, tribe.Id, tribe.Tribe) as c on c.CId = b.BId and c.CTribeId = b.BTribeId)",
                            "as x)",
                            "as y join dbo.Users as u on y.Id = u.Id",
                            "order by y.Id, y.Tribe",
                        });

            return await _context.AchievementItemByTribes.FromSql(sql).ToListAsync();
        }

        private async Task<List<AchievementItem>> GetAchievementItems(int fromMonth, int toMonth, int year)
        {
            string sql = string.Join(" ", new[]
            {
                "select u.Id, u.FirstName as Name, ISNULL(a.TargetNProposal, 0) as TargetNProposal,",
                "ISNULL(b.TargetProposalValue, 0) as TargetProposalValue, ",
                "ISNULL(c.TargetSalesVisit, 0) as TargetSalesVisit, ISNULL(D.TargetSales, 0) as TargetSales, ",
                "x.ActualNProposal, x.ActualProposalValue, x.ActualSalesVisit, x.ActualSales,",
                "CAST(0 as float) as AchNProposal, CAST(0 as float) as AchProposalValue, CAST(0 as float) as AchSalesVisit, CAST(0 as float) as AchSales, CAST(0 as float) as AveAch",
                "from dbo.CrmRelManagers as rm join Users as u on rm.UserId=u.Id",
                "full outer join",
                "(SELECT u.Id, sum(target.Target) as TargetNProposal ",
                "FROM dbo.CrmDealTargets AS target ",
                "join dbo.CrmKpis as kpi on target.KpiId = kpi.Id",
                "JOIN dbo.CrmPeriods AS per ON target.PeriodId = per.Id ",
                "JOIN dbo.Users AS u ON target.LinkedId = u.Id ",
                "WHERE per.Month >= " + fromMonth.ToString() + " AND per.Month <= " + toMonth.ToString() + " AND per.Year = " + year.ToString() + " AND KpiId = 1 AND target.Type LIKE 'rm'",
                "group by u.Id) as a on a.Id=u.Id",
                "full outer join",
                "(SELECT u.Id, sum(target.Target) as TargetProposalValue ",
                "FROM dbo.CrmDealTargets AS target ",
                "join dbo.CrmKpis as kpi on target.KpiId = kpi.Id",
                "JOIN dbo.CrmPeriods AS per ON target.PeriodId = per.Id ",
                "JOIN dbo.Users AS u ON target.LinkedId = u.Id ",
                "WHERE per.Month >= " + fromMonth.ToString() + " AND per.Month <= " + toMonth.ToString() + " AND per.Year = " + year.ToString() + " AND KpiId = 2 AND target.Type LIKE 'rm'",
                "group by u.Id) as b on b.Id=u.Id",
                "full outer join",
                "(SELECT u.Id, sum(target.Target) as TargetSalesVisit ",
                "FROM dbo.CrmDealTargets AS target ",
                "join dbo.CrmKpis as kpi on target.KpiId = kpi.Id",
                "JOIN dbo.CrmPeriods AS per ON target.PeriodId = per.Id ",
                "JOIN dbo.Users AS u ON target.LinkedId = u.Id ",
                "WHERE per.Month >= " + fromMonth.ToString() + " AND per.Month <= " + toMonth.ToString() + " AND per.Year = " + year.ToString() + " AND KpiId = 3 AND target.Type LIKE 'rm'",
                "group by u.Id) as c on c.Id=u.Id",
                "full outer join",
                "(SELECT u.Id, sum(target.Target) as TargetSales ",
                "FROM dbo.CrmDealTargets AS target ",
                "join dbo.CrmKpis as kpi on target.KpiId = kpi.Id",
                "JOIN dbo.CrmPeriods AS per ON target.PeriodId = per.Id ",
                "JOIN dbo.Users AS u ON target.LinkedId = u.Id ",
                "WHERE per.Month >= " + fromMonth.ToString() + " AND per.Month <= " + toMonth.ToString() + " AND per.Year = " + year.ToString() + " AND KpiId = 4 AND target.Type LIKE 'rm'",
                "group by u.Id) as d on d.Id=u.Id",
                "join (",
                "select u.Id, u.FirstName, ISNULL(a.ActualNProposal, 0) as ActualNProposal,",
                "ISNULL(b.ActualProposalValue, 0) as ActualProposalValue, ",
                "ISNULL(c.ActualSalesVisit, 0) as ActualSalesVisit, ISNULL(d.ActualSales, 0) as ActualSales",
                "from dbo.CrmRelManagers as rm join Users as u on rm.UserId=u.Id",
                "full outer join",
                "(SELECT Count(Distinct(proposal.Id)) AS ActualNProposal, member.UserId as Id",



                "FROM dbo.CrmDealProposals as proposal ",
                "JOIN dbo.CrmDeals AS deal ON proposal.DealId = deal.Id ",
                "join dbo.CrmDealTribes as dt on deal.Id = dt.DealId",
                "join dbo.CoreTribes as tribe on dt.TribeId = tribe.Id",
                "JOIN dbo.CrmPeriods as per ON proposal.PeriodId = per.Id ",
                "JOIN dbo.CrmDealInternalMembers AS member ON member.DealId = deal.Id ",
                "WHERE per.Month >= " + fromMonth.ToString() + " AND per.Month <= " + toMonth.ToString() + " AND per.Year =  " + year.ToString() + " ",
                "AND member.RoleId = 1  AND proposal.IsDeleted = 0 AND deal.IsDeleted = 0  ",
                "GROUP BY member.UserId) as a on a.Id=u.Id",
                "full outer join",
                "(SELECT CAST(Sum(CAST(proposal.ProposalValue as float)*CAST(member.Percentage as float)/CAST(100 as float)) as bigint) AS ActualProposalValue, member.UserId as Id",
                "FROM dbo.CrmDealProposals as proposal ",
                "JOIN dbo.CrmDeals AS deal ON proposal.DealId = deal.Id ",
                "join dbo.CrmDealTribes as dt on deal.Id = dt.DealId",
                "join dbo.CoreTribes as tribe on dt.TribeId = tribe.Id",
                "JOIN dbo.CrmPeriods as per ON proposal.PeriodId = per.Id ",
                "JOIN dbo.CrmDealInternalMembers AS member ON member.DealId = deal.Id ",
                "WHERE per.Month >= " + fromMonth.ToString() + " AND per.Month <= " + toMonth.ToString() + " AND per.Year =  2021 ",
                "AND member.RoleId = 1  AND proposal.IsDeleted = 0 AND deal.IsDeleted = 0  ",
                "GROUP BY member.UserId) as b on b.Id=u.Id",
                "full outer join",
                "(SELECT count(visit.Id) AS ActualSalesVisit, visitUser.Userid as Id ",
                "FROM dbo.CrmDealVisitUsers AS visitUser ",
                "JOIN dbo.CrmDealVisits AS visit ON visitUser.VisitId = visit.Id ",
                "JOIN dbo.CrmPeriods as per ON visit.PeriodId = per.Id ",
                "WHERE per.Month >= " + fromMonth.ToString() + " AND per.Month <= " + toMonth.ToString() + " AND per.Year = " + year.ToString() + " ",
                "GROUP BY visitUser.Userid) as c on c.Id=u.Id",
                "full outer join",
                "(SELECT sum(userInvoice.Amount) AS ActualSales, userInvoice.UserId as Id ",
                "FROM dbo.CrmDealUserInvoices AS userInvoice ",
                "JOIN dbo.CrmDealInvoices AS invoice ON userInvoice.InvoiceId = invoice.Id ",
                "JOIN dbo.CrmPeriods as per ON invoice.PeriodId = per.Id ",
                "WHERE per.Month >= " + fromMonth.ToString() + " AND per.Month <= " + toMonth.ToString() + " AND per.Year = " + year.ToString() + " AND invoice.IsDeleted=0  ",
                "GROUP BY userInvoice.UserId) as d on d.Id=u.Id",
                ") as x on x.Id=u.Id",
                "where u.Id is not null",
            });

            List<AchievementItem> items = await _context.AchievementItems.FromSql(sql).ToListAsync();

            foreach (AchievementItem item in items)
            {
                if (item.TargetNProposal != 0) item.AchNProposal = Convert.ToDouble(item.TargetNProposal) / Convert.ToDouble(item.ActualNProposal) * 100.0d;
                if (item.TargetProposalValue != 0) item.AchProposalValue = Convert.ToDouble(item.TargetProposalValue) / Convert.ToDouble(item.ActualProposalValue) * 100.0d;
                if (item.TargetSalesVisit != 0) item.AchSalesVisit = Convert.ToDouble(item.TargetSalesVisit) / Convert.ToDouble(item.ActualSalesVisit) * 100.0d;
                if (item.TargetSales != 0) item.AchSales = Convert.ToDouble(item.TargetSales) / Convert.ToDouble(item.ActualSales) * 100.0d;
                item.AveAch = (item.AchNProposal + item.AchProposalValue + item.AchSalesVisit + item.AchSales) / 4;
            }
            return items;
        }
        private async Task<List<ChartSeriesItem>> GetSalesSeries(string type, int id, GenericInfo kpi, int fromMonth, int toMonth, int year)
        {
            string targetSql = GetTargetSql(type, id, kpi, fromMonth, toMonth, year);
            string actualSql = GetActualSalesSql(type, id, fromMonth, toMonth, year);

            if (targetSql == null || actualSql == null)
            {
                return null;
            }
            string sql = string.Join(" ", new[]
            {
                "SELECT ISNULL(a.Id, b.Id) AS Id, CAST(ISNULL(a.Target, 0) as bigint) AS Target, CAST(ISNULL(b.Actual, 0) AS bigint) AS Actual, ISNULL(a.Month, b.Month) AS Month,",
                "CASE",
                "WHEN Target IS NULL THEN 0",
                "WHEN Target = 0 THEN 0",
                "ELSE ISNULL(CAST((Actual * 100)/Target as bigint), 0) END AS Achievement FROM (",
                targetSql,
                ") AS a FULL OUTER JOIN (",
                actualSql,
                ") as b on a.Month = b.Month ORDER BY Month"
            });

            return await _context.ChartSeriesItems.FromSql(sql).ToListAsync();

        }
        private string GetActualSalesSql(string type, int id, int fromMonth, int toMonth, int year)
        {
            if (type.Equals("tribe"))
            {
                string whereId = "";                        // For all tribes, when id == 0
                if (id != 0) whereId = "AND tribeInvoice.TribeId =" + id.ToString();

                return string.Join(" ", new[]
                {
                    "SELECT sum(tribeInvoice.Amount) AS Actual, tribeInvoice.TribeId as Id, per.Month",
                    "FROM dbo.CrmDealTribeInvoices AS tribeInvoice",
                    "JOIN dbo.CrmDealInvoices AS invoice ON tribeInvoice.InvoiceId = invoice.Id",
                    "JOIN dbo.CrmPeriods as per ON invoice.PeriodId = per.Id",
                    "WHERE per.Month >=", fromMonth.ToString(), "AND per.Month <=", toMonth.ToString(), "AND per.Year =", year.ToString(), whereId,
                    " AND invoice.IsDeleted=0 ",
                    "GROUP BY tribeInvoice.TribeId, per.Month"
                });
            }
            else if (type.Equals("branch"))
            {
                return string.Join(" ", new[]
                {
                    "SELECT sum(invoice.Amount) AS Actual, deal.BranchId as Id, per.Month",
                    "FROM dbo.CrmDealInvoices AS invoice",
                    "JOIN dbo.CrmPeriods as per ON invoice.PeriodId = per.Id",
                    "JOIN dbo.CrmDeals AS deal ON invoice.DealId = deal.Id",
                    "WHERE per.Month >=", fromMonth.ToString(), "AND per.Month <=", toMonth.ToString(), "AND per.Year =", year.ToString(), "AND deal.BranchId =", id.ToString(), "AND invoice.IsToBe = 0",
                    " AND invoice.IsDeleted=0 ",
                    "GROUP BY deal.BranchId, per.Month"
                });
            }
            else if (type.Equals("segment"))
            {
                return string.Join(" ", new[]
                {
                    "SELECT sum(invoice.Amount) AS Actual, deal.SegmentId as Id, per.Month",
                    "FROM dbo.CrmDealInvoices AS invoice",
                    "JOIN dbo.CrmPeriods as per ON invoice.PeriodId = per.Id",
                    "JOIN dbo.CrmDeals AS deal ON invoice.DealId = deal.Id",
                    "WHERE per.Month >=", fromMonth.ToString(), "AND per.Month <=", toMonth.ToString(), "AND per.Year =", year.ToString(), "AND deal.SegmentId =", id.ToString(), "AND invoice.IsToBe = 0",
                    " AND invoice.IsDeleted=0 ",
                    "GROUP BY deal.SegmentId, per.Month"
                });
            }
            else if (type.Equals("rm"))
            {
                CrmDealRole role = GetDealRole("rm");
                return string.Join(" ", new[]
                {
                    "SELECT sum(userInvoice.Amount) AS Actual, userInvoice.UserId as Id, per.Month",
                    "FROM dbo.CrmDealUserInvoices AS userInvoice",
                    "JOIN dbo.CrmDealInvoices AS invoice ON userInvoice.InvoiceId = invoice.Id",
                    "JOIN dbo.CrmPeriods as per ON invoice.PeriodId = per.Id",
                    "WHERE per.Month >=", fromMonth.ToString(), "AND per.Month <=", toMonth.ToString(), "AND per.Year =", year.ToString(), "AND userInvoice.UserId =", id.ToString(),
                    " AND invoice.IsDeleted=0 ",
                    "GROUP BY userInvoice.UserId, per.Month"
                });
            }

            return null;

        }
        private string GetActualNSalesSql(string type, int id, int fromMonth, int toMonth, int year)
        {
            if (type.Equals("tribe"))
            {
                string whereId = "";                        // For all tribes, when id == 0
                if (id != 0) whereId = "AND dealTribe.TribeId =" + id.ToString();

                return string.Join(" ", new[]
                {
                    "SELECT count(visit.Id) AS Actual, dealTribe.TribeId as Id, per.Month",
                    "FROM dbo.CrmDealVisitUsers AS visitUser JOIN dbo.CrmDealVisits AS visit ON visitUser.VisitId = visit.Id",
                    "JOIN dbo.CrmDeals AS deal ON visit.DealId = deal.Id",
                    "JOIN dbo.CrmDealTribes AS dealTribe ON dealTribe.DealId = deal.Id",
                    "JOIN dbo.CrmPeriods as per ON visit.PeriodId = per.Id",
                    "WHERE per.Month >=", fromMonth.ToString(), "AND per.Month <=", toMonth.ToString(), "AND per.Year = ", year.ToString(), whereId,
                    "GROUP BY dealTribe.TribeId, per.Month"
                });
            }
            else if (type.Equals("branch"))
            {
                return string.Join(" ", new[]
                {
                    "SELECT count(visit.Id) AS Actual, deal.BranchId as Id, per.Month",
                    "FROM dbo.CrmDealVisitUsers AS visitUser JOIN dbo.CrmDealVisits AS visit ON visitUser.VisitId = visit.Id",
                    "JOIN dbo.CrmDeals AS deal ON visit.DealId = deal.Id",
                    "JOIN dbo.CrmPeriods as per ON visit.PeriodId = per.Id",
                    "WHERE per.Month >=", fromMonth.ToString(), "AND per.Month <=", toMonth.ToString(), "AND per.Year = ", year.ToString(), "AND deal.BranchId =", id.ToString(),
                    "GROUP BY deal.BranchId, per.Month"
                });
            }
            else if (type.Equals("segment"))
            {
                return string.Join(" ", new[]
                {
                    "SELECT count(visit.Id) AS Actual, deal.SegmentId as Id, per.Month",
                    "FROM dbo.CrmDealVisitUsers AS visitUser JOIN dbo.CrmDealVisits AS visit ON visitUser.VisitId = visit.Id",
                    "JOIN dbo.CrmDeals AS deal ON visit.DealId = deal.Id",
                    "JOIN dbo.CrmPeriods as per ON visit.PeriodId = per.Id",
                    "WHERE per.Month >=", fromMonth.ToString(), "AND per.Month <=", toMonth.ToString(), "AND per.Year = ", year.ToString(), "AND deal.SegmentId =", id.ToString(),
                    "GROUP BY deal.SegmentId, per.Month"
                });
            }
            else if (type.Equals("rm"))
            {
                CrmDealRole role = GetDealRole("rm");
                return string.Join(" ", new[]
                {
                    "SELECT count(visit.Id) AS Actual, visitUser.Userid as Id, per.Month",
                    "FROM dbo.CrmDealVisitUsers AS visitUser JOIN dbo.CrmDealVisits AS visit ON visitUser.VisitId = visit.Id",
                    "JOIN dbo.CrmPeriods as per ON visit.PeriodId = per.Id",
                    "WHERE per.Month >=", fromMonth.ToString(), "AND per.Month <=", toMonth.ToString(),
                    "AND per.Year =", year.ToString(), "AND visitUser.Userid =", id.ToString(), "GROUP BY visitUser.Userid, per.Month"
                });
            }

            return null;

        }
        private string GetActualProposalValueSql(string type, int id, int fromMonth, int toMonth, int year)
        {
            if (type.Equals("tribe"))
            {
                string whereId = "";                        // For all tribes, when id == 0
                if (id != 0) whereId = "AND dealTribe.TribeId =" + id.ToString();

                return string.Join(" ", new[]
                {
                    "SELECT sum(proposal.ProposalValue) AS Actual, dealTribe.TribeId as Id, per.Month",
                    "FROM dbo.CrmDealProposals as proposal",
                    "JOIN dbo.CrmDeals AS deal",
                    "ON proposal.DealId = deal.Id",
                    "JOIN dbo.CrmPeriods as per",
                    "ON proposal.PeriodId = per.Id",
                    "JOIN dbo.CrmDealTribes AS dealTribe",
                    "ON dealTribe.DealId = deal.Id",
                    "WHERE per.Month >=", fromMonth.ToString(), "AND per.Month <=", toMonth.ToString(), "AND per.Year = ", year.ToString(), whereId,
                    "GROUP BY dealTribe.TribeId, per.Month"
                }); ;
            }
            else if (type.Equals("branch"))
            {
                return string.Join(" ", new[]
                {
                    "SELECT sum(proposal.ProposalValue) AS Actual, deal.BranchId as Id, per.Month",
                    "FROM dbo.CrmDealProposals as proposal",
                    "JOIN dbo.CrmDeals AS deal",
                    "ON proposal.DealId = deal.Id",
                    "JOIN dbo.CrmPeriods as per",
                    "ON proposal.PeriodId = per.Id",
                    "WHERE per.Month >=", fromMonth.ToString(), "AND per.Month <=", toMonth.ToString(), "AND per.Year = ", year.ToString(), "AND deal.BranchId =", id.ToString(),
                    "GROUP BY deal.BranchId, per.Month"
                });
            }
            else if (type.Equals("segment"))
            {
                return string.Join(" ", new[]
                {
                    "SELECT sum(proposal.ProposalValue) AS Actual, deal.SegmentId as Id, per.Month",
                    "FROM dbo.CrmDealProposals as proposal",
                    "JOIN dbo.CrmDeals AS deal",
                    "ON proposal.DealId = deal.Id",
                    "JOIN dbo.CrmPeriods as per",
                    "ON proposal.PeriodId = per.Id",
                    "WHERE per.Month >=", fromMonth.ToString(), "AND per.Month <=", toMonth.ToString(), "AND per.Year = ", year.ToString(), "AND deal.SegmentId =", id.ToString(),
                    "GROUP BY deal.SegmentId, per.Month"
                });
            }
            else if (type.Equals("rm"))
            {
                CrmDealRole role = GetDealRole("rm");
                return string.Join(" ", new[]
                {
                    "SELECT sum(proposal.ProposalValue) AS Actual, member.UserId as Id, per.Month",
                    "FROM dbo.CrmDealProposals as proposal",
                    "JOIN dbo.CrmDeals AS deal",
                    "ON proposal.DealId = deal.Id",
                    "JOIN dbo.CrmPeriods as per",
                    "ON proposal.PeriodId = per.Id",
                    "JOIN dbo.CrmDealInternalMembers AS member",
                    "ON member.DealId = deal.Id",
                    "WHERE per.Month >=", fromMonth.ToString(), "AND per.Month <=", toMonth.ToString(), "AND per.Year = ", year.ToString(), "AND member.UserId =", id.ToString(), "AND member.RoleId =", role.Id.ToString(),
                    "GROUP BY member.UserId, per.Month"
                });
            }

            return null;

        }
        private string GetActualNProposalSql(string type, int id, int fromMonth, int toMonth, int year)
        {
            if (type.Equals("tribe"))
            {
                string whereId = "";            /// for all tribes, when id == 0
                if (id != 0) whereId = "AND dealTribe.TribeId =" + id.ToString();

                return string.Join(" ", new[]
                {
                    "SELECT Count(Distinct(proposal.Id)) AS Actual, dealTribe.TribeId as Id, per.Month",
                    "FROM dbo.CrmDealProposals as proposal",
                    "JOIN dbo.CrmDeals AS deal",
                    "ON proposal.DealId = deal.Id",
                    "JOIN dbo.CrmPeriods as per",
                    "ON proposal.PeriodId = per.Id",
                    "JOIN dbo.CrmDealTribes AS dealTribe",
                    "ON dealTribe.DealId = deal.Id",
                    "WHERE per.Month >=", fromMonth.ToString(), "AND per.Month <=", toMonth.ToString(), "AND per.Year = ", year.ToString(), whereId,
                    " AND proposal.IsDeleted = 0 AND deal.IsDeleted = 0 ",
                    "GROUP BY dealTribe.TribeId, per.Month"
                });
            }
            else if (type.Equals("branch"))
            {
                return string.Join(" ", new[]
                {
                    "SELECT Count(Distinct(proposal.Id)) AS Actual, deal.BranchId as Id, per.Month",
                    "FROM dbo.CrmDealProposals as proposal",
                    "JOIN dbo.CrmDeals AS deal",
                    "ON proposal.DealId = deal.Id",
                    "JOIN dbo.CrmPeriods as per",
                    "ON proposal.PeriodId = per.Id",
                    "WHERE per.Month >=", fromMonth.ToString(), "AND per.Month <=", toMonth.ToString(), "AND per.Year = ", year.ToString(), "AND deal.BranchId =", id.ToString(),
                    " AND proposal.IsDeleted = 0 AND deal.IsDeleted = 0 ",
                    "GROUP BY deal.BranchId, per.Month"
                });
            }
            else if (type.Equals("segment"))
            {
                return string.Join(" ", new[]
                {
                    "SELECT Count(Distinct(proposal.Id)) AS Actual, deal.SegmentId as Id, per.Month",
                    "FROM dbo.CrmDealProposals as proposal",
                    "JOIN dbo.CrmDeals AS deal",
                    "ON proposal.DealId = deal.Id",
                    "JOIN dbo.CrmPeriods as per",
                    "ON proposal.PeriodId = per.Id",
                    "WHERE per.Month >=", fromMonth.ToString(), "AND per.Month <=", toMonth.ToString(), "AND per.Year = ", year.ToString(), "AND deal.SegmentId =", id.ToString(),
                    " AND proposal.IsDeleted = 0 AND deal.IsDeleted = 0 ",
                    "GROUP BY deal.SegmentId, per.Month"
                });
            }
            else if (type.Equals("rm"))
            {
                CrmDealRole role = GetDealRole("rm");
                return string.Join(" ", new[]
                {
                    "SELECT Count(Distinct(proposal.Id)) AS Actual, member.UserId as Id, per.Month",
                    "FROM dbo.CrmDealProposals as proposal",
                    "JOIN dbo.CrmDeals AS deal",
                    "ON proposal.DealId = deal.Id",
                    "JOIN dbo.CrmPeriods as per",
                    "ON proposal.PeriodId = per.Id",
                    "JOIN dbo.CrmDealInternalMembers AS member",
                    "ON member.DealId = deal.Id",
                    "WHERE per.Month >=", fromMonth.ToString(), "AND per.Month <=", toMonth.ToString(), "AND per.Year = ", year.ToString(), "AND member.UserId =", id.ToString(), "AND member.RoleId =", role.Id.ToString(),
                    " AND proposal.IsDeleted = 0 AND deal.IsDeleted = 0 ",
                    "GROUP BY member.UserId, per.Month"
                });
            }

            return null;
        }
        private string GetTargetSql(string type, int id, GenericInfo kpi, int fromMonth, int toMonth, int year)
        {
            string table = "";
            string whereId = "";

            if (id == 0)
            {
                // All.
                // Target dihitung berdasarkan total target RM
                table = "dbo.Users";
                whereId = "";
            }
            if (type.Trim().Equals("tribe"))
            {
                table = "dbo.CoreTribes";
                whereId = "AND u.Id = " + id.ToString();
            }
            else if (type.Trim().Equals("branch"))
            {
                table = "dbo.CrmBranches";
                whereId = "AND u.Id = " + id.ToString();
            }
            else if (type.Trim().Equals("segment"))
            {
                table = "dbo.CrmSegments";
                whereId = "AND u.Id = " + id.ToString();
            }
            else if (type.Trim().Equals("rm"))
            {
                table = "dbo.Users";
                whereId = "AND u.Id = " + id.ToString();
            }

            if (table.Length > 0)
            {
                return string.Join(" ", new[]
                    {
                    "SELECT u.Id, target.Target, per.Month",
                    "FROM dbo.CrmDealTargets AS target",
                    "JOIN dbo.CrmPeriods AS per",
                    "ON target.PeriodId = per.Id",
                    "JOIN", table, "AS u",
                    "ON target.LinkedId = u.Id",
                    "WHERE per.Month >=", fromMonth.ToString(), "AND per.Month <=", toMonth.ToString(), "AND per.Year =", year.ToString(),
                    whereId,
                    "AND KpiId =", kpi.Id.ToString(), "AND target.Type LIKE", ("'" + type + "'")
                });
            }

            return null;
        }
        private string GetRmInvoiceSql(string field, int year, int fromMonth, int toMonth)
        {
            return string.Join(" ", new[]
            {
                "select ISNULL(sum(invoice.Amount * deal.Percentage / 100), 0) as ", field, ", u.Id, u.FirstName",
                "FROM dbo.CrmDealInvoices AS invoice",
                "JOIN dbo.CrmPeriods AS per ON invoice.PeriodId = per.Id",
                "JOIN dbo.CrmDealUserInvoices AS deal ON invoice.Id = deal.InvoiceId",
                "JOIN dbo.Users AS u ON deal.UserId = u.ID",
                "WHERE invoice.IsDeleted = 0 AND invoice.IsToBe = 0 AND per.Month >= ", fromMonth.ToString(), "AND per.Month <= ", toMonth.ToString(), "AND per.Year = ", year.ToString(),
                "GROUP BY deal.Amount, u.Id, u.FirstName, deal.Percentage"
            });
        }
        private string GetBranchInvoiceSql(string field, int year, int fromMonth, int toMonth)
        {
            return string.Join(" ", new[]
            {
                "select ISNULL(sum(invoice.Amount), 0) as ", field, ", branch.Id, branch.Branch FROM dbo.CrmDealInvoices AS invoice",
                "JOIN dbo.CrmPeriods AS per ON invoice.PeriodId = per.Id",
                "JOIN dbo.CrmDeals AS deal ON invoice.DealId = deal.Id",
                "JOIN dbo.CrmBranches AS branch ON deal.BranchId = branch.Id",
                "WHERE invoice.IsDeleted = 0 AND invoice.IsToBe = 0 AND per.Month >= ", fromMonth.ToString(), "AND per.Month <= ", toMonth.ToString(), "AND per.Year = ", year.ToString(),
                "GROUP BY branch.Id, branch.Branch"
            });
        }
        private string GetSegmentInvoiceSql(string field, int year, int fromMonth, int toMonth)
        {
            return string.Join(" ", new[]
            {
                "select ISNULL(sum(invoice.Amount), 0) as ", field, ", segment.Id, segment.Segment FROM dbo.CrmDealInvoices AS invoice",
                "JOIN dbo.CrmPeriods AS per ON invoice.PeriodId = per.Id",
                "JOIN dbo.CrmDeals AS deal ON invoice.DealId = deal.Id",
                "JOIN dbo.CrmSegments AS segment ON deal.SegmentId = segment.Id",
                "WHERE invoice.IsDeleted = 0 AND invoice.IsToBe = 0 AND per.Month >= ", fromMonth.ToString(), "AND per.Month <= ", toMonth.ToString(), "AND per.Year = ", year.ToString(),
                "GROUP BY segment.Id, segment.Segment"
            });
        }
        private string GetTribeInvoiceSql(string field, int year, int fromMonth, int toMonth)
        {
            if (year >= 2020)
            {
                /*
                return string.Join(" ", new[] {
                    "select ISNULL(sum(tribeInvoice.Amount), 0) as ", field, ", tribe.Id, tribe.Tribe",
                    "FROM dbo.CrmDealTribeInvoices AS tribeInvoice",
                    "JOIN dbo.CrmDealInvoices AS invoice ON invoice.Id = tribeInvoice.InvoiceId",
                    "JOIN dbo.CrmPeriods AS per ON invoice.PeriodId = per.Id",
                    "JOIN dbo.CrmDeals AS deal ON invoice.DealId = deal.Id",
                    "JOIN dbo.CrmDealTribes AS dealtribe ON deal.Id = dealtribe.DealId",
                    "JOIN dbo.CoreTribes AS tribe ON tribeInvoice.TribeId = tribe.Id",
                    "WHERE invoice.IsDeleted = 0 AND invoice.IsToBe = 0 AND per.Month >= ", fromMonth.ToString(), "AND per.Month <= ", toMonth.ToString(), "AND per.Year = ", year.ToString(),
                    "GROUP BY tribe.Id, tribe.Tribe"
                });
                */

                /*
                // salah karena ada deal yang sudah dihapus tetap dihitung juga
                // Ini versi yang baru 2020_08_05
                return string.Join(" ", new[] {
                    "select ISNULL(sum(tribeInvoice.Amount), 0) as ", field, ", tribe.Id, tribe.Tribe",
                    "FROM dbo.CrmDealTribeInvoices AS tribeInvoice",
                    "JOIN dbo.CrmDealInvoices AS invoice ON invoice.Id = tribeInvoice.InvoiceId",
                    "JOIN dbo.CoreTribes AS tribe ON tribeInvoice.TribeId = tribe.Id",
                    "JOIN dbo.CrmPeriods AS per ON invoice.PeriodId = per.Id",
                    "WHERE invoice.IsDeleted = 0 AND invoice.IsToBe = 0 AND per.Month >= ", fromMonth.ToString(), "AND per.Month <= ", toMonth.ToString(), "AND per.Year = ", year.ToString(),
                    "GROUP BY tribe.Id, tribe.Tribe"
                });
                */

                // Ini versi yang baru 2020_10_14
                return string.Join(" ", new[] {
                    "select ISNULL(sum(tribeInvoice.Amount), 0) as ", field, ", tribe.Id, tribe.Tribe",
                    "FROM dbo.CrmDealTribeInvoices AS tribeInvoice",
                    "JOIN dbo.CrmDealInvoices AS invoice ON invoice.Id = tribeInvoice.InvoiceId",
                    "JOIN dbo.CrmDeals as deal ON invoice.DealId = deal.Id",
                    "JOIN dbo.CoreTribes AS tribe ON tribeInvoice.TribeId = tribe.Id",
                    "JOIN dbo.CrmPeriods AS per ON invoice.PeriodId = per.Id",
                    "WHERE invoice.IsDeleted = 0 AND deal.IsDeleted = 0 AND invoice.IsToBe = 0 AND per.Month >= ", fromMonth.ToString(), "AND per.Month <= ", toMonth.ToString(), "AND per.Year = ", year.ToString(),
                    "GROUP BY tribe.Id, tribe.Tribe"
                });

                /*
                // Ini versi 2020_09_16. Salah karena tidak memperhitungkan satu invoice bisa dibagi ke beberapa tribe
                return string.Join(" ", new[] {
                    "select ISNULL(sum(invoice.Amount), 0) as ", field, ", tribe.Id, tribe.Tribe",
                    "FROM dbo.CrmDealInvoices AS invoice",
                    "JOIN dbo.CrmPeriods AS per ON invoice.PeriodId = per.Id",
                    "JOIN dbo.CrmDeals AS deal ON invoice.DealId = deal.Id",
                    "JOIN dbo.CrmDealTribes AS dealTribe ON deal.Id = dealTribe.DealId",
                    "JOIN dbo.CoreTribes AS tribe ON dealTribe.TribeId = tribe.Id",
                    "WHERE invoice.IsDeleted = 0 AND invoice.IsToBe = 0 AND per.Month >= ", fromMonth.ToString(), "AND per.Month <= ", toMonth.ToString(), "AND per.Year = ", year.ToString(),
                    "GROUP BY tribe.Id, tribe.Tribe"
                });
                */
            }
            return string.Join(" ", new[]
            {
                "select ISNULL(sum(history.Amount), 0) as Amount, tribe.Id, tribe.Tribe",
                "FROM dbo.CrmTribeSalesHistories AS history",
                "JOIN dbo.CrmPeriods AS per ON history.PeriodId = per.Id",
                "JOIN dbo.CoreTribes AS tribe ON history.TribeId = tribe.Id",
                "WHERE per.Month >= ", fromMonth.ToString(), "AND per.Month <= ", toMonth.ToString(), "AND per.Year = ", year.ToString(),
                "GROUP BY tribe.Id, tribe.Tribe"
            });
        }
        private async Task<List<SummaryReportRow>> GetTribeProjectedInvoice(string compareString, int month, int year)
        {
            /*
            string sql = string.Join(" ", new[] {
                "select ISNULL(a.Id, b.Id) as Id, ISNULL(a.Tribe, b.Tribe) as Text, ISNULL(a.Amount1, 0) as Amount1, ISNULL(b.Amount2, 0) As Amount2, ISNULL(Amount1, 0) + ISNULL(Amount2, 0) AS Amount3",
                "FROM(",
                "select ISNULL(sum(invoice.Amount), 0) as Amount1, tribe.Id, tribe.Tribe FROM dbo.CrmDealInvoices AS invoice",
                "JOIN dbo.CrmPeriods AS per ON invoice.PeriodId = per.Id",
                "JOIN dbo.CrmDeals AS deal ON invoice.DealId = deal.Id",
                "JOIN dbo.CrmDealTribes AS dealtribe ON deal.Id = dealtribe.DealId",
                "JOIN dbo.CoreTribes AS tribe ON dealtribe.TribeId = tribe.Id",
                "WHERE invoice.IsDeleted = 0 AND invoice.IsToBe = 1 AND per.Month", compareString, month.ToString(), "AND per.Year = ", year.ToString(),
                "GROUP BY tribe.Id, tribe.Tribe",
                ") as a",
                "FULL OUTER JOIN(",
                "select ISNULL(CAST(SUM(invoice.Amount * dealTribe.Percentage / 100) AS bigint), 0) as Amount2, tribe.Id, tribe.Tribe",
                "FROM dbo.CrmDealInvoices AS invoice",
                "JOIN dbo.CrmPeriods AS per ON invoice.PeriodId = per.Id",
                "JOIN dbo.CrmDeals AS deal ON invoice.DealId = deal.Id",
                "join dbo.CrmDealTribes as dealTribe on deal.Id = dealTribe.Id",
                "JOIN dbo.CoreTribes AS tribe ON dealTribe.TribeId = tribe.Id",
                "WHERE invoice.IsDeleted = 0 AND invoice.IsToBe = 0 AND per.Month", compareString, month.ToString(), "AND per.Year = ", year.ToString(),
                "GROUP BY tribe.Id, tribe.Tribe",
                ") as b ON b.Id = a.Id"
            });
            */

            /*
            // Salah karena tidak memperhitungkan deal yang deleted
            // Ini versi yang baru 2020_08_05
            string sql = string.Join(" ", new[] {
                "select ISNULL(a.Id, b.Id) as Id, ISNULL(a.Tribe, b.Tribe) as Text, ISNULL(a.Amount1, 0) as Amount1, ISNULL(b.Amount2, 0) As Amount2, ISNULL(Amount1, 0) + ISNULL(Amount2, 0) AS Amount3",
                "FROM(",
                "select ISNULL(sum(invoice.Amount), 0) as Amount1, tribe.Id, tribe.Tribe FROM dbo.CrmDealInvoices AS invoice",
                "JOIN dbo.CrmPeriods AS per ON invoice.PeriodId = per.Id",
                "JOIN dbo.CrmDeals AS deal ON invoice.DealId = deal.Id",
                "JOIN dbo.CrmDealTribes AS dealtribe ON deal.Id = dealtribe.DealId",
                "JOIN dbo.CoreTribes AS tribe ON dealtribe.TribeId = tribe.Id",
                "WHERE invoice.IsDeleted = 0 AND invoice.IsToBe = 1 AND per.Month", compareString, month.ToString(), "AND per.Year = ", year.ToString(),
                "GROUP BY tribe.Id, tribe.Tribe",
                ") as a",
                "FULL OUTER JOIN(",
                "select ISNULL(sum(tribeInvoice.Amount), 0) as Amount2, tribe.Id, tribe.Tribe",
                "FROM dbo.CrmDealTribeInvoices AS tribeInvoice",
                "JOIN dbo.CrmDealInvoices AS invoice ON invoice.Id = tribeInvoice.InvoiceId",
                "JOIN dbo.CoreTribes AS tribe ON tribeInvoice.TribeId = tribe.Id",
                "JOIN dbo.CrmPeriods AS per ON invoice.PeriodId = per.Id",
                "WHERE invoice.IsDeleted = 0 AND invoice.IsToBe = 0 AND per.Month", compareString, month.ToString(), "AND per.Year = ", year.ToString(),
                "GROUP BY tribe.Id, tribe.Tribe",
                ") as b ON b.Id = a.Id"
            });
            */

            // Ini versi yang baru 2020_10_14
            string sql = string.Join(" ", new[] {
                "select ISNULL(a.Id, b.Id) as Id, ISNULL(a.Tribe, b.Tribe) as Text, ISNULL(a.Amount1, 0) as Amount1, ISNULL(b.Amount2, 0) As Amount2, ISNULL(Amount1, 0) + ISNULL(Amount2, 0) AS Amount3",
                "FROM(",
                "select ISNULL(sum(invoice.Amount), 0) as Amount1, tribe.Id, tribe.Tribe FROM dbo.CrmDealInvoices AS invoice",
                "JOIN dbo.CrmPeriods AS per ON invoice.PeriodId = per.Id",
                "JOIN dbo.CrmDeals AS deal ON invoice.DealId = deal.Id",
                "JOIN dbo.CrmDealTribes AS dealtribe ON deal.Id = dealtribe.DealId",
                "JOIN dbo.CoreTribes AS tribe ON dealtribe.TribeId = tribe.Id",
                "WHERE invoice.IsDeleted = 0 AND deal.IsDeleted = 0 AND invoice.IsToBe = 1 AND per.Month", compareString, month.ToString(), "AND per.Year = ", year.ToString(),
                "GROUP BY tribe.Id, tribe.Tribe",
                ") as a",
                "FULL OUTER JOIN(",
                "select ISNULL(sum(tribeInvoice.Amount), 0) as Amount2, tribe.Id, tribe.Tribe",
                "FROM dbo.CrmDealTribeInvoices AS tribeInvoice",
                "JOIN dbo.CrmDealInvoices AS invoice ON invoice.Id = tribeInvoice.InvoiceId",
                "JOIN dbo.CrmDeals as deal ON invoice.DealId = deal.Id",
                "JOIN dbo.CoreTribes AS tribe ON tribeInvoice.TribeId = tribe.Id",
                "JOIN dbo.CrmPeriods AS per ON invoice.PeriodId = per.Id",
                "WHERE invoice.IsDeleted = 0 AND deal.IsDeleted = 0 AND invoice.IsToBe = 0 AND per.Month", compareString, month.ToString(), "AND per.Year = ", year.ToString(),
                "GROUP BY tribe.Id, tribe.Tribe",
                ") as b ON b.Id = a.Id"
            });
            return await _context.SummaryReportRows.FromSql(sql).ToListAsync();
        }
        private async Task<List<SummaryReportRow>> GetBranchProjectedInvoice(string compareString, int month, int year)
        {
            string sql = string.Join(" ", new[] {
                "select ISNULL(a.Id, b.Id) as Id, ISNULL(a.Branch, b.Branch) as Text, ISNULL(a.Amount1, 0) as Amount1, ISNULL(b.Amount2, 0) As Amount2, ISNULL(Amount1, 0) + ISNULL(Amount2, 0) AS Amount3",
                "FROM(",
                "select ISNULL(sum(invoice.Amount), 0) as Amount1, branch.Id, branch.Branch FROM dbo.CrmDealInvoices AS invoice",
                "JOIN dbo.CrmPeriods AS per ON invoice.PeriodId = per.Id",
                "JOIN dbo.CrmDeals AS deal ON invoice.DealId = deal.Id",
                "JOIN dbo.CrmBranches AS branch ON deal.BranchId = branch.Id",
                "WHERE deal.IsDeleted = 0 AND invoice.IsDeleted = 0 AND invoice.IsToBe = 1 AND per.Month", compareString, month.ToString(), "AND per.Year = ", year.ToString(),
                "GROUP BY branch.Id, branch.Branch",
                ") as a",
                "FULL OUTER JOIN(",
                "select ISNULL(sum(invoice.Amount), 0) as Amount2, branch.Id, branch.Branch FROM dbo.CrmDealInvoices AS invoice",
                "JOIN dbo.CrmPeriods AS per ON invoice.PeriodId = per.Id",
                "JOIN dbo.CrmDeals AS deal ON invoice.DealId = deal.Id",
                "JOIN dbo.CrmBranches AS branch ON deal.BranchId = branch.Id",
                "WHERE deal.IsDeleted = 0 AND invoice.IsDeleted = 0 AND invoice.IsToBe = 0 AND per.Month", compareString, month.ToString(), "AND per.Year = ", year.ToString(),
                "GROUP BY branch.Id, branch.Branch",
                ") as b ON b.Id = a.Id"
            });


            return await _context.SummaryReportRows.FromSql(sql).ToListAsync();
        }
        private async Task<List<SummaryReportRow>> GetSegmentProjectedInvoice(string compareString, int month, int year)
        {
            string sql = string.Join(" ", new[] {
                "select ISNULL(a.Id, b.Id) as Id, ISNULL(a.Segment, b.Segment) as Text, ISNULL(a.Amount1, 0) as Amount1, ISNULL(b.Amount2, 0) As Amount2, ISNULL(Amount1, 0) + ISNULL(Amount2, 0) AS Amount3",
                "FROM(",
                "select ISNULL(sum(invoice.Amount), 0) as Amount1, segment.Id, segment.Segment FROM dbo.CrmDealInvoices AS invoice",
                "JOIN dbo.CrmPeriods AS per ON invoice.PeriodId = per.Id",
                "JOIN dbo.CrmDeals AS deal ON invoice.DealId = deal.Id",
                "JOIN dbo.CrmSegments AS segment ON deal.SegmentId = segment.Id",
                "WHERE deal.IsDeleted = 0 AND invoice.IsDeleted = 0 AND invoice.IsToBe = 1 AND per.Month", compareString, month.ToString(), "AND per.Year = ", year.ToString(),
                "GROUP BY segment.Id, segment.Segment",
                ") as a",
                "FULL OUTER JOIN(",
                "select ISNULL(sum(invoice.Amount), 0) as Amount2, segment.Id, segment.Segment FROM dbo.CrmDealInvoices AS invoice",
                "JOIN dbo.CrmPeriods AS per ON invoice.PeriodId = per.Id",
                "JOIN dbo.CrmDeals AS deal ON invoice.DealId = deal.Id",
                "JOIN dbo.CrmSegments AS segment ON deal.SegmentId = segment.Id",
                "WHERE deal.IsDeleted = 0 AND invoice.IsDeleted = 0 AND invoice.IsToBe = 0 AND per.Month", compareString, month.ToString(), "AND per.Year = ", year.ToString(),
                "GROUP BY segment.Id, segment.Segment",
                ") as b ON b.Id = a.Id"
            });

            return await _context.SummaryReportRows.FromSql(sql).ToListAsync();
        }
        private async Task<List<SummaryReportRow>> GetRmProjectedInvoice(string compareString, int month, int year)
        {
            CrmDealRole role = GetDealRole("rm");
            string sql = string.Join(" ", new[]
            {
                "SELECT ISNULL(a.Id, b.Id) as Id, ISNULL(a.Firstname, b.Firstname) as Text, ISNULL(a.Amount1, 0) as Amount1, ISNULL(b.Amount2, 0) As Amount2, ISNULL(Amount1, 0) + ISNULL(Amount2, 0) AS Amount3 FROM",
                "(select distinct ISNULL(sum(invoice.Amount * member.Percentage / 100), 0) as Amount1, u.Id, u.FirstName FROM dbo.CrmDealInvoices AS invoice",
                "JOIN dbo.CrmPeriods AS per ON invoice.PeriodId = per.Id",
                "JOIN dbo.CrmDeals AS deal ON invoice.DealId = deal.Id",
                "JOIN dbo.CrmDealProposals AS proposal ON deal.Id = proposal.DealId",
                "JOIN dbo.CrmDealInternalMembers AS member ON deal.Id = member.DealId",
                "JOIN dbo.Users AS u ON member.UserId = u.ID",
                "WHERE deal.IsDeleted = 0 AND invoice.IsDeleted = 0 AND invoice.IsToBe = 1 AND per.Month", compareString, month.ToString(), "AND per.Year =", year.ToString(), "AND proposal.IsActive = 1",
                "AND member.RoleId = ", role.Id.ToString(),
                "GROUP BY invoice.Amount, u.Id, u.FirstName, member.Percentage) AS a",
                "FULL OUTER JOIN",
                "(select ISNULL(sum(invoice.Amount * deal.Percentage / 100), 0) as Amount2, u.Id, u.FirstName",
                "FROM dbo.CrmDealInvoices AS invoice",
                "JOIN dbo.CrmPeriods AS per ON invoice.PeriodId = per.Id",
                "JOIN dbo.CrmDealUserInvoices AS deal ON invoice.Id = deal.InvoiceId",
                "JOIN dbo.Users AS u ON deal.UserId = u.ID",
                "WHERE deal.IsDeleted = 0 AND invoice.IsDeleted = 0 AND invoice.IsToBe = 0 AND per.Month", compareString, month.ToString(), "AND per.Year =", year.ToString(),
                "GROUP BY deal.Amount, u.Id, u.FirstName, deal.Percentage",
                ") AS b",
                "ON a.Id = b.Id",
            });
            return await _context.SummaryReportRows.FromSql(sql).ToListAsync();
        }
        private int GetIndividualSummaryNProposal(int userId, int fromMonth, int toMonth, int year, List<int> tribeIds)
        {
            Func<CrmDealTribe, bool> WherePredicate = v =>
            {
                return tribeIds == null || tribeIds.Count() == 0 || tribeIds.Contains(v.Id);
            };

            CrmDealRole rm = GetDealRole("rm");
            var query = from prop in _context.CrmDealProposals
                        join deal in _context.CrmDeals on prop.DealId equals deal.Id
                        join tribe in _context.CrmDealTribes on deal.Id equals tribe.DealId
                        join member in _context.CrmDealInternalMembers on deal.Id equals member.DealId
                        join period in _context.CrmPeriods on prop.PeriodId equals period.Id
                        where member.UserId == userId && period.Year == year && period.Month >= fromMonth && period.Month <= toMonth &&
                        !prop.IsDeleted && !deal.IsDeleted && !member.IsDeleted && WherePredicate(tribe) && member.RoleId == rm.Id
                        select prop.Id;
            return query.Distinct().Count();
        }
        private async Task<long> GetIndividualSummaryProposalValue(int userId, int fromMonth, int toMonth, int year, List<int> tribeIds)
        {
            Func<CrmDealTribe, bool> WherePredicate = v =>
            {
                return tribeIds == null || tribeIds.Count() == 0 || tribeIds.Contains(v.Id);
            };

            CrmDealRole rm = GetDealRole("rm");
            var query = from prop in _context.CrmDealProposals
                        join deal in _context.CrmDeals on prop.DealId equals deal.Id
                        join tribe in _context.CrmDealTribes on deal.Id equals tribe.DealId
                        join member in _context.CrmDealInternalMembers on deal.Id equals member.DealId
                        join period in _context.CrmPeriods on prop.PeriodId equals period.Id
                        where member.UserId == userId && period.Year == year && period.Month >= fromMonth && period.Month <= toMonth &&
                        !prop.IsDeleted && !deal.IsDeleted && !member.IsDeleted && WherePredicate(tribe) && member.RoleId == rm.Id
                        select prop.Id;
            List<int> propIds = await query.Distinct().ToListAsync();

            var query1 = from prop in _context.CrmDealProposals
                         join deal in _context.CrmDeals on prop.DealId equals deal.Id
                         join member in _context.CrmDealInternalMembers on deal.Id equals member.DealId
                         where propIds.Contains(prop.Id)
                         select Convert.ToInt64(prop.ProposalValue * (Convert.ToDouble(member.Percentage) / 100d));
            return query1.Sum();
        }
        private int GetIndividualSummaryVisit(int userId, int fromMonth, int toMonth, int year, List<int> tribeIds)
        {
            /*
            Func<CrmDealTribe, bool> WherePredicate = v =>
            {
                return tribeIds == null || tribeIds.Count() == 0 || tribeIds.Contains(v.Id);
            };

            var query = from visitUser in _context.CrmDealVisitUsers
                         join visit in _context.CrmDealVisits on visitUser.VisitId equals visit.Id
                         join deal in _context.CrmDeals on visit.DealId equals deal.Id
                         join tribe in _context.CrmDealTribes on deal.Id equals tribe.DealId
                         join period in _context.CrmPeriods on visit.PeriodId equals period.Id
                         where visitUser.Userid == userId && period.Year == year && period.Month <= fromMonth && period.Month >= toMonth &&
                         !visit.IsDeleted && WherePredicate(tribe) && visitUser.IsRm
                         select visitUser.Id;
            return query.Distinct().Count();
            */
            var query = from visitUser in _context.CrmDealVisitUsers
                        join visit in _context.CrmDealVisits on visitUser.VisitId equals visit.Id
                        join period in _context.CrmPeriods on visit.PeriodId equals period.Id
                        where visitUser.Userid == userId && period.Year == year && period.Month >= fromMonth && period.Month <= toMonth &&
                        !visit.IsDeleted && visitUser.IsRm
                        select visitUser.Id;
            return query.Distinct().Count();

        }
        private long GetIndividualSummarySalesOnePeriod(int userId, int fromMonth, int toMonth, int year)
        {
            var query = from userInvoice in _context.CrmDealUserInvoices
                        join invoice in _context.CrmDealInvoices
                        on userInvoice.InvoiceId equals invoice.Id
                        join p in _context.CrmPeriods
                        on invoice.PeriodId equals p.Id
                        where userInvoice.UserId == userId && p.Month >= fromMonth && p.Month <= toMonth && p.Year == year && !invoice.IsDeleted && !invoice.IsToBe
                        select userInvoice.Amount;

            return query.Sum();
        }


        private async Task<DoubleLong> GetIndividualSummaryNProposal(int userId, int year)
        {
            IndividualExportProposal cur = await _crmReportService.GetListProposalByUserId("rm", userId, 1, 12, year, 0, 0, "*");
            IndividualExportProposal last = await _crmReportService.GetListProposalByUserId("rm", userId, 1, 12, year - 1, 0, 0, "*");
            //            IndividualExportProposal cur = await _crmReportService.GetListProposalByUserId(userId, 1, DateTime.Now.Month, year, 0, 0, "*"); 
            //          IndividualExportProposal last = await _crmReportService.GetListProposalByUserId(userId, 1, DateTime.Now.Month, year - 1, 0, 0, "*");

            DoubleLong response = new DoubleLong();

            response.Amount1 = cur.Items.Count();
            response.Amount2 = last.Items.Count();

            return response;

            /* Update 2021-09-09
            CrmDealRole rm = GetDealRole("rm");
            var query1 = from proposal in _context.CrmDealProposals
                    join deal in _context.CrmDeals
                    on proposal.DealId equals deal.Id
                    join t in _context.CrmDealProposalTypes
                    on proposal.TypeId equals t.Id
                    join u in _context.Users
                    on proposal.SentById equals u.ID
                    join member in _context.CrmDealInternalMembers
                    on deal.Id equals member.DealId
                    join p in _context.CrmPeriods
                    on proposal.PeriodId equals p.Id
                    where member.UserId == userId && member.RoleId == rm.Id && p.Year == year
                    select new IndividualReportProposalItem()
                    {
                        ProposalId = proposal.Id,
                        ProposalValue = Convert.ToInt64(Math.Round(member.Percentage * proposal.ProposalValue / 100)),
                        Name = deal.Name,
                        Type = t.Name,
                        SentBy = u.FirstName,
                        SentDate = proposal.SentDate,

                    };

            var query2 = from proposal in _context.CrmDealProposals
                         join deal in _context.CrmDeals
                         on proposal.DealId equals deal.Id
                         join t in _context.CrmDealProposalTypes
                         on proposal.TypeId equals t.Id
                         join u in _context.Users
                         on proposal.SentById equals u.ID
                         join member in _context.CrmDealInternalMembers
                         on deal.Id equals member.DealId
                         join p in _context.CrmPeriods
                         on proposal.PeriodId equals p.Id
                         where member.UserId == userId && member.RoleId == rm.Id && p.Year == (year - 1)
                         select new IndividualReportProposalItem()
                         {
                             ProposalId = proposal.Id,
                             ProposalValue = Convert.ToInt64(Math.Round(member.Percentage * proposal.ProposalValue / 100)),
                             Name = deal.Name,
                             Type = t.Name,
                             SentBy = u.FirstName,
                             SentDate = proposal.SentDate,

                         };
            DoubleLong response = new DoubleLong();

            response.Amount1 = query1.Distinct().Count();
            response.Amount2 = query2.Distinct().Count();

            return response;
            */
        }

        private DoubleLong GetIndividualSummaryVisit(int userId, int year)
        {
            // Muter dulu karena ada bug di post visit
            var query1 = from visitUser in _context.CrmDealVisitUsers
                         join visit in _context.CrmDealVisits
                         on visitUser.VisitId equals visit.Id
                         join deal in _context.CrmDeals
                         on visit.DealId equals deal.Id
                         join client in _context.CrmClients
                         on deal.ClientId equals client.Id
                         join period in _context.CrmPeriods
                         on visit.PeriodId equals period.Id
                         where visitUser.Userid == userId && !visit.IsDeleted && period.Year == year
                         select new IndividualReportVisitItem()
                         {
                             VisitId = visit.Id,
                             Company = client.Company,
                             VisitDate = visit.VisitFromTime,
                             Location = visit.Location,
                             Objective = visit.Objective,
                             NextStep = visit.NextStep,
                             Remarks = visit.Remark
                         };
            /*var query = from visitUser in _context.CrmDealVisitUsers
                        join visit in _context.CrmDealVisits
                        on visitUser.VisitId equals visit.Id
                        join client in _context.CrmClients
                        on visit.ClientId equals client.Id
                        join period in _context.CrmPeriods
                        on visit.PeriodId equals period.Id
                        where visitUser.Userid == userId && !visit.IsDeleted && period.Year == year
                        orderby visit.VisitFromTime descending
                        select new IndividualReportVisitItem()
                        {
                            VisitId = visit.Id,
                            Company = client.Company,
                            VisitDate = visit.VisitFromTime,
                            Location = visit.Location,
                            Objective = visit.Objective,
                            NextStep = visit.NextStep,
                            Remarks = visit.Remark
                        };*/
            var query2 = from visitUser in _context.CrmDealVisitUsers
                         join visit in _context.CrmDealVisits
                         on visitUser.VisitId equals visit.Id
                         join deal in _context.CrmDeals
                         on visit.DealId equals deal.Id
                         join client in _context.CrmClients
                         on deal.ClientId equals client.Id
                         join period in _context.CrmPeriods
                         on visit.PeriodId equals period.Id
                         where visitUser.Userid == userId && !visit.IsDeleted && period.Year == (year - 1)
                         select new IndividualReportVisitItem()
                         {
                             VisitId = visit.Id,
                             Company = client.Company,
                             VisitDate = visit.VisitFromTime,
                             Location = visit.Location,
                             Objective = visit.Objective,
                             NextStep = visit.NextStep,
                             Remarks = visit.Remark
                         };

            DoubleLong response = new DoubleLong();

            response.Amount1 = query1.Count();
            response.Amount2 = query2.Count();

            return response;

        }

        private DoubleLong GetIndividualSummarySales(int userId, int fromMonth, int toMonth, int year)
        {
            var query1 = from userInvoice in _context.CrmDealUserInvoices
                         join invoice in _context.CrmDealInvoices
                         on userInvoice.InvoiceId equals invoice.Id
                         join p in _context.CrmPeriods
                         on invoice.PeriodId equals p.Id
                         where userInvoice.UserId == userId && p.Month >= fromMonth && p.Month <= toMonth && p.Year == year && !invoice.IsDeleted && !invoice.IsToBe
                         select userInvoice.Amount;
            var query2 = from userInvoice in _context.CrmDealUserInvoices
                         join invoice in _context.CrmDealInvoices
                         on userInvoice.InvoiceId equals invoice.Id
                         join p in _context.CrmPeriods
                         on invoice.PeriodId equals p.Id
                         where userInvoice.UserId == userId && p.Month >= fromMonth && p.Month <= toMonth && p.Year == (year - 1) && !invoice.IsDeleted && !invoice.IsToBe
                         select userInvoice.Amount;

            long n1 = 0;
            long n2 = 0;

            List<long> ns1 = query1.ToList();
            List<long> ns2 = query2.ToList();

            DoubleLong response = new DoubleLong();
            response.Amount1 = ns1.Sum();
            response.Amount2 = ns2.Sum();

            return response;
        }

        private async Task<DoubleLong> GetSummaryHistoryItem1(int curMonth, int curYear)
        {
            List<SummaryReportRow> rows = await GetTribeProjectedInvoice("=", curMonth, curYear);

            DoubleLong response = new DoubleLong();

            foreach (SummaryReportRow row in rows)
            {
                response.Amount1 += row.Amount1;
                response.Amount1 += row.Amount2;
            }

            if ((curYear - 1) < 2020)
            {
                var query = from history in _context.CrmTribeSalesHistories
                            join per in _context.CrmPeriods
                            on history.PeriodId equals per.Id
                            where per.Month == curMonth && per.Year == (curYear - 1)
                            select history.Amount;
                List<long> a = query.ToList();
                response.Amount2 = a.Sum();
            }
            else
            {
                List<SummaryReportRow> rows2 = await GetTribeProjectedInvoice("=", curMonth, curYear - 1);

                foreach (SummaryReportRow row2 in rows2)
                {
                    response.Amount2 += row2.Amount1;
                    response.Amount2 += row2.Amount2;
                }
            }
            return response;
        }
        private async Task<DoubleLong> GetSummaryHistoryItem2(int curMonth, int curYear)
        {
            DoubleLong response = new DoubleLong();

            var q = from invoice in _context.CrmDealInvoices
                    join per in _context.CrmPeriods
                    on invoice.PeriodId equals per.Id
                    where per.Month == curMonth && per.Year == curYear && !invoice.IsDeleted && !invoice.IsToBe
                    select invoice.Amount;

            List<long> b = q.ToList();
            response.Amount1 = b.Sum();

            if ((curYear - 1) < 2020)
            {
                var query = from history in _context.CrmTribeSalesHistories
                            join per in _context.CrmPeriods
                            on history.PeriodId equals per.Id
                            where per.Month == curMonth && per.Year == (curYear - 1)
                            select history.Amount;
                List<long> a = query.ToList();
                response.Amount2 = a.Sum();
            }
            else
            {
                var q2 = from invoice in _context.CrmDealInvoices
                         join per in _context.CrmPeriods
                         on invoice.PeriodId equals per.Id
                         where per.Month == curMonth && per.Year == (curYear - 1) && !invoice.IsDeleted && !invoice.IsToBe
                         select invoice.Amount;

                List<long> c = q2.ToList();
                response.Amount2 = c.Sum();
            }
            return response;
        }
        private async Task<DoubleLong> GetSummaryHistoryItem3(int curMonth, int curYear)
        {
            DoubleLong response = new DoubleLong();

            var q = from tribeInvoice in _context.CrmDealTribeInvoices
                    join invoice in _context.CrmDealInvoices
                    on tribeInvoice.InvoiceId equals invoice.Id
                    join deal in _context.CrmDeals
                    on invoice.DealId equals deal.Id
                    join tribe in _context.CoreTribes
                    on tribeInvoice.TribeId equals tribe.Id
                    join per in _context.CrmPeriods
                    on invoice.PeriodId equals per.Id
                    where !deal.IsDeleted && per.Month <= curMonth && per.Year == curYear && !invoice.IsDeleted && !invoice.IsToBe
                    select tribeInvoice.Amount;

            List<long> b = q.ToList();
            response.Amount1 = b.Sum();

            if ((curYear - 1) < 2020)
            {
                var query = from history in _context.CrmTribeSalesHistories
                            join per in _context.CrmPeriods
                            on history.PeriodId equals per.Id
                            where per.Month <= curMonth && per.Year == (curYear - 1)
                            select history.Amount;
                List<long> a = query.ToList();
                response.Amount2 = a.Sum();
            }
            else
            {
                var q2 = from tribeInvoice in _context.CrmDealTribeInvoices
                         join invoice in _context.CrmDealInvoices
                         on tribeInvoice.InvoiceId equals invoice.Id
                         join deal in _context.CrmDeals
                         on invoice.DealId equals deal.Id
                         join tribe in _context.CoreTribes
                         on tribeInvoice.TribeId equals tribe.Id
                         join per in _context.CrmPeriods
                         on invoice.PeriodId equals per.Id
                         where !deal.IsDeleted && per.Month <= curMonth && per.Year == (curYear - 1) && !invoice.IsDeleted && !invoice.IsToBe
                         select tribeInvoice.Amount;

                List<long> c = q2.ToList();
                response.Amount2 = c.Sum();
            }
            return response;
        }
        private DoubleLong GetSummaryHistoryItem(string compareString, string isToBeInvoice, int curMonth, int curYear)
        {
            // compareString == "<=" atau "="
            // isToBeInvoice = "invoice.IsToBe = 0 AND" (untuk yang invoiced saja) atau "" (untuk semuanya)

            string secondTable = "dbo.CrmTribeSalesHistories";
            string isToBe = "";
            if (curYear != 2020)         // Untuk tahun 2020 dibandingkan dengan data history
            {
                secondTable = "dbo.CrmDealInvoices";
                isToBe = "invoice.IsToBe = 0 AND";
            }
            string sql1 = "select ISNULL(a.ytd, -1) AS Amount1, ISNULL(b.ytd, -1) AS Amount2 FROM (select sum(invoice.Amount) as ytd, per.Year FROM dbo.CrmDealInvoices AS invoice";
            sql1 = string.Join(" ", new[] { sql1, "JOIN dbo.CrmPeriods AS per ON invoice.PeriodId = per.Id" });
            sql1 = string.Join(" ", new[] { sql1, "JOIN dbo.CrmDeals as deal ON invoice.DealId = deal.Id" });
            sql1 = string.Join(" ", new[] { sql1, "JOIN dbo.CrmDealTribes as dealTribe on dealTribe.DealId = deal.Id" });

            sql1 = string.Join(" ", new[] { sql1, "WHERE", isToBeInvoice, "per.Month", compareString, curMonth.ToString(), "AND per.Year =", curYear.ToString(), "AND invoice.IsDeleted = 0" });
            sql1 = string.Join(" ", new[] { sql1, "GROUP BY per.Year) as a FULL OUTER JOIN (select sum(invoice.Amount) as ytd, per.Year FROM", secondTable, "AS invoice" });
            sql1 = string.Join(" ", new[] { sql1, "JOIN dbo.CrmPeriods AS per ON invoice.PeriodId = per.Id" });
            sql1 = string.Join(" ", new[] { sql1, "WHERE", isToBe, "per.Month", compareString, curMonth.ToString(), "AND per.Year =", (curYear - 1).ToString() });
            sql1 = string.Join(" ", new[] { sql1, "GROUP BY per.Year) as b ON b.Year = a.Year" });

            List<DoubleLong> ds = _context.GetDoubleLongs.FromSql(sql1).ToList();

            DoubleLong response = new DoubleLong();
            foreach (DoubleLong d in ds)
            {
                if (d.Amount1 == -1)
                {
                    response.Amount2 = d.Amount2;
                }
                else if (d.Amount2 == -1)
                {
                    response.Amount1 = d.Amount1;
                }
            }

            return response;
        }

        private async Task<int> UpdateUserInvoice(List<PercentInfo> rms, int dealId, long invAmount, DateTime now, int userId)
        {
            CrmDealRole role = GetDealRole("rm");

            List<CrmDealInternalMember> members = await _context.CrmDealInternalMembers.Where(a => a.DealId == dealId && a.RoleId == role.Id && !a.IsDeleted).ToListAsync();

            if (members != null)
            {
                foreach (CrmDealInternalMember member in members)
                {
                    member.IsDeleted = true;
                    member.DeletedBy = userId;
                    member.DeletedDate = now;
                    _context.Entry(member).State = EntityState.Modified;
                }
            }

            foreach (PercentInfo rm in rms)
            {
                CrmDealInternalMember member = new CrmDealInternalMember()
                {
                    DealId = dealId,
                    RoleId = role.Id,
                    UserId = rm.UserId,
                    Percentage = rm.Percent,
                    UsePercent = rm.UsePercent,
                    Nominal = rm.Nominal,
                    CreatedDate = now,
                    CreatedBy = userId,
                    LastUpdated = now,
                    LastUpdatedBy = userId,
                    IsDeleted = false,
                    DeletedBy = 0
                };
                _context.CrmDealInternalMembers.Add(member);
            }

            return await _context.SaveChangesAsync();
        }


        private async Task<int> UpdateTribeInvoice(List<PercentTribeInfo> tribes, int dealId, long invAmount, DateTime now, int userId)
        {
            List<CrmDealTribe> curTribes = await _context.CrmDealTribes.Where(a => a.DealId == dealId && !a.IsDeleted).ToListAsync();

            if (curTribes != null)
            {
                foreach (CrmDealTribe curTribe in curTribes)
                {
                    /*
                    curTribe.IsDeleted = true;
                    curTribe.DeletedBy = userId;
                    curTribe.DeletedDate = now;
                    _context.Entry(curTribe).State = EntityState.Modified;
                    */
                    _context.CrmDealTribes.Remove(curTribe);
                }
            }

            foreach (PercentTribeInfo tribe in tribes)
            {
                CrmDealTribe dealTribe = new CrmDealTribe()
                {
                    DealId = dealId,
                    TribeId = tribe.TribeId,
                    UsePercent = tribe.UsePercent,
                    Nominal = tribe.Nominal,
                    Percentage = tribe.Percent,
                    CreatedDate = now,
                    CreatedBy = userId,
                    LastUpdated = now,
                    LastUpdatedBy = userId,
                    IsDeleted = false,
                    DeletedBy = 0
                };
                _context.CrmDealTribes.Add(dealTribe);
            }

            return await _context.SaveChangesAsync();
        }
        private async Task<int> DeleteTribeUserInvoice(int invoiceId)
        {
            List<CrmDealTribeInvoice> curTribes = await _context.CrmDealTribeInvoices.Where(a => a.InvoiceId == invoiceId).ToListAsync();

            if (curTribes != null)
            {
                foreach (CrmDealTribeInvoice curTribe in curTribes)
                {
                    _context.CrmDealTribeInvoices.Remove(curTribe);
                }
            }

            List<CrmDealUserInvoice> curUsers = await _context.CrmDealUserInvoices.Where(a => a.InvoiceId == invoiceId).ToListAsync();
            if (curUsers != null)
            {
                foreach (CrmDealUserInvoice curUser in curUsers)
                {
                    _context.CrmDealUserInvoices.Remove(curUser);
                }
            }

            return await _context.SaveChangesAsync();
        }

        private async Task<int> UpdateDealTribes(List<PercentTribeInfo> tribes, int dealId, long invAmount, int userId)
        {
            try
            {
                // CrmDealTribes
                List<CrmDealTribe> curTribes = await _context.CrmDealTribes.Where(a => a.DealId == dealId && !a.IsDeleted).ToListAsync();

                if (curTribes != null)
                {
                    foreach (CrmDealTribe curTribe in curTribes)
                    {
                        PercentTribeInfo inList = tribes.Find(a => a.TribeId == curTribe.TribeId);
                        if (inList != null)
                        {
                            if (inList.UsePercent)
                            {
                                curTribe.Percentage = inList.Percent;
                                curTribe.Nominal = Convert.ToInt64(Math.Round(Convert.ToSingle(invAmount) * Convert.ToSingle(Convert.ToSingle(inList.Percent) / Convert.ToSingle(100))));
                            }
                            else
                            {
                                curTribe.Nominal = inList.Nominal;
                                curTribe.Percentage = inList.Nominal / invAmount;
                            }
                            curTribe.UsePercent = inList.UsePercent;
                            _context.Entry(curTribe).State = EntityState.Modified;
                            tribes.Remove(inList);
                        }
                        else
                        {
                            _context.CrmDealTribes.Remove(curTribe);
                        }
                    }
                }

                DateTime now = DateTime.Now;

                foreach (PercentTribeInfo tribe in tribes)
                {
                    long a = 0;
                    double p = 0.0d;

                    if (tribe.UsePercent)
                    {
                        p = tribe.Percent;
                        a = Convert.ToInt64(Math.Round(Convert.ToSingle(invAmount) * Convert.ToSingle(Convert.ToSingle(tribe.Percent) / Convert.ToSingle(100))));
                    }
                    else
                    {
                        a = tribe.Nominal;
                        p = tribe.Nominal / invAmount;
                    }

                    CrmDealTribe tribeInvoice = new CrmDealTribe()
                    {
                        DealId = dealId,
                        TribeId = tribe.TribeId,
                        Percentage = p,
                        CreatedDate = now,
                        CreatedBy = userId,
                        LastUpdated = now,
                        LastUpdatedBy = userId,
                        IsDeleted = false,
                        DeletedBy = 0,
                        Nominal = a,
                        UsePercent = tribe.UsePercent
                    };
                    _context.CrmDealTribes.Add(tribeInvoice);
                }

                return await _context.SaveChangesAsync();

            }
            catch
            {
                return 0;
            }
        }
        private async Task<int> UpdateTribeUserInvoice(List<PercentTribeInfo> tribes, List<PercentInfo> rms, int invoiceId, long invAmount)
        {
            try
            {
                // CrmDealTribeInvoice
                List<CrmDealTribeInvoice> curTribes = await _context.CrmDealTribeInvoices.Where(a => a.InvoiceId == invoiceId).ToListAsync();

                if (curTribes != null)
                {
                    foreach (CrmDealTribeInvoice curTribe in curTribes)
                    {
                        PercentTribeInfo inList = tribes.Find(a => a.TribeId == curTribe.TribeId);
                        if (inList != null)
                        {
                            if (inList.UsePercent)
                            {
                                curTribe.Percentage = inList.Percent;
                                curTribe.Amount = Convert.ToInt64(Math.Round(Convert.ToSingle(invAmount) * Convert.ToSingle(Convert.ToSingle(inList.Percent) / Convert.ToSingle(100))));
                            }
                            else
                            {
                                curTribe.Amount = inList.Nominal;
                                curTribe.Percentage = inList.Nominal / invAmount;
                            }
                            curTribe.UsePercent = inList.UsePercent;
                            _context.Entry(curTribe).State = EntityState.Modified;
                            tribes.Remove(inList);
                        }
                        else
                        {
                            _context.CrmDealTribeInvoices.Remove(curTribe);
                        }
                    }
                }


                foreach (PercentTribeInfo tribe in tribes)
                {
                    long a = 0;
                    double p = 0.0d;

                    if (tribe.UsePercent)
                    {
                        p = tribe.Percent;
                        a = Convert.ToInt64(Math.Round(Convert.ToSingle(invAmount) * Convert.ToSingle(Convert.ToSingle(tribe.Percent) / Convert.ToSingle(100))));
                    }
                    else
                    {
                        a = tribe.Nominal;
                        p = tribe.Nominal / invAmount;
                    }

                    CrmDealTribeInvoice tribeInvoice = new CrmDealTribeInvoice()
                    {
                        InvoiceId = invoiceId,
                        Amount = a,
                        Percentage = p,
                        TribeId = tribe.TribeId,
                        UsePercent = tribe.UsePercent
                    };
                    _context.CrmDealTribeInvoices.Add(tribeInvoice);
                }

                // CrmDealUserInvoices
                List<CrmDealUserInvoice> curUsers = await _context.CrmDealUserInvoices.Where(a => a.InvoiceId == invoiceId).ToListAsync();

                if (curUsers != null)
                {
                    foreach (CrmDealUserInvoice curUser in curUsers)
                    {
                        PercentInfo inList = rms.Find(a => a.UserId == curUser.UserId);
                        if (inList != null)
                        {
                            if (inList.UsePercent)
                            {
                                curUser.Percentage = inList.Percent;
                                curUser.Amount = Convert.ToInt64(Math.Round(Convert.ToSingle(invAmount) * Convert.ToSingle(Convert.ToSingle(inList.Percent) / Convert.ToSingle(100))));
                            }
                            else
                            {
                                curUser.Amount = inList.Nominal;
                                curUser.Percentage = inList.Nominal / invAmount;
                            }
                            curUser.UsePercent = inList.UsePercent;
                            _context.Entry(curUser).State = EntityState.Modified;
                            rms.Remove(inList);
                        }
                        else
                        {
                            _context.CrmDealUserInvoices.Remove(curUser);
                        }
                    }
                }

                foreach (PercentInfo rm in rms)
                {
                    long a = 0;
                    double p = 0.0d;

                    if (rm.UsePercent)
                    {
                        p = rm.Percent;
                        a = Convert.ToInt64(Math.Round(Convert.ToSingle(invAmount) * Convert.ToSingle(Convert.ToSingle(rm.Percent) / Convert.ToSingle(100))));
                    }
                    else
                    {
                        a = rm.Nominal;
                        p = rm.Nominal / invAmount;
                    }

                    CrmDealUserInvoice invoice = new CrmDealUserInvoice()
                    {
                        InvoiceId = invoiceId,
                        Amount = a,
                        Percentage = p,
                        UsePercent = rm.UsePercent,
                        UserId = rm.UserId
                    };
                    _context.CrmDealUserInvoices.Add(invoice);
                }

                return await _context.SaveChangesAsync();

            }
            catch
            {
                return 0;
            }

        }
        private List<HistoryItem> GetDealHistory(int dealId)
        {
            var query = from history in _context.CrmDealHistories
                        join type in _context.CrmDealHistoryTypes
                        on history.TypeId equals type.Id
                        join user in _context.Users
                        on history.CreatedBy equals user.ID
                        where history.DealId == dealId
                        orderby history.CreatedDate descending
                        select new
                        {
                            history.Id,
                            type.Shortname,
                            history.ActionDate,
                            history.Header1,
                            history.Header2,
                            history.Header3,
                            history.HeaderId1,
                            history.HeaderId2,
                            history.HeaderId3,
                            UpdateTime = history.CreatedDate,
                            UpdateById = history.CreatedBy,
                            UpdateByName = user.FirstName,
                            history.CurData,
                            history.PrevData
                        };
            var histories = query.ToList();
            List<HistoryItem> items = new List<HistoryItem>();

            foreach (var history in histories)
            {
                HistoryItem item = new HistoryItem();
                item.Type = history.Shortname;
                item.Data.Header1.Id = history.HeaderId1;
                item.Data.Header1.Text = history.Header1;
                item.Data.Header2.Id = history.HeaderId2;
                item.Data.Header2.Text = history.Header2;
                item.Data.Header3.Id = history.HeaderId3;
                item.Data.Header3.Text = history.Header3;
                item.Data.UpdateBy.Id = history.UpdateById;
                item.Data.UpdateBy.Text = history.UpdateByName;
                item.Data.UpdateTime = history.UpdateTime;
                if (item.Type.Equals("prop"))
                {
                    try
                    {
                        item.Data.Info = GetProposalInfo(Int32.Parse(history.CurData));
                    }
                    catch
                    {
                        return null;
                    }
                }
                else if (item.Type.Equals("meeting"))
                {
                    try
                    {
                        item.Data.Info = GetVisitInfo(Int32.Parse(history.CurData));
                    }
                    catch
                    {
                        return null;
                    }

                }
                else if (item.Type.Equals("pnl"))
                {
                    try
                    {
                        item.Data.Info = new { Filename = history.PrevData, id = history.CurData };
                    }
                    catch
                    {
                        return null;
                    }

                }
                else if (item.Type.Equals("created"))
                {
                    try
                    {
                        item.Data.Info = new { DateCreated = history.ActionDate };
                    }
                    catch
                    {
                        return null;
                    }

                }
                else if (item.Type.Equals("invoicedcanceled"))
                {
                    try
                    {
                        item.Data.Info = new { DateCreated = history.ActionDate };
                    }
                    catch
                    {
                        return null;
                    }

                }
                else if (item.Type.Equals("tobe"))
                {
                    try
                    {
                        CrmDealHistory rec = _context.CrmDealHistories.Find(history.Id);
                        if (rec != null)
                        {
                            item.Data.Info = new
                            {
                                invoiceDate = rec.ActionDate,
                                remarks = rec.Remarks,
                                amount = rec.RemarksValue
                            };
                        }
                    }
                    catch
                    {
                        return null;
                    }
                }
                else if (item.Type.Equals("tobedate"))
                {
                    CrmDealHistory rec = _context.CrmDealHistories.Find(history.Id);
                    if (rec != null)
                    {
                        item.Data.Info = new
                        {
                            remarks = rec.Remarks
                        };
                    }
                }
                else if (item.Type.Equals("tobeamount"))
                {
                    CrmDealHistory rec = _context.CrmDealHistories.Find(history.Id);
                    if (rec != null)
                    {
                        item.Data.Info = new
                        {
                            remarks = rec.Remarks,
                            amount = rec.RemarksValue
                        };
                    }
                }
                else if (item.Type.Equals("invoiced"))
                {
                    try
                    {
                        CrmDealHistory rec = _context.CrmDealHistories.Find(history.Id);
                        if (rec != null)
                        {
                            CrmContact contact = _context.CrmContacts.Find(rec.ReservedId);
                            string nm = "";
                            int id = 0;
                            if (contact != null && contact.Id > 0)
                            {
                                nm = contact.Name;
                                id = contact.Id;
                            }
                            item.Data.Info = new
                            {
                                filename = history.PrevData,
                                invoiceId = Int32.Parse(history.CurData),
                                invoiceDate = history.ActionDate,
                                contactName = nm,
                                contactId = id,
                                remarks = rec.Remarks,
                                amount = rec.RemarksValue
                            };

                        }



                    }
                    catch
                    {
                        return null;
                    }
                }

                items.Add(item);
            }
            return items;
        }

        private async Task<Error> AddDealTribe(List<PercentTribeInfo> tribes, int dealId, DateTime now, int userId)
        {
            try
            {
                DeleteCurDealTribes(tribes, dealId, now, userId);

                foreach (PercentTribeInfo tribe in tribes)
                {
                    if (_context.CrmDealTribes.Any(a => a.TribeId == tribe.TribeId && a.DealId == dealId && !a.IsDeleted))
                    {
                        CrmDealTribe curTribe = _context.CrmDealTribes.Where(a => a.TribeId == tribe.TribeId && a.DealId == dealId && !a.IsDeleted).FirstOrDefault();

                        if (curTribe != null && curTribe.Id > 0)
                        {
                            curTribe.Percentage = tribe.Percent;
                            curTribe.Nominal = tribe.Nominal;
                            curTribe.UsePercent = tribe.UsePercent;
                            curTribe.LastUpdated = now;
                            curTribe.LastUpdatedBy = userId;
                            _context.Entry(curTribe).State = EntityState.Modified;
                        }
                    }
                    else
                    {
                        CrmDealTribe t = new CrmDealTribe()
                        {
                            DealId = dealId,
                            TribeId = tribe.TribeId,
                            Percentage = tribe.Percent,
                            Nominal = tribe.Nominal,
                            UsePercent = tribe.UsePercent,
                            CreatedDate = now,
                            CreatedBy = userId,
                            LastUpdated = now,
                            LastUpdatedBy = userId,
                            IsDeleted = false,
                            DeletedBy = 0
                        };
                        _context.CrmDealTribes.Add(t);

                    }
                }
                await _context.SaveChangesAsync();

            }
            catch
            {
                return new Error("error", "Error writing to database");

            }
            return new Error("ok", "");
        }

        private void DeleteCurDealTribes(List<PercentTribeInfo> tribes, int dealId, DateTime now, int userId)
        {
            List<CrmDealTribe> curTribes = _context.CrmDealTribes.Where(a => a.DealId == dealId && !a.IsDeleted).ToList();
            foreach (CrmDealTribe tribe in curTribes)
            {
                if (!tribes.Any(a => a.TribeId == tribe.TribeId))
                {
                    /*
                    tribe.IsDeleted = true;
                    tribe.DeletedBy = userId;
                    tribe.DeletedDate = now;
                    _context.Entry(tribe).State = EntityState.Modified;
                    */
                    _context.CrmDealTribes.Remove(tribe);
                }
                _context.SaveChanges();
            }
        }
        private void DeleteCurInternalMembers(List<PercentInfo> users, int dealId, int roleId, DateTime now, int userId)
        {
            List<CrmDealInternalMember> curMembers = _context.CrmDealInternalMembers.Where(a => a.DealId == dealId && a.RoleId == roleId && !a.IsDeleted).ToList();
            foreach (CrmDealInternalMember curMember in curMembers)
            {
                if (!users.Any(a => a.UserId == curMember.UserId))
                {
                    curMember.IsDeleted = true;
                    curMember.DeletedBy = userId;
                    curMember.DeletedDate = now;
                    _context.Entry(curMember).State = EntityState.Modified;
                }
                _context.SaveChanges();
            }
        }

        private async Task<Error> AddDealOwner(List<PercentInfo> users, int dealId, string shortname, DateTime now, int userId)
        {
            CrmDealRole role = GetDealRole(shortname);
            if (role == null || role.Id == 0) return new Error("error", "Role not found");

            try
            {
                DeleteCurInternalMembers(users, dealId, role.Id, now, userId);
                foreach (PercentInfo pi in users)
                {
                    User u = _context.Users.Find(pi.UserId);
                    if (u == null || u.ID == 0) return new Error("error", "User not found");

                    if (_context.CrmDealInternalMembers.Any(a => a.UserId == pi.UserId && a.DealId == dealId && !a.IsDeleted))
                    {
                        CrmDealInternalMember member = _context.CrmDealInternalMembers.Where(a => a.UserId == pi.UserId && a.DealId == dealId && !a.IsDeleted).FirstOrDefault();
                        if (member != null && member.Id > 0)
                        {
                            member.RoleId = role.Id;
                            member.Percentage = pi.Percent;
                            member.UsePercent = pi.UsePercent;
                            member.Nominal = pi.Nominal;
                            member.LastUpdated = now;
                            member.LastUpdatedBy = userId;
                            _context.Entry(member).State = EntityState.Modified;
                        }
                    }
                    else
                    {
                        CrmDealInternalMember member = new CrmDealInternalMember()
                        {
                            DealId = dealId,
                            RoleId = role.Id,
                            UserId = pi.UserId,
                            Percentage = pi.Percent,
                            UsePercent = pi.UsePercent,
                            Nominal = pi.Nominal,
                            CreatedDate = now,
                            CreatedBy = userId,
                            LastUpdated = now,
                            LastUpdatedBy = userId,
                            IsDeleted = false,
                            DeletedBy = 0
                        };
                        _context.CrmDealInternalMembers.Add(member);

                        if (shortname.Equals("rm"))
                        {
                            await AddDealHistory("addrm", dealId, "", "", now, userId, now, userId, "Relationship Manager", u.FirstName, "Join to this deal", 0, u.ID, 0);

                        }
                    }
                }
                await _context.SaveChangesAsync();

            }
            catch
            {
                return new Error("error", "Error writing to database");

            }
            return new Error("ok", "");
        }
        private void AddVisitContacts(List<int> contacts, int visitId)
        {
            List<CrmDealVisitContact> curContacts = _context.CrmDealVisitContacts.Where(a => a.VisitId == visitId).ToList();
            foreach (CrmDealVisitContact con in curContacts)
            {
                _context.Remove(con);
            }

            foreach (int c in contacts)
            {
                CrmDealVisitContact contact = new CrmDealVisitContact()
                {
                    VisitId = visitId,
                    ContactId = c
                };
                _context.CrmDealVisitContacts.Add(contact);
            }
            _context.SaveChanges();
        }
        private void AddVisitTribes(List<int> tribes, int visitId)
        {
            if (tribes == null) return;

            List<CrmDealVisitTribe> curTribes = _context.CrmDealVisitTribes.Where(a => a.VisitId == visitId).ToList();
            foreach (CrmDealVisitTribe u in curTribes)
            {
                _context.Remove(u);
            }

            foreach (int t in tribes)
            {
                CrmDealVisitTribe tribe = new CrmDealVisitTribe()
                {
                    VisitId = visitId,
                    TribeId = t
                };
                _context.CrmDealVisitTribes.Add(tribe);
            }
            _context.SaveChanges();
        }
        private void AddVisitUsers(List<int> users, int visitId, bool isRm, bool isConsultant)
        {
            List<CrmDealVisitUser> curUsers = _context.CrmDealVisitUsers.Where(a => a.VisitId == visitId && a.IsRm == isRm && a.IsConsultant == isConsultant).ToList();
            foreach (CrmDealVisitUser u in curUsers)
            {
                _context.Remove(u);
            }

            foreach (int con in users)
            {
                CrmDealVisitUser user = new CrmDealVisitUser()
                {
                    VisitId = visitId,
                    Userid = con,
                    IsRm = isRm,
                    IsConsultant = isConsultant
                };
                _context.CrmDealVisitUsers.Add(user);
            }
            _context.SaveChanges();
        }

        private async Task<CrmDealHistory> AddDealHistory(string shortname, int dealId, string prevData, string curData, DateTime actionDate, int actionBy, DateTime createdDate, int createdBy, string header1, string header2, string header3, int headerId1, int headerId2, int headerId3, string remarks = "", long v = 0, string reserved = "", int reservedId = 0)
        {
            CrmDealHistoryType type = GetDealHistoryType(shortname);
            if (type == null || type.Id == 0)
            {
                return null;
            }

            CrmDealHistory history = new CrmDealHistory()
            {
                DealId = dealId,
                TypeId = type.Id,
                PrevData = prevData,
                CurData = curData,
                ActionDate = actionDate,
                ActionBy = actionBy,
                Header1 = header1,
                Header2 = header2,
                Header3 = header3,
                HeaderId1 = headerId1,
                HeaderId2 = headerId2,
                HeaderId3 = headerId3,
                Remarks = remarks,
                RemarksValue = v,
                ReservedText = reserved,
                ReservedId = reservedId,
                CreatedDate = createdDate,
                CreatedBy = createdBy
            };

            _context.CrmDealHistories.Add(history);
            await _context.SaveChangesAsync();

            return history;
        }

        private async Task<CrmDealHistory> EditDealHistory(string shortname, int dealId, string prevData, string curData, DateTime actionDate, int actionBy, DateTime createdDate, int createdBy, string header1, string header2, string header3, int headerId1, int headerId2, int headerId3, string remarks = "", long v = 0, string reserved = "", int reservedId = 0)
        {
            // Search berdasarkan type, dealId, dan curData
            CrmDealHistoryType type = GetDealHistoryType(shortname);
            if (type == null || type.Id == 0)
            {
                return null;
            }

            CrmDealHistory item = _context.CrmDealHistories.Where(a => a.DealId == dealId && a.TypeId == type.Id && a.CurData.Equals(curData)).FirstOrDefault();
            if (item == null)
            {
                return null;
            }

            item.DealId = dealId;
            item.TypeId = type.Id;
            item.PrevData = prevData;
            item.ActionDate = actionDate;
            item.ActionBy = actionBy;
            item.Header1 = header1;
            item.Header2 = header2;
            item.Header3 = header3;
            item.HeaderId1 = headerId1;
            item.HeaderId2 = headerId2;
            item.HeaderId3 = headerId3;
            item.Remarks = remarks;
            item.RemarksValue = v;
            item.ReservedText = reserved;
            item.ReservedId = reservedId;
            item.CreatedDate = createdDate;
            item.CreatedBy = createdBy;

            _context.Entry(item).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return item;
        }

        private async Task<CrmDealHistory> UpdateDealHistoryRemarks(string shortname, int dealId, int invoiceId, string remarks)
        {
            // Search berdasarkan type, dealId, dan curData
            CrmDealHistoryType type = GetDealHistoryType(shortname);
            if (type == null || type.Id == 0)
            {
                return null;
            }

            CrmDealHistory item = _context.CrmDealHistories.Where(a => a.DealId == dealId && a.TypeId == type.Id && a.CurData.Equals(invoiceId.ToString())).FirstOrDefault();
            if (item == null)
            {
                return null;
            }

            item.Remarks = remarks;

            _context.Entry(item).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return item;
        }

        private async Task<CrmDealHistory> UpdateDealHistory(string shortname, int dealId, string prevData, string curData, DateTime actionDate, int actionBy, DateTime createdDate, int createdBy, string header1, string header2, string header3, int headerId1, int headerId2, int headerId3)
        {
            CrmDealHistoryType type = GetDealHistoryType(shortname);
            if (type == null || type.Id == 0)
            {
                return null;
            }

            CrmDealHistory history = _context.CrmDealHistories.Where(a => a.DealId == dealId && a.PrevData.Equals("prevData")).FirstOrDefault();
            if (history == null || history.Id == 0)
            {
                return null;
            }

            history.DealId = dealId;
            history.TypeId = type.Id;
            history.PrevData = prevData;
            history.CurData = curData;
            history.ActionDate = actionDate;
            history.ActionBy = actionBy;
            history.Header1 = header1;
            history.Header2 = header2;
            history.Header3 = header3;
            history.HeaderId1 = headerId1;
            history.HeaderId2 = headerId2;
            history.HeaderId3 = headerId3;
            history.CreatedDate = createdDate;
            history.CreatedBy = createdBy;

            _context.Entry(history).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return history;
        }

        private CrmDealHistoryType GetDealHistoryType(string shortname)
        {
            return _context.CrmDealHistoryTypes.Where(a => a.Shortname.Equals(shortname)).FirstOrDefault();
        }
        private CrmDealRole GetDealRole(string shortname)
        {
            return _context.CrmDealRoles.Where(a => a.Shortname.Equals(shortname)).FirstOrDefault();
        }

        private CrmDealState GetDealState(string shortname)
        {
            return _context.CrmDealStates.Where(a => a.Shortname.Equals(shortname)).FirstOrDefault();
        }

        private async Task<List<GenericInfo>> GetAllStates()
        {
            var query = from state in _context.CrmDealStates
                        where !state.IsDeleted
                        orderby state.Id
                        select new GenericInfo()
                        {
                            Text = state.State,
                            Id = state.Id
                        };

            return await query.ToListAsync();
        }

        private async Task<List<GenericInfo>> GetAllStages()
        {
            var query = from state in _context.CrmDealStages
                        where !state.IsDeleted
                        orderby state.Id
                        select new GenericInfo()
                        {
                            Text = state.Stage,
                            Id = state.Id
                        };

            return await query.ToListAsync();
        }

        private async Task<List<GenericInfo>> GetAllTribes()
        {
            var query = from state in _context.CoreTribes
                        where !state.IsDeleted
                        orderby state.Id
                        select new GenericInfo()
                        {
                            Text = state.Tribe,
                            Id = state.Id
                        };

            return await query.ToListAsync();
        }

        private async Task<List<GenericInfo>> GetAllSegments()
        {
            var query = from state in _context.CrmSegments
                        where !state.IsDeleted
                        orderby state.Id
                        select new GenericInfo()
                        {
                            Text = state.Segment,
                            Id = state.Id
                        };

            return await query.ToListAsync();
        }

        private async Task<List<GenericInfo>> GetAllBranches()
        {
            var query = from state in _context.CrmBranches
                        where !state.IsDeleted
                        orderby state.Id
                        select new GenericInfo()
                        {
                            Text = state.Branch,
                            Id = state.Id
                        };

            return await query.ToListAsync();
        }

        private async Task<List<GenericInfo>> GetProposalTypes()
        {
            var query = from state in _context.CrmDealProposalTypes
                        where !state.IsDeleted
                        orderby state.Id
                        select new GenericInfo()
                        {
                            Text = state.Name,
                            Id = state.Id
                        };

            return await query.ToListAsync();
        }

        private async Task<List<RMInfo>> GetAllRMs()
        {
            var query = from rm in _context.CrmRelManagers
                        join user in _context.Users
                        on rm.UserId equals user.ID
                        where rm.isActive && rm.IsDeleted == false
                        orderby user.FirstName
                        select new RMInfo()
                        {
                            Id = rm.Id,
                            UserId = rm.UserId,
                            SegmentId = rm.SegmentId,
                            BranchId = rm.BranchId,
                            LeaderId = rm.LeaderId,
                            Name = user.FirstName,
                            Email = user.Email,
                            Percentage = 0,
                            UsePercent = true,
                            Nominal = 0
                        };
            return await query.ToListAsync();
        }

        private int GetPeriodId(DateTime date, int userId)
        {
            if (PeriodExists(date.Month, date.Year))
            {
                CrmPeriod p = GetPeriod(date.Month, date.Year);
                if (p != null && p.Id > 0)
                {
                    return p.Id;
                }
            }

            DateTime now = DateTime.Now;

            for (int i = 1; i <= 12; i++)
            {
                if (!PeriodExists(i, date.Year))
                {
                    CrmPeriod p2 = new CrmPeriod()
                    {
                        Month = i,
                        Year = date.Year,
                        CreatedDate = now,
                        CreatedBy = userId,
                        LastUpdated = now,
                        LastUpdatedBy = userId,
                        IsDeleted = false,
                        DeletedBy = 0
                    };
                    _context.CrmPeriods.Add(p2);
                }
            }

            _context.SaveChanges();

            CrmPeriod period = GetPeriod(date.Month, date.Year);
            if (period != null && period.Id > 0)
            {
                return period.Id;
            }

            return 0;

        }

        private bool DealExists(int id)
        {
            return _context.CrmDeals.Any(e => e.Id == id);
        }

        private bool VisitExists(int id)
        {
            return _context.CrmDealVisits.Any(e => e.Id == id);
        }
        private CrmPeriod GetPeriod(int month, int year)
        {
            return _context.CrmPeriods.Where(e => e.Month == month && e.Year == year && !e.IsDeleted).FirstOrDefault();
        }

        private bool PeriodExists(int month, int year)
        {
            return _context.CrmPeriods.Any(e => e.Month == month && e.Year == year && !e.IsDeleted);
        }

        private string GetDealDirectory(int dealId)
        {
            return Path.Combine(_options.DataRootDirectory, @"deal", dealId.ToString());
        }
        private VisitInfo GetVisitInfo(int visitId)
        {
            if (!VisitExists(visitId))
            {
                return null;
            }
            var query = from visit in _context.CrmDealVisits
                        join deal in _context.CrmDeals
                        on visit.DealId equals deal.Id
                        join client in _context.CrmClients
                        on deal.ClientId equals client.Id
                        where visit.Id == visitId
                        select new VisitInfo
                        {
                            VisitId = visit.Id,
                            ClientId = client.Id,
                            ClientName = client.Company,
                            Location = visit.Location,
                            NextStep = visit.NextStep,
                            Objective = visit.Objective,
                            Remark = visit.Remark,
                            FromTime = visit.VisitFromTime,
                            ToTime = visit.VisitToTime
                        };
            VisitInfo info = query.FirstOrDefault();
            if (info != null && info.VisitId != 0)
            {
                var q = from visitor in _context.CrmDealVisitUsers
                        join user in _context.Users
                        on visitor.Userid equals user.ID
                        where visitor.VisitId == visitId && visitor.IsRm
                        select new GenericInfo()
                        {
                            Id = user.ID,
                            Text = user.FirstName
                        };
                info.Rms = q.ToList();

                var q2 = from visitor in _context.CrmDealVisitContacts
                         join contact in _context.CrmContacts
                         on visitor.ContactId equals contact.Id
                         where visitor.VisitId == visitId
                         select new GenericInfo()
                         {
                             Id = contact.Id,
                             Text = contact.Name
                         };
                info.Contacts = q2.ToList();

                var q3 = from visitor in _context.CrmDealVisitUsers
                         join user in _context.Users
                         on visitor.Userid equals user.ID
                         where visitor.VisitId == visitId && visitor.IsConsultant
                         select new GenericInfo()
                         {
                             Id = user.ID,
                             Text = user.FirstName
                         };
                info.Consultants = q3.ToList();

                var q4 = from vt in _context.CrmDealVisitTribes
                         join tribe in _context.CoreTribes
                         on vt.TribeId equals tribe.Id
                         where vt.VisitId == visitId
                         select new GenericInfo()
                         {
                             Id = tribe.Id,
                             Text = tribe.Tribe
                         };
                info.Tribes = q4.ToList();
            }

            return info;
        }
        private PostPricingResponse GetPricingInfo(int pnlId, int docType)
        {
            var q = from pnl in _context.CrmDealPNLs
                    where pnl.Id == pnlId && pnl.IsActive && !pnl.IsDeleted && pnl.DocumentType == docType
                    select new PostPricingResponse()
                    {
                        Id = pnl.Id,
                        DealId = pnl.DealId,
                        Filename = pnl.OriginalFilename
                    };

            return q.FirstOrDefault();
        }

        private PostProposalResponse GetProposalInfo(int proposalId)
        {
            var query = from prop in _context.CrmDealProposals
                        join type in _context.CrmDealProposalTypes
                        on prop.TypeId equals type.Id
                        where prop.Id == proposalId
                        select new
                        {
                            Id = prop.Id,
                            DealId = prop.DealId,
                            SentById = prop.SentById,
                            SentDate = prop.SentDate,
                            RandomName = prop.Filename,
                            ProposalTypeName = type.Name,
                            ProposalTypeId = type.Id,
                            Filename = prop.OriginalFilename,
                            Proposalvalue = prop.ProposalValue
                        };
            var obj = query.FirstOrDefault();
            if (obj != null && obj.Id > 0)
            {
                PostProposalResponse response = new PostProposalResponse();
                response.Id = obj.Id;
                response.SentDate = obj.SentDate;
                response.SentById = obj.SentById;
                response.ProposalType.Id = obj.ProposalTypeId;
                response.ProposalType.Text = obj.ProposalTypeName;
                response.Filename = obj.Filename;
                response.Url = Uri.EscapeUriString(string.Join("/", new[] { "", "download", "deal", obj.DealId.ToString(), obj.RandomName, obj.Filename }));
                response.ProposalValue = obj.Proposalvalue;

                var q = from invs in _context.CrmDealProposalInvoices
                        where invs.ProposalId == obj.Id
                        select new InvoicePeriodInfo()
                        {
                            Id = invs.Id,
                            InvoiceDate = invs.InvoiceDate,
                            InvoiceAmount = invs.InvoiceAmount,
                            Remarks = invs.Remarks
                        };
                response.Invoices = q.ToList();

                response.ContactIds = _context.CrmDealProposalSentContacts.Where(a => a.ProposalId == obj.Id).Select(a => a.ContactId).ToList();

                return response;
            }
            return null;
        }
        private Error SaveFileUpload(string base64String, string filename, int dealId)
        {
            try
            {
                // base64String = base64String.Substring(n);
                var fileExt = System.IO.Path.GetExtension(filename).Substring(1).ToLower();

                string randomName = Path.GetRandomFileName() + "." + fileExt;
                string fileDir = GetDealDirectory(dealId);
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

        private string CreateWhereClause(string str, string prefix, string field, string mid)
        {

            if (str.Equals("0"))
            {
                return "";
            }

            List<string> rets = new List<string>();
            rets.Add(prefix);
            rets.Add("(");

            bool init = false;
            foreach (string s in str.Split(","))
            {
                try
                {
                    int n = Int32.Parse(s);
                    if (!init)
                    {
                        init = true;
                        rets.Add(string.Join(" ", new[] { field, "=", n.ToString() }));
                    }
                    else
                    {
                        rets.Add(string.Join(" ", new[] { "", "OR", field, "=", n.ToString() }));
                    }
                }
                catch
                {
                    return null;
                };
            }

            rets.Add(")");
            return string.Join("", rets);
        }

        private List<MonthStageInfo> GetMonthStages(int startMonth, int startYear, int n, MonthStageInfo last)
        {
            List<MonthStageInfo> response = new List<MonthStageInfo>();

            int curStage = 1;

            while (response.Count < (n - 1))
            {
                response.Add(AddMonthStage(curStage, startMonth, startYear, true, last));
                startMonth++;
                if (startMonth > 12)
                {
                    startMonth = 1;
                    startYear++;
                }
                curStage++;
            }
            response.Add(AddMonthStage(curStage, startMonth, startYear, false, last));

            return response;
        }

        private MonthStageInfo AddMonthStage(int stage, int month, int year, bool monthName, MonthStageInfo last)
        {
            MonthStageInfo info = new MonthStageInfo()
            {
                Stage = stage,
                Month = month,
                Year = year,
                Text = String.Format("{0:MMMM}", new DateTime(year, month, 1))
            };

            if (!monthName && last != null)
            {
                string b = "";
                if (last.Year > year)
                {
                    string fromStr = String.Format("{0:MMM}", new DateTime(year, month, 1));
                    string toStr = String.Format("{0:MMM yyyy}", new DateTime(last.Year, last.Month, 1));
                    info.Text = string.Join(" - ", new[] { fromStr, toStr });
                }
                else if (last.Month > month)
                {
                    string fromStr = String.Format("{0:MMM}", new DateTime(year, month, 1));
                    string toStr = String.Format("{0:MMM}", new DateTime(last.Year, last.Month, 1));
                    info.Text = string.Join(" - ", new[] { fromStr, toStr });
                }
            }
            return info;
        }

        private async Task AddToToBeInvoicedAsync(CrmDeal deal, DateTime now, int userId)
        {
            var q = from invoice in _context.CrmDealProposalInvoices
                    join proposal in _context.CrmDealProposals
                    on invoice.ProposalId equals proposal.Id
                    where proposal.IsActive && proposal.DealId == deal.Id
                    select new CrmDealInvoice()
                    {
                        DealId = proposal.DealId,
                        Amount = invoice.InvoiceAmount,
                        PeriodId = invoice.PeriodId,
                        InvoiceDate = invoice.InvoiceDate,
                        Filename = "",
                        OriginalFilename = "",
                        Remarks = invoice.Remarks,
                        IsToBe = true,
                        CreatedDate = now,
                        CreatedBy = userId,
                        LastUpdated = now,
                        LastUpdatedBy = userId,
                        IsDeleted = false,
                        DeletedBy = 0,
                    };

            List<CrmDealInvoice> invoices = q.ToList();
            _context.CrmDealInvoices.AddRange(invoices);

            foreach (CrmDealInvoice inv in invoices)
            {
                string prevData = inv.InvoiceDate.ToString("DD MMM YYYY", CultureInfo.CreateSpecificCulture("en"));
                _ = await AddDealHistory("tobe", deal.Id, prevData, inv.Id.ToString(), inv.InvoiceDate, userId, now, userId, "To Be Invoiced", "", "", 0, 0, 0, inv.Remarks, inv.Amount);
                await SendEmail(EMAIL_ADD_TO_BE_INVOICED, deal, inv);
            }

            _context.SaveChanges();


        }

        private SummaryItem GetSummaryItem(string tribeFilter, string segmentFilter, string rmFilter, string probabilityFilter, int stateId)
        {
            string selectSql = "SELECT DISTINCT invoice.Id, invoice.InvoiceAmount";
            string fromSql = "FROM dbo.CrmDealProposalInvoices AS invoice";
            string joinSql1 = "JOIN dbo.CrmDealProposals AS proposal ON invoice.ProposalId = proposal.Id AND proposal.IsActive = 1";
            string joinSql2 = "JOIN dbo.CrmDeals as deal ON proposal.DealId = deal.Id";
            string joinSql3 = "JOIN dbo.CrmClients AS client ON deal.ClientId = client.Id";
            string joinSql3b = "JOIN dbo.CrmDealTribes AS dealTribe ON dealTribe.DealId = deal.Id ";
            string joinSql4 = "JOIN dbo.CoreTribes AS tribe ON tribe.Id = dealTribe.TribeId";
            string joinSql5 = "JOIN dbo.CrmSegments AS segment ON deal.SegmentId = segment.Id ";
            string orderBy = "";

            List<string> wheres = new List<string>();
            wheres.Add(string.Join(" ", new[] { "WHERE deal.IsDeleted = 0", "AND", "deal.StateId", "=", stateId.ToString() }));
            wheres.Add(CreateWhereClause(tribeFilter, "AND ", "tribe.Id", "OR"));
            wheres.Add(CreateWhereClause(segmentFilter, "AND ", "segment.Id", "OR"));
            wheres.Add(CreateWhereClause(probabilityFilter, "AND ", "deal.Probability", "OR"));

            string joinSql6 = "";
            if (!rmFilter.Equals("0"))
            {
                CrmDealRole role = GetDealRole("rm");
                joinSql3 = "JOIN dbo.CrmDealInternalMembers AS member ON deal.Id = member.DealId";
                wheres.Add(string.Join(" ", new[] { "AND", "member.RoleId", "=", role.Id.ToString() }));
                wheres.Add(CreateWhereClause(rmFilter, "AND ", "member.UserId", "OR"));
            }
            string whereSql = string.Join(" ", wheres);
            string sql = string.Join(" ", new[] { selectSql, fromSql, joinSql1, joinSql2, joinSql3, joinSql3b, joinSql4, joinSql5, joinSql6, whereSql, orderBy });

            string preSql = "SELECT ISNULL(SUM(b.InvoiceAmount), 0) AS Amount FROM(";
            string postSql = ") as b";
            sql = string.Join("", new[] { preSql, sql, postSql });

            return _context.SummaryItems.FromSql(sql).FirstOrDefault<SummaryItem>();

        }

        private SummaryItem GetSummaryItemByMonth(string fromMonth, string toMonth, string tribeFilter, string segmentFilter, string rmFilter, string probabilityFilter, int stateId)
        {
            string selectSql = "SELECT DISTINCT invoice.Id, invoice.InvoiceAmount";
            string fromSql = "FROM dbo.CrmDealProposalInvoices AS invoice";
            string joinSql1 = "JOIN dbo.CrmDealProposals AS proposal ON invoice.ProposalId = proposal.Id AND proposal.IsActive = 1";
            string joinSql2 = "JOIN dbo.CrmDeals as deal ON proposal.DealId = deal.Id";
            string joinSql3 = "JOIN dbo.CrmClients AS client ON deal.ClientId = client.Id";
            string joinSql3b = "JOIN dbo.CrmDealTribes AS dealTribe ON dealTribe.DealId = deal.Id ";
            string joinSql4 = "JOIN dbo.CoreTribes AS tribe ON tribe.Id = dealTribe.TribeId";
            string joinSql5 = "JOIN dbo.CrmSegments AS segment ON deal.SegmentId = segment.Id ";
            string orderBy = "";

            string whereFromMonth = "'19000101'";
            string whereToMonth = "'21001231'";

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

            string whereMonthClause = string.Join("", new[] { "(invoice.InvoiceDate >= ", whereFromMonth, " AND invoice.InvoiceDate <= ", whereToMonth, ")" }); // "deal.DealDate BETWEEN { ts '2008-12-20 00:00:00'} AND { ts '2008-12-20 23:59:59'}";
            List<string> wheres = new List<string>();
            wheres.Add(string.Join(" ", new[] { "WHERE", whereMonthClause, "AND deal.IsDeleted = 0", "AND", "deal.StateId", "=", stateId.ToString() }));
            wheres.Add(CreateWhereClause(tribeFilter, "AND ", "tribe.Id", "OR"));
            wheres.Add(CreateWhereClause(segmentFilter, "AND ", "segment.Id", "OR"));
            wheres.Add(CreateWhereClause(probabilityFilter, "AND ", "deal.Probability", "OR"));

            string joinSql6 = "";
            if (!rmFilter.Equals("0"))
            {
                CrmDealRole role = GetDealRole("rm");
                joinSql3 = "JOIN dbo.CrmDealInternalMembers AS member ON deal.Id = member.DealId";
                wheres.Add(string.Join(" ", new[] { "AND", "member.RoleId", "=", role.Id.ToString() }));
                wheres.Add(CreateWhereClause(rmFilter, "AND ", "member.UserId", "OR"));
            }
            string whereSql = string.Join(" ", wheres);
            string sql = string.Join(" ", new[] { selectSql, fromSql, joinSql1, joinSql2, joinSql3, joinSql3b, joinSql4, joinSql5, joinSql6, whereSql, orderBy });

            string preSql = "SELECT ISNULL(SUM(b.InvoiceAmount), 0) AS Amount FROM(";
            string postSql = ") as b";
            sql = string.Join("", new[] { preSql, sql, postSql });

            return _context.SummaryItems.FromSql(sql).FirstOrDefault<SummaryItem>();

        }

        private List<GenericInfo> GetAccess(List<PercentTribeResponse> rms)
        {
            List<GenericInfo> managers = new List<GenericInfo>();

            foreach (PercentTribeResponse rm in rms)
            {
                GenericInfo manager = GetManager(rm.Id);
                if (manager != null)
                {
                    if (!managers.Any(a => a.Id == manager.Id))
                    {
                        managers.Add(manager);
                    }
                }
            }

            List<GenericInfo> adds = new List<GenericInfo>();

            foreach (GenericInfo man in managers)
            {
                GenericInfo manager = GetManager(man.Id);
                if (manager != null)
                {
                    if (!managers.Any(a => a.Id == manager.Id))
                    {
                        adds.Add(manager);
                    }
                }
            }

            foreach (GenericInfo a in adds)
            {
                managers.Add(a);
            }

            return managers;
        }

        private GenericInfo GetManager(int rmUserId)
        {
            var query = from rm in _context.CrmRelManagers
                        where rm.UserId == rmUserId
                        select rm.LeaderId;
            int leaderId = query.FirstOrDefault();
            if (leaderId != 0)
            {
                var q2 = from user in _context.Users
                         where user.ID == leaderId
                         select new GenericInfo()
                         {
                             Id = user.ID,
                             Text = user.FirstName
                         };
                GenericInfo leader = q2.FirstOrDefault();
                return leader;
            }
            return null;
        }
        private GenericInfo GetInternalMember(int dealId, String role)
        {
            CrmDealRole dealRole = GetDealRole(role);
            if (dealRole != null)
            {
                var query = from member in _context.CrmDealInternalMembers
                            join user in _context.Users
                            on member.UserId equals user.ID
                            where member.DealId == dealId && member.RoleId == dealRole.Id && !member.IsDeleted
                            select new GenericInfo()
                            {
                                Id = user.ID,
                                Text = user.FirstName
                            };

                return query.FirstOrDefault();
            }

            return new GenericInfo()
            {
                Id = 0,
                Text = "-"
            };
        }
        private async Task<List<PercentTribeResponse>> GetInternalMembers(int dealId, int roleId)
        {
            var query = from member in _context.CrmDealInternalMembers
                        join user in _context.Users
                        on member.UserId equals user.ID
                        where member.DealId == dealId && member.RoleId == roleId && !member.IsDeleted
                        select new PercentTribeResponse()
                        {
                            Id = user.ID,
                            Percent = member.Percentage,
                            UsePercent = member.UsePercent,
                            Nominal = member.Nominal,
                            Text = user.FirstName
                        };

            return await query.ToListAsync();
        }

        private async Task<List<PercentTribeResponse>> GetTribeMembers(int dealId)
        {
            var query = from tribe in _context.CrmDealTribes
                        join t in _context.CoreTribes
                        on tribe.TribeId equals t.Id
                        where tribe.DealId == dealId && !tribe.IsDeleted
                        select new PercentTribeResponse()
                        {
                            Id = t.Id,
                            Percent = tribe.Percentage,
                            Text = t.Tribe,
                            UsePercent = tribe.UsePercent,
                            Nominal = tribe.Nominal
                        };

            return await query.ToListAsync();
        }

        private GenericInfo GetInvoiceContact(int invoiceId)
        {
            CrmDealHistoryType type = GetDealHistoryType("invoiced");
            if (type == null) return null;

            CrmDealHistory history = _context.CrmDealHistories.Where(a => a.CurData.Equals(invoiceId.ToString()) && a.TypeId == type.Id).FirstOrDefault();
            if (history != null && history.ReservedId > 0)
            {
                CrmContact contact = _context.CrmContacts.Find(history.ReservedId);
                if (contact != null)
                {
                    return new GenericInfo()
                    {
                        Id = contact.Id,
                        Text = contact.Name
                    };
                }
            }

            return null;
        }

        private async Task<int> addOrUpdatePIC(int dealId, int picId, int userId, DateTime now)
        {
            CrmDealRole picRole = GetDealRole("pic");
            if (picRole != null)
            {
                CrmDealInternalMember curPic = _context.CrmDealInternalMembers.Where(a => a.DealId == dealId && a.RoleId == picRole.Id && !a.IsDeleted).FirstOrDefault();
                if (curPic == null && picId != 0)
                {
                    CrmDealInternalMember member = new CrmDealInternalMember()
                    {
                        DealId = dealId,
                        RoleId = picRole.Id,
                        UserId = picId,
                        Percentage = 100,
                        UsePercent = true,
                        Nominal = 0,
                        CreatedDate = now,
                        CreatedBy = userId,
                        LastUpdated = now,
                        LastUpdatedBy = userId,
                        IsDeleted = false
                    };
                    _context.CrmDealInternalMembers.Add(member);
                }
                else if (curPic != null && curPic.UserId != picId)
                {
                    if (picId == 0)
                    {
                        // Remove
                        _context.CrmDealInternalMembers.Remove(curPic);
                    }
                    else
                    {
                        curPic.UserId = picId;
                        curPic.LastUpdated = now;
                        curPic.LastUpdatedBy = userId;
                        _context.Entry(curPic).State = EntityState.Modified;
                    }
                }

            }
            return await _context.SaveChangesAsync();
        }

        private async Task<List<EmailAddress>> GetCrmAdmins()
        {
            var query = from admin in _context.CrmAdmins
                        join user in _context.Users
                        on admin.UserId equals user.ID
                        where admin.Active && !admin.IsDeleted
                        select new EmailAddress()
                        {
                            Name = user.FirstName,
                            Address = user.UserName
                        };
            return await query.ToListAsync();
        }
        private string JoinStrings(List<string> strs)
        {
            string s = "";
            foreach (string str in strs)
            {
                s += "<p>" + str + "</p>";
            }
            return s;
        }
        private async Task SendEmail(int emailToSend, CrmDeal deal, CrmDealInvoice invoice)
        {
            List<EmailAddress> tos = await GetCrmAdmins();

            if (tos == null || tos.Count == 0) return;

            string subject = "";
            string content = "";

            string dealName = deal.Name;
            string company = _context.CrmClients.Where(a => a.Id == deal.ClientId).Select(a => a.Company).FirstOrDefault();
            string invoiceDate = invoice.InvoiceDate.ToString("dd MMM yyyy");

            if (emailToSend == EMAIL_ADD_TO_BE_INVOICED)
            {
                subject = "Penambahan To Be Invoiced";
            }
            else if (emailToSend == EMAIL_EDIT_TO_BE_INVOICED)
            {
                subject = "Perubahan To Be Invoiced";
            }
            else if (emailToSend == EMAIL_DELETE_TO_BE_INVOICED)
            {
                subject = "To Be Invoiced Dihapus";
            }

            List<string> strs = new List<string>(new[]
                {
                    subject,
                    "",
                    @"Project/workshop: " + dealName,
                    "Klien: " + company,
                    "Tanggal: " + invoiceDate,
                    "Jumlah: " + invoice.Amount.ToString()
                });

            content = JoinStrings(strs);
            if (!String.IsNullOrEmpty(subject) && !String.IsNullOrEmpty(content))
            {
                await SendEmail(tos, subject, content);
            }
        }

        private async Task SendEmail(List<EmailAddress> tos, string subject, string content)
        {
            EmailMessage message = new EmailMessage();

            List<EmailAddress> senders = new List<EmailAddress>();
            senders.Add(new EmailAddress()
            {
                Name = "Web Admin",
                Address = "cs@gmlperformance.co.id"
            });

            message.FromAddresses = senders;
            message.ToAddresses = tos;

            message.Subject = subject;
            message.Content = content;

            _emailService.Send(message);

        }

        private DateTime? GetStatusDate(int dealId)
        {
            CrmDealStatus status = _context.CrmDealStatuses.Where(a => a.DealId == dealId && !a.IsDeleted).OrderByDescending(a => a.LastUpdated).FirstOrDefault();
            if (status == null) return null;

            return status.LastUpdated;
        }

        /**
         * @api {post} /pipeline/status POST status
         * @apiVersion 1.0.0
         * @apiName PostStatus
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "id": 0,
         *     "dealId": 1316,
         *     "Status": "Proposal sudah dikirimkan",
         *     "userId": 12
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   {
         *     "id": 1,
         *     "dealId": 1316,
         *     "status": "Proposal sudah dikirimkan",
         *     "createdDate": "2024-01-21T13:21:29.0695966+07:00",
         *     "createdBy": 12,
         *     "lastUpdated": "2024-01-21T13:21:29.0695966+07:00",
         *     "lastUpdatedBy": 12,
         *     "isDeleted": false,
         *     "deletedBy": 0,
         *     "deletedDate": "1970-01-01T00:00:00"
         * }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPost("status")]
        public async Task<ActionResult<CrmDealStatus>> PostStatus(StatusItem request)
        {
            DateTime now = DateTime.Now;
            CrmDealStatus status = new CrmDealStatus()
            {
                DealId = request.DealId,
                Status = request.Status,
                CreatedBy = request.UserId,
                CreatedDate = now,
                LastUpdatedBy = request.UserId,
                LastUpdated = now,
                IsDeleted = false,
                DeletedDate = new DateTime(1970, 1, 1),
            };
            _context.CrmDealStatuses.Add(status);
            await _context.SaveChangesAsync();
            return status;
        }


        /**
         * @api {put} /pipeline/status/{id} PUT status
         * @apiVersion 1.0.0
         * @apiName PutStatus
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * @apiParam {Number} id        Id dari status yang bersangkutan, sama dengan id di request
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "id": 2,
         *     "dealId": 1316,
         *     "Status": "Proposal sudah dikirimkan lagi 2 kali",
         *     "userId": 12
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   {
         *     "id": 2,
         *     "dealId": 1316,
         *     "status": "Proposal sudah dikirimkan lagi 2 kali",
         *     "createdDate": "2024-01-21T13:21:29.0695966+07:00",
         *     "createdBy": 12,
         *     "lastUpdated": "2024-01-21T13:21:29.0695966+07:00",
         *     "lastUpdatedBy": 12,
         *     "isDeleted": false,
         *     "deletedBy": 0,
         *     "deletedDate": "1970-01-01T00:00:00"
         * }
         * 
         * @apiError NotAuthorized Token salah.
         */
        [Authorize(Policy = "ApiUser")]
        [HttpPut("status/{id}")]
        public async Task<ActionResult<CrmDealStatus>> PutStatus(int id, StatusItem request)
        {
            CrmDealStatus status = _context.CrmDealStatuses.Where(a => a.Id == id && !a.IsDeleted).FirstOrDefault();
            if (status == null) return NotFound();

            DateTime now = DateTime.Now;
            status.Status = request.Status;
            status.DealId = request.DealId;
            status.LastUpdated = now;
            status.LastUpdatedBy = request.UserId;

            _context.Entry(status).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return status;
        }

        /**
         * @api {delete} /pipeline/status/{statusId}/{userId} DELETE status
         * @apiVersion 1.0.0
         * @apiName DeleteStatus
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiParam {statusId} statusId    Id dari status yang ingin dihapus
         * @apiParam {Number} userId        Id dari user yang sedang login
         * 
         * @apiSuccessExample Success-Response:
         *   {
         *     "id": 2,
         *     "dealId": 1316,
         *     "status": "Proposal sudah dikirimkan lagi 2 kali",
         *     "createdDate": "2024-01-21T13:24:08.9943594",
         *     "createdBy": 12,
         *     "lastUpdated": "2024-01-21T13:38:31.5608833",
         *     "lastUpdatedBy": 12,
         *     "isDeleted": true,
         *     "deletedBy": 12,
         *     "deletedDate": "2024-01-21T13:43:07.8033201+07:00"
         * }
         * 
         */
        [Authorize(Policy = "ApiUser")]
        [HttpDelete("status/{statusId}/{userId}")]
        public async Task<ActionResult<CrmDealStatus>> DeleteStatus(int statusId, int userId)
        {
            CrmDealStatus status = _context.CrmDealStatuses.Where(a => a.Id == statusId && !a.IsDeleted).FirstOrDefault();
            if (status == null) return NotFound();

            status.IsDeleted = true;
            status.DeletedBy = userId;
            status.DeletedDate = DateTime.Now;

            _context.Entry(status).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return status;

        }

        /**
         * @api {get} /pipeline/status/{dealId} GET list status
         * @apiVersion 1.0.0
         * @apiName GetStatusList
         * @apiGroup Pipeline
         * @apiPermission ApiUser
         * 
         * @apiSuccessExample Success-Response:
         * [
         *     {
         *         "id": 1,
         *         "status": "Proposal sudah dikirimkan",
         *         "userId": 12,
         *         "firstname": "Irfan Mahfuzi",
         *         "lastUpdated": "2024-01-21T13:23:18.0152678"
         *     }
         * ]
         * 
         */
        [Authorize(Policy = "ApiUser")]
        [HttpGet("status/{dealId}")]
        public async Task<ActionResult<List<StatusListItem>>> GetStatusList(int dealId)
        {
            var query = from s in _context.CrmDealStatuses
                        join u in _context.Users on s.LastUpdatedBy equals u.ID
                        where s.DealId == dealId && !s.IsDeleted
                        orderby s.LastUpdated descending
                        select new StatusListItem()
                        {
                            Id = s.Id,
                            Status = s.Status,
                            UserId = s.LastUpdatedBy,
                            Firstname = u.FirstName,
                            LastUpdated = s.LastUpdated
                        };
            return await query.ToListAsync();
        }


    }
}