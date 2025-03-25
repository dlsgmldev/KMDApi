using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Km
{
    public class TreeNode
    {
        public TreeNode(int id, string name)
        {
            Id = id;
            Text = name;
            children = new List<TreeNode>();
        }
        public int Id { get; set; }
        public string Text { get; set; }
        public List<TreeNode> children { get; set; }
    }
}
