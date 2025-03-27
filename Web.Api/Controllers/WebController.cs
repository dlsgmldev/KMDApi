using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using KDMApi.DataContexts;
using KDMApi.Models.Web;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using KDMApi.Models.Helper;
using Microsoft.AspNetCore.Cors;
using System.IO;
using KDMApi.Services;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Net.Imap;


namespace KDMApi.Controllers
{
    [Route("v1/web")]
    [ApiController]
    [EnableCors("QuBisaPolicy")]
    public class WebController : ControllerBase
    {
        private readonly DefaultContext _context;
        private readonly IEmailService _emailService;
        private DataOptions _options;
        private readonly FileService _fileService;
        public WebController(DefaultContext context, IEmailService emailService, Microsoft.Extensions.Options.IOptions<DataOptions> options, FileService fileService)
        {
            _context = context;
            _emailService = emailService;
            _options = options.Value;
            _fileService = fileService;
        }

        /**
         * @api {post} /web/register Registrasi dari pengunjung web
         * @apiVersion 1.0.0
         * @apiName RegisterFromWeb
         * @apiGroup Web
         * @apiPermission Basic authentication 
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "name": "Rita",
         *     "phoneNumber": "0817-8592-6893",
         *     "email": "rita@gmail.com",
         *     "companyName": "Terang Benderang",
         *     "jobTitle": "CEO",
         *     "department": "Operations",
         *     "message": "Halo ini cuma test aja",
         *     "action": "action:register event Train the Trainer",
         *     "voucher": ""
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   {
         *     "id": 1,
         *     "name": "Rita Tjio",
         *     "phoneNumber": "0817-859263",
         *     "email": "rita@gmail.com",
         *     "companyName": "Terang Benderang",
         *     "jobTitle": "CEO",
         *     "department": "Operations",
         *     "message": "Halo ini cuma test aja",
         *     "action": "register",
         *     "lastUpdated": "1970-01-01T00:00:00",
         *     "lastUpdatedBy": 0,
         *     "isDeleted": false,
         *     "deletedBy": 0,
         *     "deletedDate": "1970-01-01T00:00:00"
         *   }
         * 
         */
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult<WebContactRegister>> RegisterFromWeb(WebContactRegister request)
        {
            if (Request.Headers["Authorization"].ToString() != "" && Request.Headers["Authorization"].ToString().StartsWith("Basic "))
            {
                var authHeader = Request.Headers["Authorization"].ToString();
                authHeader = authHeader.Trim();
                string encodedCredentials = authHeader.Substring(6);
                var credentialBytes = Convert.FromBase64String(encodedCredentials);
                var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
                var username = credentials[0];
                var password = credentials[1];
                if (username == "onegmlapi" && password == "O1n6e0G4M7L")
                {
                    if (request.Voucher == null) request.Voucher = "";
                    request.LastUpdated = DateTime.Now;

                    _context.WebContactRegisters.Add(request);
                    await _context.SaveChangesAsync();

                    //if(_options.Environment.Equals("Production"))
                    //{
/*                        EmailMessage message = new EmailMessage();
                        List<EmailAddress> senders = new List<EmailAddress>();
                        senders.Add(new EmailAddress()
                        {
                            Name = "Web",
                            Address = "no-reply@knowcap.co.id"
                        });
*/
                        List<EmailAddress> recipients = new List<EmailAddress>();
                        //recipients.Add(new EmailAddress()
                        //{
                        //    Name = "Yenny",
                        //    Address = "yenny@knowcap.co.id"
                        //});
                        //recipients.Add(new EmailAddress()
                        //{
                        //    Name = "GML",
                        //    Address = "gml@knowcap.co.id"
                        //});

                        recipients.Add(new EmailAddress()
                        {
                            Name = "CS GML Performance Consulting",
                            Address = "admin@gmlperformance.co.id"
                        });
                    //    recipients.Add(new EmailAddress()
                    //    {
                    //        Name = "Lutan",
                    //        Address = "lutan@knowcap.co.id"
                    //    });
                    //recipients.Add(new EmailAddress()
                    //{
                    //    Name = "Knowcap",
                    //    Address = "kca@kreasiciptaasia.com"
                    //});
                    //recipients.Add(new EmailAddress()
                    //    {
                    //        Name = "Knowcap",
                    //        Address = "knowcap@knowcap.co.id"
                    //    });
                    //    recipients.Add(new EmailAddress()
                    //    {
                    //        Name = "GML",
                    //        Address = "gml@gmlperformance.co.id"
                    //    });


                    string subject = "URGENT: Contact dari web site GML";
                        string content = "<p>" + "URGENT: Contact dari web site GML" + "</p>";
                    content += "<p>" + "Nama: " + request.Name + "</p>";
                    content += "<p>" + "Nomor telepon: " + request.PhoneNumber + "</p>";
                    content += "<p>" + "Email: " + request.Email + "</p>";
                    content += "<p>" + "Perusahaan: " + request.CompanyName + "</p>";
                    content += "<p>" + "Jabatan: " + request.JobTitle + "</p>";
                    content += "<p>" + "Departement: " + request.Department + "</p>";
                    content += "<p>" + "Pesan: " + request.Message + "</p>";
                    content += "<p>" + "Action: " + request.Action + "</p>";
                    content += "<p>" + "Voucher: " + request.Voucher + "</p>";
                    content += "<p>" + "Reference From: " + request.ReferenceFrom + "</p>";

                    await SendEmail(recipients, subject, content);
                    //_emailService.Send(message);
                    //}

                    return request;
                }
            }

            return Unauthorized();
            
        }


