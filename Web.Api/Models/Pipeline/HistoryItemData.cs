using KDMApi.Models.Km;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class HistoryItemData
    {
        public HistoryItemData()
        {
            Header1 = new GenericInfo();
            Header2 = new GenericInfo();
            Header3 = new GenericInfo();
            UpdateBy = new GenericInfo();
            Info = new { };
        }
        public GenericInfo Header1 { get; set; }
        public GenericInfo Header2 { get; set; }
        public GenericInfo Header3 { get; set; }
        public DateTime UpdateTime { get; set; }
        public GenericInfo UpdateBy { get; set; }
        public Object Info { get; set; }

    }
}
