﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class WebEventTestimony
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public string Testimony { get; set; }
        public string Name { get; set; }
        public string Company { get; set; }
        public string Title { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime LastUpdated { get; set; }
        public int LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public int DeletedBy { get; set; }
        public DateTime DeletedDate { get; set; }
    }
}
