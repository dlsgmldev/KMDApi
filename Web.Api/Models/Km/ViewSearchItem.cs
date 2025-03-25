using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Km
{
    public class ViewSearchItem
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public String ProjectName { get; set; }
        public int ClientId { get; set; }
        public String Client { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TribeId { get; set; }
        public Boolean Onegml { get; set; }
        public String Filename { get; set; }
        public String Filetype { get; set; }
        public String Content { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
