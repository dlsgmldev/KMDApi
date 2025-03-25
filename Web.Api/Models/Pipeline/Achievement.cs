using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class Achievement
    {
        public AchievementSheetByTribe Sheet1 { get; set; }
        public AchievementSheet Sheet2 { get; set; }
        public DateTime GeneratedDate { get; set; }
    }
}
