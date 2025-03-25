using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Digital
{
    public class DigitalBarItem : IComparer<DigitalBarItem>
    {
        public string Indicator { get; set; }
        public List<double> Values { get; set; }

        //SortBySalaryByAscendingOrder
        public int Compare(DigitalBarItem x, DigitalBarItem y)
        {
            if (x.Values == null || x.Values.Count() == 0) return 1;
            if (y.Values == null || y.Values.Count() == 0) return -1;

            if (x.Values[0] < y.Values[0]) return 1;
            else if (x.Values[0] > y.Values[0]) return -1;
            else return 0;
        }

        //SortBySalaryByDescendingOrder
        //public int Compare(Employee x, Employee y)
        //{
        //    if (x.Salary < y.Salary) return 1;
        //    else if (x.Salary > y.Salary) return -1;
        //    else return 0;
        //}
    }
}
