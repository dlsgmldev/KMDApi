using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Km
{
    public class GenericInfoString
    {
        public GenericInfoString(int id, string name)
        {
            Id = id;
            Name = name;
            Children = new List<string>();
        }
        public int Id { get; set; }
        public string Name { get; set; }
        public List<string> Children { get; set; }
    }
}
