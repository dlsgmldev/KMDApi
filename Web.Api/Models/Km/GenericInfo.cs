using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Km
{
    public class GenericInfo
    {
        public GenericInfo()
        {
            Id = 0;
            Text = "";
        }
        public int Id { get; set; }
        public string Text { get; set; }
    }
}
