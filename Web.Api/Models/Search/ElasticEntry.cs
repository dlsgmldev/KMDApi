using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Search
{
    public class ElasticEntry
    {
        public int Id { get; set; }         // File.Id
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public int ClientId { get; set; }
        public string Client { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TribeId { get; set; }
        public bool IsDeliverables { get; set; } // false if pre-sales/proposal
        public string Filename { get; set; }
        public string FileType { get; set; }
        public string Content { get; set; }     // Content includes industry, company name, project name. 
                                                // If public seminar, company name is replaced with "public seminar" or "public workhshop"
        public DateTime LastUpdated { get; set; }
    }
}
