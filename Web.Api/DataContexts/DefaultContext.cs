using Microsoft.EntityFrameworkCore;
using KDMApi.Models;
using KDMApi.Models.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KDMApi.Models.Pipeline;
using KDMApi.Models.Survey;
using KDMApi.Models.Digital;
using KDMApi.Models.Crm;
using KDMApi.Models.Km;

namespace KDMApi.DataContexts
{
    public class DefaultContext : DbContext
    {

        public DefaultContext(DbContextOptions<DefaultContext> options) : base(options)
        {
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<CrmClientRelManager>()
               .HasKey(c => new { c.CrmRelManagerId, c.CrmClientId });

            modelBuilder.Entity<KmProjectProduct>().HasKey(c => new { c.ProjectId, c.ProductId });
            modelBuilder.Entity<KmProjectInternalTeam>().HasKey(c => new { c.ProjectId, c.RoleId, c.UserId });
            modelBuilder.Entity<KmProjectExternalTeam>().HasKey(c => new { c.ProjectId, c.RoleId, c.ContactId });
            modelBuilder.Entity<CrmDealProposalSentContact>().HasKey(c => new { c.ProposalId, c.ContactId });
            modelBuilder.Entity<WebSurveyItemDimension>().HasKey(c => new { c.ItemId, c.DimensionId });
            modelBuilder.Entity<KmInsightAuthor>().HasKey(c => new { c.InsightId, c.UserId });
            modelBuilder.Entity<KmInsightCategory>().HasKey(c => new { c.InsightId, c.CategoryId });
            modelBuilder.Entity<WebEventParticipant>().HasKey(c => new { c.EventId, c.RoleId, c.ContactId });
            modelBuilder.Entity<WebEventSpeakerEvent>().HasKey(c => new { c.EventId, c.SpeakerId });
        }

        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Images> Images { get; set; }
        public DbSet<AspNetUser> AspNetUsers { get; set; }

        public DbSet<FileDirectory> FilesDirectory { get; set; }
        public DbSet<FileType> FilesType { get; set; }
        public DbSet<File> Files { get; set; }


        public DbSet<CoreRole> CoreRoles { get; set; }
        public DbSet<CrmClient> CrmClients { get; set; }
        public DbSet<CrmClientRelManager> CrmClientRelManagers { get; set; }
        public DbSet<CrmContact> CrmContacts { get; set; }
        public DbSet<CrmIndustry> CrmIndustries { get; set; }
        public DbSet<CrmRelManager> CrmRelManagers { get; set; }
        public DbSet<CrmSegment> CrmSegments { get; set; }
        public DbSet<CrmBranch> CrmBranches { get; set; }
        public DbSet<vProfileImage> vProfileImage { get; set; }

        public DbSet<CoreTribe> CoreTribes { get; set; }
        public DbSet<CorePlatform> CorePlatforms { get; set; }
        public DbSet<KmProduct> KmProducts { get; set; }
        public DbSet<KmProjectExternalTeam> KmProjectExternalTeams { get; set; }
        public DbSet<KmFile> KmFiles { get; set; }
        public DbSet<KmProjectProduct> KmProjectProducts { get; set; }
        public DbSet<KmProject> KmProjects { get; set; }
        public DbSet<KmProjectTeamRole> KmProjectTeamRoles { get; set; }
        public DbSet<KmProjectInternalTeam> KmProjectInternalTeams { get; set; }
        public DbSet<KmWorkshopType> KmWorkshopTypes { get; set; }
        public DbSet<KmYear> KmYears { get; set; }
        public DbSet<KmProjectAdditionalClient> KmProjectAdditionalClients { get; set; }
        public DbSet<KmActivityLog> KmActivityLogs { get; set; }
        public DbSet<KmPrepareView> KmPrepareViews { get; set; }
        public DbSet<KmInsight> KmInsights { get; set; }
        public DbSet<KmInsightAuthor> KmInsightAuthors { get; set; }
        public DbSet<KmInsightCategory> KmInsightCategories { get; set; }
        public DbSet<KmWebinarRootFolder> KmWebinarRootFolders { get; set; }
        public DbSet<KmWebinarFileFolder> KmWebinarFileFolders { get; set; }
        public DbSet<ViewSearchItem> ViewSearchItems { get; set; }

