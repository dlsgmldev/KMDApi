using System;
using Web.Api.Core.Shared;



namespace Web.Api.Core.Domain.Entities
{
    public class ProfileImage : BaseEntity
    {
        public string FileURL { get; set; }
        public string FileName { get; set; }
        public bool IsDeleted { get; set; }

    }
}
