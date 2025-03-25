using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class TeamAchievement
    {
        public int UserId { get; set; }         
        public string TeamName { get; set; }
        public List<TeamAchievementItem> Items { get; set; }
    }
}
