using KDMApi.Models.Km;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class HistoryItem
    {
        public HistoryItem()
        {
            Data = new HistoryItemData();
        }
        public string Type { get; set; }
        public HistoryItemData Data { get; set; }
    }
}