        public DbSet<WebContactRegister> WebContactRegisters { get; set; }
        public DbSet<WebCareer> WebCareers { get; set; }
        public DbSet<WebEvent> WebEvents { get; set; }
        public DbSet<WebEventDescription> WebEventDescriptions { get; set; }
        public DbSet<WebEventImage> WebEventImages { get; set; }
        public DbSet<WebEventFramework> WebEventFrameworks { get; set; }
        public DbSet<WebEventTestimony> WebEventTestimonies { get; set; }
        public DbSet<WebEventAgenda> WebEventAgendas { get; set; }
        public DbSet<WebEventImageCaption> WebEventImageCaptions { get; set; }
        public DbSet<WebEventInvestment> WebEventInvestments { get; set; }
        public DbSet<WebEventBrochure> WebEventBrochures { get; set; }
        public DbSet<WebEventFlyer> WebEventFlyers { get; set; }
        public DbSet<WebEventTakeaway> WebEventTakeaways { get; set; }
        public DbSet<WebEventCategory> WebEventCategories { get; set; }
        public DbSet<WebEventSpeaker> WebEventSpeakers { get; set; }
        public DbSet<WebEventSpeakerRecord> WebEventSpeakerRecords { get; set; }
        public DbSet<WebEventSpeakerEvent> WebEventSpeakerEvents { get; set; }
        public DbSet<WebEventNotification> WebEventNotifications { get; set; }
        public DbSet<WebPublicWorkshopCategory> WebPublicWorkshopCategories { get; set; }
        public DbSet<WebPublicWorkshop> WebPublicWorkshops { get; set; }
        public DbSet<WebPublicWorkshopEvent> WebPublicWorkshopEvents { get; set; }
        public DbSet<WebPublicWorkshopEventDate> WebPublicWorkshopEventDates { get; set; }
        public DbSet<WebEventCdhxCategory> WebEventCdhxCategories { get; set; }
        public DbSet<WebEventRegistration> WebEventRegistrations { get; set; }
        public DbSet<WebEventRegParticipant> WebEventRegParticipants { get; set; }
        public DbSet<WebEventRegPayment> WebEventRegPayments { get; set; }

        // CRM
        public DbSet<CrmDealRole> CrmDealRoles { get; set; }
        public DbSet<CrmDealHistoryType> CrmDealHistoryTypes { get; set; }
        public DbSet<CrmDealState> CrmDealStates { get; set; }
        public DbSet<CrmDealStage> CrmDealStages { get; set; }
        public DbSet<CrmDeal> CrmDeals { get; set; }
        public DbSet<CrmDealInternalMember> CrmDealInternalMembers { get; set; }
        public DbSet<CrmDealExternalMember> CrmDealExternalMembers { get; set; }
        public DbSet<CrmDealHistory> CrmDealHistories { get; set; }
        public DbSet<CrmDealVisit> CrmDealVisits { get; set; }
        public DbSet<CrmDealVisitContact> CrmDealVisitContacts { get; set; }
        public DbSet<CrmDealVisitUser> CrmDealVisitUsers { get; set; }
        public DbSet<CrmDealProposal> CrmDealProposals { get; set; }
        public DbSet<CrmDealProposalSentContact> CrmDealProposalSentContacts { get; set; }
        public DbSet<CrmDealProposalInvoice> CrmDealProposalInvoices { get; set; }
        public DbSet<CrmPeriod> CrmPeriods { get; set; }
        public DbSet<CrmDealProposalType> CrmDealProposalTypes { get; set; }
        public DbSet<CrmDealInvoice> CrmDealInvoices { get; set; }
        public DbSet<CrmDealTribe> CrmDealTribes { get; set; }
        public DbSet<CrmDealPNL> CrmDealPNLs { get; set; }
        public DbSet<CrmDealTribeInvoice> CrmDealTribeInvoices { get; set; }
        public DbSet<CrmDealUserInvoice> CrmDealUserInvoices { get; set; }
        public DbSet<CrmDealTarget> CrmDealTargets { get; set; }
        public DbSet<CrmKpi> CrmKpis { get; set; }
        public DbSet<CrmAdmin> CrmAdmins { get; set; }
        public DbSet<CrmTribeSalesHistory> CrmTribeSalesHistories { get; set; }
        public DbSet<CrmDealActualHistory> CrmDealActualHistories { get; set; }
        public DbSet<CrmDealStatus> CrmDealStatuses { get; set; }
        public DbQuery<IndividualExportVisitItem> IndividualExportVisitItems { get; set; }

