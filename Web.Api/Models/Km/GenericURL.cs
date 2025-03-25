using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Km
{
    public class GenericURL
    {
        public GenericURL()
        {
            Time = new DateTime(2100, 12, 31);
        }
        public int Id { get; set; }         
        public string Text { get; set; }
        public string URL { get; set; }
        public DateTime Time { get; set; }
    }
}