        /**
         * @api {post} /web/register Registrasi dari pengunjung web
         * @apiVersion 1.0.0
         * @apiName RegisterFromWeb
         * @apiGroup Web
         * @apiPermission Basic authentication 
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "name": "Rita",
         *     "phoneNumber": "0817-8592-6893",
         *     "email": "rita@gmail.com",
         *     "companyName": "Terang Benderang",
         *     "jobTitle": "CEO",
         *     "department": "Operations",
         *     "message": "Halo ini cuma test aja",
         *     "action": "action:register event Train the Trainer",
         *     "voucher": ""
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   {
         *     "id": 1,
         *     "name": "Rita Tjio",
         *     "phoneNumber": "0817-859263",
         *     "email": "rita@gmail.com",
         *     "companyName": "Terang Benderang",
         *     "jobTitle": "CEO",
         *     "department": "Operations",
         *     "message": "Halo ini cuma test aja",
         *     "action": "register",
         *     "lastUpdated": "1970-01-01T00:00:00",
         *     "lastUpdatedBy": 0,
         *     "isDeleted": false,
         *     "deletedBy": 0,
         *     "deletedDate": "1970-01-01T00:00:00"
         *   }
         * 
         */
        [AllowAnonymous]
        [HttpPost("event/register")]
        public async Task<ActionResult<WebContactRegister>> RegisterEventFromWeb(WebContactRegister request)
        {
            if (Request.Headers["Authorization"].ToString() != "" && Request.Headers["Authorization"].ToString().StartsWith("Basic "))
            {
                var authHeader = Request.Headers["Authorization"].ToString();
                authHeader = authHeader.Trim();
                string encodedCredentials = authHeader.Substring(6);
                var credentialBytes = Convert.FromBase64String(encodedCredentials);
                var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
                var username = credentials[0];
                var password = credentials[1];
                if (username == "onegmlapi" && password == "O1n6e0G4M7L")
                {
                    if (request.Voucher == null) request.Voucher = "";
                    request.LastUpdated = DateTime.Now;

                    _context.WebContactRegisters.Add(request);
                    await _context.SaveChangesAsync();

                    //if(_options.Environment.Equals("Production"))
                    //{
                    List<EmailAddress> recipients = new List<EmailAddress>();
                    recipients.Add(new EmailAddress()
                    {
                        Name = "Yenny",
                        Address = "yenny@knowcap.co.id"
                    });
                    recipients.Add(new EmailAddress()
                    {
                        Name = "GML",
                        Address = "gml@knowcap.co.id"
                    });
                    recipients.Add(new EmailAddress()
                    {
                        Name = "Lutan",
                        Address = "lutan@knowcap.co.id"
                    });
                    recipients.Add(new EmailAddress()
                    {
                        Name = "Knowcap",
                        Address = "kca@kreasiciptaasia.com"
                    });
                    recipients.Add(new EmailAddress()
                    {
                        Name = "Knowcap",
                        Address = "knowcap@kreasiciptaasia.com"
                    });
                    recipients.Add(new EmailAddress()
                    {
                        Name = "Knowcap",
                        Address = "knowcap@knowcap.co.id"
                    });
                    recipients.Add(new EmailAddress()
                    {
                        Name = "GML",
                        Address = "gml@gmlperformance.co.id"
                    });

                    string subject = "URGENT: Contact dari web site GML";
                    string content = "<p>" + "URGENT: Contact dari web site GML" + "</p>";
                    content += "<p>" + "Nama: " + request.Name + "</p>";
                    content += "<p>" + "Nomor telepon: " + request.PhoneNumber + "</p>";
                    content += "<p>" + "Email: " + request.Email + "</p>";
                    content += "<p>" + "Perusahaan: " + request.CompanyName + "</p>";
                    content += "<p>" + "Jabatan: " + request.JobTitle + "</p>";
                    content += "<p>" + "Departement: " + request.Department + "</p>";
                    content += "<p>" + "Pesan: " + request.Message + "</p>";
                    content += "<p>" + "Action: " + request.Action + "</p>";
                    content  += "<p>" + "Voucher: " + request.Voucher + "</p>";
                    content += "<p>" + "Reference From: " + request.ReferenceFrom + "</p>";

                    await SendEmail(recipients, subject, content);
                    /*
                    recipients.Add(new EmailAddress()
                    {
                        Name = "GML",
                        Address = "rafdi@gmlperformance.co.id"
                    });
                    */
                    /*
                    message.FromAddresses = senders;
                    message.ToAddresses = recipients;
                    message.Subject = "URGENT: Contact dari web site GML";
                    message.Content = "<p>" + "URGENT: Contact dari web site GML" + "</p>";
                    message.Content += "<p>" + "Nama: " + request.Name + "</p>";
                    message.Content += "<p>" + "Nomor telepon: " + request.PhoneNumber + "</p>";
                    message.Content += "<p>" + "Email: " + request.Email + "</p>";
                    message.Content += "<p>" + "Perusahaan: " + request.CompanyName + "</p>";
                    message.Content += "<p>" + "Jabatan: " + request.JobTitle + "</p>";
                    message.Content += "<p>" + "Departement: " + request.Department + "</p>";
                    message.Content += "<p>" + "Pesan: " + request.Message + "</p>";
                    message.Content += "<p>" + "Action: " + request.Action + "</p>";
                    message.Content += "<p>" + "Voucher: " + request.Voucher + "</p>";
                    message.Content += "<p>" + "Reference From: " + request.ReferenceFrom + "</p>";

                    _emailService.Send(message);
                    //}
                    */
                    return request;
                }
            }

            return Unauthorized();

        }