        public DbSet<WebTopicCategory> WebTopicCategories { get; set; }
        public DbSet<WebSurvey> WebSurveys { get; set; }
        public DbSet<WebSurveyItemType> WebSurveyItemTypes { get; set; }
        public DbSet<WebSurveyRating> WebSurveyRatings { get; set; }
        public DbSet<WebSurveyRatingItem> WebSurveyRatingItems { get; set; }
        public DbSet<WebSurveyPage> WebSurveyPages { get; set; }
        public DbSet<WebSurveyItem> WebSurveyItems { get; set; }
        public DbSet<WebSurveyResponse> WebSurveyResponses { get; set; }
        public DbSet<WebSurveyDimension> WebSurveyDimensions { get; set; }
        public DbSet<WebSurveyItemDimension> WebSurveyItemDimensions { get; set; }
        public DbSet<WebSurveyOwner> WebSurveyOwners { get; set; }
        public DbSet<WebSurveyGroup> WebSurveyGroups { get; set; }
        public DbSet<WebImage> WebImages { get; set; }
        public DbSet<WebText> WebTexts { get; set; } 
        public DbSet<WebEventLocation> WebEventLocations { get; set; }
        public DbSet<WebEventHoliday> WebEventHolidays { get; set; }
        public DbSet<WebEventHolidayType> WebEventHolidayTypes { get; set; }
        public DbSet<CrmDealVisitTribe> CrmDealVisitTribes { get; set; }
       


        // Digital
        public DbSet<DigitalIndustry> DigitalIndustries { get; set; }

        public DbQuery<PipelineItem> PipelineItems { get; set; }
        public DbQuery<LostDealItem> LostDealItems { get; set; }
        public DbQuery<MonthStageInfo> MonthStageInfos { get; set; }
        public DbQuery<ProjectionItem> ProjectionItems { get; set; }
        public DbQuery<InvoiceItem> InvoiceItems { get; set; }
        public DbQuery<SummaryItem> SummaryItems { get; set; }
        public DbQuery<PublicWorkshopItem> PublicWorkshopItems { get; set; }
        public DbQuery<DoubleLong> GetDoubleLongs { get; set; }
        public DbQuery<SummaryReportRow> SummaryReportRows { get; set; }
        public DbQuery<ChartSeriesItem> ChartSeriesItems { get; set; }
        public DbQuery<IndicatorValue> IndicatorValues { get; set; }
        public DbQuery<SegmentSalesCallSummary> SegmentSalesCallSummaries { get; set; }
        public DbQuery<ResultItem> ResultItems { get; set; }
        public DbQuery<AchievementItem> AchievementItems { get; set; }
        public DbQuery<AchievementItemByTribe> AchievementItemByTribes { get; set; }
        public DbQuery<ReportItem> ReportItems { get; set; }
        public DbQuery<VisitByTribe> VisitByTribes { get; set; }
        // Forum
        public DbSet<WebEventParticipant> WebEventParticipants { get; set; }
        public DbSet<WebEventExpert> WebEventExperts { get; set; }
        public DbSet<WebEventVisitors> WebEventVisitors { get; set; }

        // Views
        public DbSet<ViewUserRM> ViewUserRMs { get; set; }
    }
}
