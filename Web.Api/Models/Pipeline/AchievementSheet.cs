using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class AchievementSheet
    {
        public string Title { get; set; }
        public string Period { get; set; }
        public List<AchievementItem> Items { get; set; }
    }
}