        /**
         * @api {post} /web/ecatalog Download E-catalog
         * @apiVersion 1.0.0
         * @apiName DownloadEcatalog
         * @apiGroup Web
         * @apiPermission Basic authentication 
         * 
         * @apiParamExample {json} Request-Example:
         *   {
         *     "name": "Rita",
         *     "phoneNumber": "0817-8592-6893",
         *     "email": "rita@gmail.com",
         *     "companyName": "Terang Benderang",
         *     "jobTitle": "CEO",
         *     "department": "Operations",
         *     "message": "Halo ini cuma test aja",
         *     "action": "action:register event Train the Trainer",
         *     "voucher": ""
         *   }
         *   
         * @apiSuccessExample Success-Response:
         *   {
         *     "id": 1,
         *     "name": "Rita Tjio",
         *     "phoneNumber": "0817-859263",
         *     "email": "rita@gmail.com",
         *     "companyName": "Terang Benderang",
         *     "jobTitle": "CEO",
         *     "department": "Operations",
         *     "message": "Halo ini cuma test aja",
         *     "action": "register",
         *     "lastUpdated": "1970-01-01T00:00:00",
         *     "lastUpdatedBy": 0,
         *     "isDeleted": false,
         *     "deletedBy": 0,
         *     "deletedDate": "1970-01-01T00:00:00"
         *   }
         * 
         */
        [AllowAnonymous]
        [HttpPost("ecatalog")]
        public async Task<ActionResult<string>> DownloadEcatalog(List<int> sections)
        {
            if (Request.Headers["Authorization"].ToString() != "" && Request.Headers["Authorization"].ToString().StartsWith("Basic "))
            {
                var authHeader = Request.Headers["Authorization"].ToString();
                authHeader = authHeader.Trim();
                string encodedCredentials = authHeader.Substring(6);
                var credentialBytes = Convert.FromBase64String(encodedCredentials);
                var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
                var username = credentials[0];
                var password = credentials[1];
                if (username == "onegmlapi" && password == "O1n6e0G4M7L")
                {
                    if (sections == null || sections.Count() == 0) return BadRequest(new { error = "Invalid sections." });

                    string fn = "";

                    sections.Sort();

                    string url = "";
                    string source;
                    string target;
                    if (sections.Where(a => a == 0).Any())
                    {
                        fn = "OneGML_ECatalog.3.1.pdf";
                        source = Path.Combine(_options.DataRootDirectory, @"web", @"ecatalog", fn);
                        target = Path.Combine(_options.AssetsRootDirectory, @"web", @"ecatalog", fn);
                        url = CheckOrCreateAll(fn, source, target);
                    }
                    else
                    {
                        sections.ForEach(delegate (int section)
                        {
                            fn += section.ToString();
                        });

                        fn = @"OneGML_ECatalog.3.1_" + fn + @".pdf";
                        target = Path.Combine(_options.AssetsRootDirectory, @"web", @"ecatalog", fn);

                        url = CheckOrCreateSections(fn, sections, target);
                    }

                    if (url == null || url.Equals("")) return BadRequest(new { error = "Error creating PDF file." });

                    return url;
                }
            }

            return Unauthorized();

        }

