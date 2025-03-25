using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class AchievementSheetByTribe
    {
        public string Title { get; set; }
        public string Period { get; set; }
        public List<AchievementItemByTribe> Items { get; set; }
    }
}
