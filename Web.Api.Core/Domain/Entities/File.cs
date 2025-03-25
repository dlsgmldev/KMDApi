using System;
using Web.Api.Core.Shared;



namespace Web.Api.Core.Domain.Entities
{
   public class File : BaseEntity
    {
        public string URL { get; set; }

        public File(string URL)
         {

        }
    }
}
