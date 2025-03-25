using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Pipeline
{
    public class ProjectionResponse
    {
        public List<MonthStageInfo> Stages { get; set; }
        public List<ProjectionItem> Projection { get; set; }
    }
}
