﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class WebTopicCategory
    {
        public int Id { get; set; }
        public string Category { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime LastUpdated { get; set; }
        public int LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public int DeletedBy { get; set; }
        public DateTime DeletedDate { get; set; }

    }
}
