﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Helper
{
    public class EmailTemplate
    {
        public int Id { get; set; }
        public string Subject { get; set; }
        public string Text { get; set; }
    }
}
