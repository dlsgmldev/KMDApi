using KDMApi.Models.Km;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Web
{
    public class PostCdhxCategories
    {
        public int UserId { get; set; }
        public List<GenericString> Items { get; set; }
    }
}
