﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class CrmDealUserInvoice
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public long Amount { get; set; }
        public double Percentage { get; set; }
        public int UserId { get; set; }
        public bool UsePercent { get; set; }
    }
}
