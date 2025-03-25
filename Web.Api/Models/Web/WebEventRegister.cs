using KDMApi.Models.Crm;
using KDMApi.Models.Km;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Models.Web
{
    public class ListWebEventRegister
    {
        public List<WebEventRegisterItem> items { get; set; }
        public PaginationInfo info { get; set; }
    }

    public class WebEventRegisterItem
    {
        public int Id { get; set; }
        public GenericInfo Event { get; set; }
        public string Company { get; set; }
        public string ContactPerson { get; set; }
        public string Telephone { get; set; }
        public string Fax { get; set; }
        public string Handphone { get; set; }
        public string Email { get; set; }
        public int Payment { get; set; }
        public int Participants { get; set; }
        public DateTime RegistrationDate { get; set; }
        public bool Free { get; set; }
    }
    public class WebEventRegisterDetail
    {
        public int Id { get; set; }
        public WebEventResponse Event { get; set; }
        public string Company { get; set; }
        public string CompanyType { get; set; }
        public string StatusPPN { get; set; }
        public string NPWP { get; set; }
        public string Address { get; set; }
        public string ContactPerson { get; set; }
        public string Telephone { get; set; }
        public string Fax { get; set; }
        public string Handphone { get; set; }
        public string Email { get; set; }
        public string MailAddress { get; set; }
        public string City { get; set; }
        public string Kecamatan { get; set; }
        public string PostCode { get; set; }
        public string Voucher { get; set; }
        public string Reference { get; set; }
        public string JenisPelatihan { get; set; }
        public int Payment { get; set; }
        public List<WebEventPart> Participants { get; set; }
        public bool Free { get; set; }
        public string Signature { get; set; }
        public string SignatureURL { get; set; }
        public string KeteranganPembayaran { get; set; }
    }
    public class WebEventRegister
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public string Company { get; set; }
        public string CompanyType { get; set; }
        public string StatusPPN { get; set; }
        public string NPWP { get; set; }
        public string Address { get; set; }
        public string ContactPerson { get; set; }
        public string Telephone { get; set; }
        public string Fax { get; set; }
        public string Handphone { get; set; }
        public string Email { get; set; }
        public string MailAddress { get; set; }
        public string Kecamatan { get; set; }
        public string City { get; set; }
        public string PostCode { get; set; }
        public string Voucher { get; set; }
        public string Reference { get; set; }
        public string JenisPelatihan { get; set; }
        public string KeteranganPembayaran { get; set; }
        public List<WebEventPart> Participants { get; set; }
    }

    public class WebEventPart
    {
        public string Name { get; set; }
        public string JobTitle { get; set; }
        public string Department { get; set; }
        public string Handphone { get; set; }
        public string Email { get; set; }
        public String Gender { get; set; }
    }

    public class WebEventSignatureRequest
    {
        public int RegistrationId { get; set; }
        public string Image { get; set; }
        //public List<IFormFile> Image { get; set; }
    }

}