        private async Task SendEmail(List<EmailAddress> tos, string subject, string content)
        {
            EmailMessage message = new EmailMessage();

            List<EmailAddress> senders = new List<EmailAddress>();
            senders.Add(new EmailAddress()
            {
                Name = "Web Admin",
                //Address = "admin1@gmlperformance.co.id"
                Address = "admin@gmlperformance.co.id"
            });

            List<EmailAddress> recipients = new List<EmailAddress>();
            foreach (EmailAddress recipient in tos)
            {
                recipients.Add(new EmailAddress()
                {
                    Name = recipient.Name,
                    Address = recipient.Address
                });
            }

            message.FromAddresses = senders;
            message.ToAddresses = recipients;

            message.Subject = subject;
            message.Content = content;

            _emailService.Send(message);

        }
        private string CheckOrCreateAll(string filename, string source, string target)
        {
            if (System.IO.File.Exists(target)) return GetECatalogURL(filename);
            if(_fileService.CopyFile(source, target) == 0)
            {
                return null;
            }
            if (System.IO.File.Exists(target)) return GetECatalogURL(filename);

            return null;
        }

        private string CheckOrCreateSections(string filename, List<int> sections, string target)
        {
            if (System.IO.File.Exists(target)) return GetECatalogURL(filename);

            List<string> cover = new List<string>(new[] { "1_OneGML_ECatalog.3.1.pdf" });
            List<string> foreword = new List<string>(new[] { "2_OneGML_ECatalog.3.1.pdf",
                                                            "3_OneGML_ECatalog.3.1.pdf",
                                                            "4_OneGML_ECatalog.3.1.pdf",
                                                            "5_OneGML_ECatalog.3.1.pdf",
                                                            "6_OneGML_ECatalog.3.1.pdf"
                                                             });
            List<string> workshops = new List<string>(new[] { "7_OneGML_ECatalog.3.1.pdf",
                                                            "8_OneGML_ECatalog.3.1.pdf",
                                                            "9_OneGML_ECatalog.3.1.pdf",
                                                            "10_OneGML_ECatalog.3.1.pdf",
                                                            "11_OneGML_ECatalog.3.1.pdf",
                                                            "12_OneGML_ECatalog.3.1.pdf",
                                                            "13_OneGML_ECatalog.3.1.pdf",
                                                            "14_OneGML_ECatalog.3.1.pdf"
                                                             });
            List<string> calendar = new List<string>(new[] { "15_OneGML_ECatalog.3.1.pdf",
                                                            "16_OneGML_ECatalog.3.1.pdf",
                                                            "17_OneGML_ECatalog.3.1.pdf",
                                                            "18_OneGML_ECatalog.3.1.pdf",
                                                            "19_OneGML_ECatalog.3.1.pdf",
                                                            "20_OneGML_ECatalog.3.1.pdf",
                                                            "21_OneGML_ECatalog.3.1.pdf",
                                                            "22_OneGML_ECatalog.3.1.pdf",
                                                            "23_OneGML_ECatalog.3.1.pdf",
                                                            "24_OneGML_ECatalog.3.1.pdf",
                                                            "25_OneGML_ECatalog.3.1.pdf",
                                                            "26_OneGML_ECatalog.3.1.pdf",
                                                            "27_OneGML_ECatalog.3.1.pdf"
                                                             });
            List<string> academy = new List<string>(new[] { "28_OneGML_ECatalog.3.1.pdf",
                                                            "29_OneGML_ECatalog.3.1.pdf",
                                                            "30_OneGML_ECatalog.3.1.pdf",
                                                            "31_OneGML_ECatalog.3.1.pdf"
                                                             });
            List<string> em = new List<string>(new[] { "32_OneGML_ECatalog.3.1.pdf",
                                                        "33_OneGML_ECatalog.3.1.pdf",
                                                        "34_OneGML_ECatalog.3.1.pdf",
                                                        "35_OneGML_ECatalog.3.1.pdf",
                                                        "36_OneGML_ECatalog.3.1.pdf",
                                                        "37_OneGML_ECatalog.3.1.pdf"
                                                         });
            List<string> coming = new List<string>(new[] { "38_OneGML_ECatalog.3.1.pdf",
                                                            "39_OneGML_ECatalog.3.1.pdf",
                                                            "40_OneGML_ECatalog.3.1.pdf",
                                                            "41_OneGML_ECatalog.3.1.pdf",
                                                            "42_OneGML_ECatalog.3.1.pdf",
                                                            "43_OneGML_ECatalog.3.1.pdf",
                                                             });
            List<string> backCover = new List<string>(new[] { "44_OneGML_ECatalog.3.1.pdf" });

            List<string> pages = new List<string>();
            pages.AddRange(cover);

            Dictionary<int, List<string>> dicts = new Dictionary<int, List<string>>();
            dicts.Add(1, foreword);
            dicts.Add(2, workshops);
            dicts.Add(3, calendar);
            dicts.Add(4, academy);
            dicts.Add(5, em);
            dicts.Add(6, coming);

            foreach (int section in sections)
            {
                pages.AddRange(dicts[section]);
            }

            pages.AddRange(backCover);

            string dir = Path.Combine(_options.DataRootDirectory, @"web", @"ecatalog");

            MergePDFs(target, dir, pages);

            return GetECatalogURL(filename);
        }

        private void MergePDFs(string targetPath, string dir, List<string> pdfs)
        {
            using (PdfDocument targetDoc = new PdfDocument())
            {
                foreach (string pdf in pdfs)
                {
                    string filePath = Path.Combine(dir, pdf);

                    if (System.IO.File.Exists(filePath))
                    {
                        using (PdfDocument pdfDoc = PdfReader.Open(filePath, PdfDocumentOpenMode.Import))
                        {
                            for (int i = 0; i < pdfDoc.PageCount; i++)
                            {
                                targetDoc.AddPage(pdfDoc.Pages[i]);
                            }
                        }
                    }
                }

                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                targetDoc.Save(targetPath);
            }
        }
        private string GetECatalogURL(string filename)
        {
            return _options.AssetsBaseURL + @"web" + "/" + @"ecatalog" + "/" + filename;
        }
    }
}