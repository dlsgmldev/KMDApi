using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Crm
{
    public class Error
    {
        public string Code { get; }
        public string Description { get; }
        public Error(string code, string description)
        {
            Code = code;
            Description = description;
        }
    }
}
