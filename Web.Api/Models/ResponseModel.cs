using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace KDMApi.Models
{
    public class Response_Status 
    {
        public string code { get; set; }
        public string description { get; set; }
        public string access_token { get; set; }
    }

    public class Page_Info
    {
        public int current_page { get; set; }
        public bool has_next_page { get; set; }
        public Page_Count count { get; set; }
    }

    public class Page_Count
    {
        public int page { get; set; }
        public int record { get; set; }
    }

    public class Order
    {
        // 0=asc, 1=desc
        public int order_seq { get; set; }
        public int order_by { get; set; }
    }


    public class Post_Request
    {
        public int page_size { get; set; }
        public int page_number { get; set; }
        public string user_id { get; set; }
        public string source_id { get; set; }
        public string public_ip { get; set; }
        public Order order { get; set; }
        public string search_txt { get; set; }
        public string term { get; set; }
    }


    public class Delete_Request
    {
        public string user_id { get; set; }
        public string source_id { get; set; }
        public string public_ip { get; set; }
        public List<int> delete_list { get; set; }
        public string term { get; set; }
    }

    public class Add_Response
    {
        public Response_Status Response_status { get; set; }
        public int? id { get; set; }
    }
    public class Add_Response1
    {
        public Response_Status Response_status { get; set; }
        public int? id { get; set; }
        public int? typeid { get; set; }
    }
    public class Add_Response_url
    {
        public Response_Status Response_status { get; set; }
        public int? id { get; set; }
        public string FileURL { get; set; }

    }
    public class Upd_Response
    {
        public Response_Status Response_status { get; set; }
        public int? id { get; set; }
    }
}
