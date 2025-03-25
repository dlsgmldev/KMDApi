using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Survey
{
    public class TableRow : IComparable
    {
        public string Title { get; set; }
        public List<double> Data { get; set; }

        int IComparable.CompareTo(object obj)
        {
            TableRow c = (TableRow) obj;
            return String.Compare(this.Title, c.Title);
        }
    }
}
