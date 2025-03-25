using KDMApi.DataContexts;
using KDMApi.Models;
using KDMApi.Models.Crm;
using KDMApi.Models.Pipeline;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Services
{
    public class CrmReportService
    {
        private DefaultContext _context;

        public CrmReportService(DefaultContext context)
        {
            _context = context;
        }

        public long GetActualSalesByUserId(int userId, int fromMonth, int toMonth, int year)
        {
            var query = from userInvoice in _context.CrmDealUserInvoices
                        join invoice in _context.CrmDealInvoices on userInvoice.InvoiceId equals invoice.Id
                        join per in _context.CrmPeriods on invoice.PeriodId equals per.Id
                        where userInvoice.UserId == userId && per.Month >= fromMonth && per.Month <= toMonth && per.Year == year && !invoice.IsDeleted
                        select userInvoice.Amount;
            return query.Sum();
        }

        public long GetActualSalesByUserIdByDate(int userId, DateTime fromDate, DateTime toDate)
        {
            var query = from userInvoice in _context.CrmDealUserInvoices
                        join invoice in _context.CrmDealInvoices on userInvoice.InvoiceId equals invoice.Id
                        where userInvoice.UserId == userId && invoice.InvoiceDate >= fromDate && invoice.InvoiceDate <= toDate && !invoice.IsDeleted
                        select userInvoice.Amount;
            return query.Sum();
        }

        public async Task<long> GetActualSalesByUserIdFilterTribe(int userId, int fromMonth, int toMonth, int year, List<int> tribeIds)
        {
            if (tribeIds == null || tribeIds.Count() == 0) return GetActualSalesByUserId(userId, fromMonth, toMonth, year);

            var query = from userInvoice in _context.CrmDealUserInvoices
                        join invoice in _context.CrmDealInvoices on userInvoice.InvoiceId equals invoice.Id
                        join per in _context.CrmPeriods on invoice.PeriodId equals per.Id
                        where userInvoice.UserId == userId && per.Month >= fromMonth && per.Month <= toMonth && per.Year == year && !invoice.IsDeleted
                        select new
                        {
                            invoice.Id,
                            userInvoice.Amount
                        };
            var objs = await query.ToListAsync();
            long total = 0;
            foreach (var obj in objs)
            {
                var q = from invoice in _context.CrmDealTribeInvoices
                        where invoice.InvoiceId == obj.Id && tribeIds.Contains(invoice.TribeId)
                        select invoice.Id;
                if (q.Count() > 0) total += obj.Amount;
            }

            return total;
        }

        public async Task<long> GetActualSalesByUserIdFilterTribeByDate(int userId, DateTime fromDate, DateTime toDate, List<int> tribeIds)
        {
            if (tribeIds == null || tribeIds.Count() == 0) return GetActualSalesByUserIdByDate(userId, fromDate, toDate);

            var query = from userInvoice in _context.CrmDealUserInvoices
                        join invoice in _context.CrmDealInvoices on userInvoice.InvoiceId equals invoice.Id
                        where userInvoice.UserId == userId && invoice.InvoiceDate >= fromDate && invoice.InvoiceDate <= toDate && !invoice.IsDeleted
                        select new
                        {
                            invoice.Id,
                            userInvoice.Amount
                        };
            var objs = await query.ToListAsync();
            long total = 0;
            foreach (var obj in objs)
            {
                var q = from invoice in _context.CrmDealTribeInvoices
                        where invoice.InvoiceId == obj.Id && tribeIds.Contains(invoice.TribeId)
                        select invoice.Id;
                if (q.Count() > 0) total += obj.Amount;
            }

            return total;
        }


        public async Task<List<VisitByTribe>> GetActualVisitsByTribe(int tribeId, int fromMonth, int toMonth, int year)
        {
            DateTime fromDate = new DateTime(year, fromMonth, 1);
            int nextMonth = toMonth + 1;
            int nextYear = year;
            if (nextMonth == 13)
            {
                nextYear++;
                nextMonth = 1;
            }
            DateTime toDate = new DateTime(nextYear, nextMonth, 1);

            string sql = "SELECT count(visit.Id) as Visit, u.Id, u.FirstName, tribe.Id as TribeId " +
                          "FROM CrmDealVisits as visit " +
                          "join CrmDealVisitTribes as vt on visit.Id = vt.VisitId " +
                          "join CoreTribes as tribe on vt.TribeId = tribe.Id " +
                          "join CrmDealVisitUsers as vu on visit.Id = vu.VisitId " +
                          "join Users as u on vu.Userid = u.Id " +
                          "where VisitFromtime >= '" + fromDate.ToString("yyyy-MM-dd") + "' and VisitToTime < '" + toDate.ToString("yyyy-MM-dd") + "' and tribe.Id = " + tribeId.ToString() +
                          " and visit.IsDeleted = 0 and vu.IsRm = 1 and vu.IsConsultant = 0 " +
                          "group by u.Id, u.FirstName, tribe.Id";

            List<VisitByTribe> visits = await _context.VisitByTribes.FromSql(sql).ToListAsync();
            return visits;
        }

        // Harus dibuat karena ada visit yang tanpa tribe
        public async Task<List<VisitByTribe>> GetActualVisitsNoTribe(int fromMonth, int toMonth, int year)
        {
            DateTime fromDate = new DateTime(year, fromMonth, 1);
            int nextMonth = toMonth + 1;
            int nextYear = year;
            if (nextMonth == 13)
            {
                nextYear++;
                nextMonth = 1;
            }
            DateTime toDate = new DateTime(nextYear, nextMonth, 1);

            string sql = "SELECT count(visit.Id) as Visit, u.Id, u.FirstName, " +
                          "ISNULL(vt.TribeId, 0) AS TribeId " +
                          "FROM CrmDealVisits as visit " +
                          "join CrmDealVisitUsers as vu on visit.Id = vu.VisitId " +
                          "join Users as u on vu.Userid = u.Id " +
                          "full outer join CrmDealVisitTribes as vt on visit.Id = vt.VisitId " +
                          "where VisitFromtime >= '" + fromDate.ToString("yyyy-MM-dd") + "' and VisitToTime < '" + toDate.ToString("yyyy-MM-dd") + "' and vt.TribeId IS NULL " +
                          " and vu.IsRm = 1 and vu.IsConsultant = 0 " +
                          "group by u.Id, u.FirstName, vt.TribeId";

            List<VisitByTribe> visits = await _context.VisitByTribes.FromSql(sql).ToListAsync();
            return visits;
        }

        public async Task<IndividualExportProposal> GetListProposalByUserId(String role, int userId, int fromMonth, int toMonth, int year, int page, int perPage, string search)
        {
            List<string> roles = new List<string>();
            roles.Add(role);
            return await GetListProposalByUserIdRoles(roles, userId, fromMonth, toMonth, year, page, perPage, search);
        }

        public async Task<IndividualExportProposalItemResponse> GetIndividualExportProposalItemsByDate(List<string> roles, int userId, DateTime fr, DateTime to, int page, int perPage, string search)
        {
            IndividualExportProposalItemResponse response = new IndividualExportProposalItemResponse();
            List<int> rmIds = await _context.CrmDealRoles.Where(a => roles.Contains(a.Shortname)).Select(a => a.Id).ToListAsync();

            IQueryable<IndividualExportProposalItem> query;

            if (search.Equals("*"))
            {
                query = from proposal in _context.CrmDealProposals
                        join deal in _context.CrmDeals
                        on proposal.DealId equals deal.Id
                        join cl in _context.CrmClients
                        on deal.ClientId equals cl.Id
                        join t in _context.CrmDealProposalTypes
                        on proposal.TypeId equals t.Id
                        join u in _context.Users
                        on proposal.SentById equals u.ID
                        join member in _context.CrmDealInternalMembers
                        on deal.Id equals member.DealId
                        where member.UserId == userId && rmIds.Contains(member.RoleId) && proposal.SentDate >= fr && proposal.SentDate <= to
                        && !proposal.IsDeleted && !deal.IsDeleted
                        orderby proposal.SentDate
                        select new IndividualExportProposalItem()
                        {
                            No = 0,
                            ProposalId = proposal.Id,
                            ProposalValue = Convert.ToInt64(Math.Round(member.Percentage * proposal.ProposalValue / 100)),
                            Name = deal.Name,
                            Client = cl.Company,
                            Type = t.Name,
                            SentBy = u.FirstName,
                            SentDate = proposal.SentDate,
                            SentById = proposal.SentById,
                            Filename = proposal.OriginalFilename,
                            LastUpdated = proposal.LastUpdated
                        };

            }
            else
            {
                query = from proposal in _context.CrmDealProposals
                        join deal in _context.CrmDeals
                        on proposal.DealId equals deal.Id
                        join cl in _context.CrmClients
                        on deal.ClientId equals cl.Id
                        join t in _context.CrmDealProposalTypes
                        on proposal.TypeId equals t.Id
                        join u in _context.Users
                        on proposal.SentById equals u.ID
                        join member in _context.CrmDealInternalMembers
                        on deal.Id equals member.DealId
                        where member.UserId == userId && rmIds.Contains(member.RoleId) && deal.Name.Contains(search) && proposal.SentDate >= fr && proposal.SentDate <= to
                        && !proposal.IsDeleted && !deal.IsDeleted
                        orderby proposal.SentDate
                        select new IndividualExportProposalItem()
                        {
                            No = 0,
                            ProposalId = proposal.Id,
                            ProposalValue = Convert.ToInt64(Math.Round(member.Percentage * proposal.ProposalValue / 100)),
                            Name = deal.Name,
                            Client = cl.Company,
                            Type = t.Name,
                            SentBy = u.FirstName,
                            SentDate = proposal.SentDate,
                            SentById = proposal.SentById,
                            Filename = proposal.OriginalFilename,
                            LastUpdated = proposal.LastUpdated
                        };

            }


            response.Total = query.Distinct().Count();

            if (page != 0 && perPage != 0)
            {
                response.Items = await query.Distinct().Skip(perPage * (page - 1)).Take(perPage).ToListAsync();
            }
            else
            {
                response.Items = await query.Distinct().ToListAsync();
            }

            return response;
        }


        public async Task<IndividualExportProposalItemResponse> GetIndividualExportProposalItems(List<string> roles, int userId, int fromMonth, int toMonth, int year, int page, int perPage, string search)
        {
            IndividualExportProposalItemResponse response = new IndividualExportProposalItemResponse();
            List<int> rmIds = await _context.CrmDealRoles.Where(a => roles.Contains(a.Shortname)).Select(a => a.Id).ToListAsync();

            IQueryable<IndividualExportProposalItem> query;

            if (search.Equals("*"))
            {
                query = from proposal in _context.CrmDealProposals
                        join deal in _context.CrmDeals
                        on proposal.DealId equals deal.Id
                        join cl in _context.CrmClients
                        on deal.ClientId equals cl.Id
                        join t in _context.CrmDealProposalTypes
                        on proposal.TypeId equals t.Id
                        join u in _context.Users
                        on proposal.SentById equals u.ID
                        join member in _context.CrmDealInternalMembers
                        on deal.Id equals member.DealId
                        join p in _context.CrmPeriods
                        on proposal.PeriodId equals p.Id
                        where member.UserId == userId && rmIds.Contains(member.RoleId) && p.Year == year && p.Month >= fromMonth && p.Month <= toMonth
                        && !proposal.IsDeleted && !deal.IsDeleted
                        orderby proposal.SentDate
                        select new IndividualExportProposalItem()
                        {
                            No = 0,
                            ProposalId = proposal.Id,
                            ProposalValue = Convert.ToInt64(Math.Round(member.Percentage * proposal.ProposalValue / 100)),
                            Name = deal.Name,
                            Client = cl.Company,
                            Type = t.Name,
                            SentBy = u.FirstName,
                            SentDate = proposal.SentDate,
                            SentById = proposal.SentById,
                            Filename = proposal.OriginalFilename,
                            LastUpdated = proposal.LastUpdated
                        };

            }
            else
            {
                query = from proposal in _context.CrmDealProposals
                        join deal in _context.CrmDeals
                        on proposal.DealId equals deal.Id
                        join cl in _context.CrmClients
                        on deal.ClientId equals cl.Id
                        join t in _context.CrmDealProposalTypes
                        on proposal.TypeId equals t.Id
                        join u in _context.Users
                        on proposal.SentById equals u.ID
                        join member in _context.CrmDealInternalMembers
                        on deal.Id equals member.DealId
                        join p in _context.CrmPeriods
                        on proposal.PeriodId equals p.Id
                        where member.UserId == userId && rmIds.Contains(member.RoleId) && p.Year == year && deal.Name.Contains(search) && p.Month >= fromMonth && p.Month <= toMonth
                        && !proposal.IsDeleted && !deal.IsDeleted
                        orderby proposal.SentDate
                        select new IndividualExportProposalItem()
                        {
                            No = 0,
                            ProposalId = proposal.Id,
                            ProposalValue = Convert.ToInt64(Math.Round(member.Percentage * proposal.ProposalValue / 100)),
                            Name = deal.Name,
                            Client = cl.Company,
                            Type = t.Name,
                            SentBy = u.FirstName,
                            SentDate = proposal.SentDate,
                            SentById = proposal.SentById,
                            Filename = proposal.OriginalFilename,
                            LastUpdated = proposal.LastUpdated
                        };

            }


            response.Total = query.Distinct().Count();

            if (page != 0 && perPage != 0)
            {
                response.Items = await query.Distinct().Skip(perPage * (page - 1)).Take(perPage).ToListAsync();
            }
            else
            {
                response.Items = await query.Distinct().ToListAsync();
            }

            return response;
        }
        public async Task<IndividualExportProposal> GetListProposalByUserIdRoles(List<string> roles, int userId, int fromMonth, int toMonth, int year, int page, int perPage, string search)
        {
            IndividualExportProposalItemResponse r = await GetIndividualExportProposalItems(roles, userId, fromMonth, toMonth, year, page, perPage, search);

            IndividualExportProposal response = new IndividualExportProposal();

            response.Items = new List<IndividualExportProposalItem>();
            int n = 1;
            foreach (IndividualExportProposalItem item in r.Items)
            {
                item.No = n++;
                response.Items.Add(item);
            }

            response.Headers = new List<string>(new string[] { "No.", "Proposal name", "Client", "Deal type", "Sent by", "Delivery date", "Proposal value", "LastUpdated" });
            response.Info = new PaginationInfo(page, perPage, r.Total);

            return response;
        }

        public async Task<IndividualExportVisit> GetListActualVisitsByUserId(int userId, int fromMonth, int toMonth, int year, int page, int perPage, string search)
        {
            DateTime fromDate = new DateTime(year, fromMonth, 1);
            int nextMonth = toMonth + 1;
            int nextYear = year;
            if (nextMonth == 13)
            {
                nextYear++;
                nextMonth = 1;
            }
            DateTime toDate = new DateTime(nextYear, nextMonth, 1);
            return await GetListActualVisitsByUserIdByDate(userId, fromDate, toDate, page, perPage, search);
        }

        public async Task<IndividualExportVisit> GetListActualVisitsByUserIdByDate(int userId, DateTime fromDate, DateTime toDate, int page, int perPage, string search)
        {
            string searchStr = "";
            if (!string.IsNullOrEmpty(search) && !search.Equals("*"))
            {
                searchStr = " and client.Company like '%" + search + "%' ";
            }
            string sql = "SELECT 1 as No, visit.Id as VisitId, visit.DealId, client.Id as ClientId, client.Company, visit.VisitFromTime as VisitDate, visit.Location, visit.Objective, visit.NextStep, visit.Remark as Remarks, visit.LastUpdated " +
                          "FROM CrmDealVisits as visit " +
                          "join CrmDealVisitUsers as vu on visit.Id = vu.VisitId " +
                          "join CrmClients as client on visit.ClientId = client.Id " +
                          "where visit.VisitFromTime >= '" + fromDate.ToString("yyyy-MM-dd") + "' and visit.VisitToTime < '" + toDate.ToString("yyyy-MM-dd") + "' and vu.UserId = " + userId.ToString() +
                          " and visit.IsDeleted = 0 and vu.IsRm = 1 and vu.IsConsultant = 0 " + searchStr +
                          " order by visit.VisitFromTime";

            List<IndividualExportVisitItem> items = await _context.IndividualExportVisitItems.FromSql(sql).ToListAsync();

            if (string.IsNullOrEmpty(search) || search.Equals("*"))
            {
                // Kadang-kadang visit ngga ada klien nya!
                string sql1 = "SELECT 1 as No, visit.Id as VisitId, visit.DealId, 0 as ClientId, '' as Company, visit.VisitFromTime as VisitDate, visit.Location, visit.Objective, visit.NextStep, visit.Remark as Remarks, visit.LastUpdated " +
                              "FROM CrmDealVisits as visit " +
                              "join CrmDealVisitUsers as vu on visit.Id = vu.VisitId " +
                              "where visit.ClientId = 0 and visit.VisitFromTime >= '" + fromDate.ToString("yyyy-MM-dd") + "' and visit.VisitToTime < '" + toDate.ToString("yyyy-MM-dd") + "' and vu.UserId = " + userId.ToString() +
                              " and visit.IsDeleted = 0 and vu.IsRm = 1 and vu.IsConsultant = 0 " +
                              " order by visit.VisitFromTime";

                items.AddRange(await _context.IndividualExportVisitItems.FromSql(sql1).ToListAsync());
            }
            items = items.OrderBy(a => a.VisitDate).ToList();

            int total = items.Count();

            if (page != 0 && perPage != 0)
            {
                items = items.Skip(perPage * (page - 1)).Take(perPage).ToList();
            }
            int n = 1;
            foreach (IndividualExportVisitItem item in items)
            {
                item.No = n++;
            }

            IndividualExportVisit response = new IndividualExportVisit();
            response.Headers = new List<string>(new string[] { "No", "Company", "Visit date", "Location", "Visit objective", "Next step", "Remarks", "LastUpdated" });
            response.Items = items;
            response.Info = new PaginationInfo(page, perPage, total);

            return response;
        }


    }
}
