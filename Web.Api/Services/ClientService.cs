using KDMApi.DataContexts;
using KDMApi.Models;
using KDMApi.Models.Crm;
using KDMApi.Models.Helper;
using KDMApi.Models.Web;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KDMApi.Services
{
    public class ClientService
    {
        private DefaultContext _context;

        public ClientService(DefaultContext context)
        {
            _context = context;
        }

        public async Task<int> GetOrCreateContact(EventAlumniInfo contact, DateTime now, string source)
        {
            CrmContact crmContact = new CrmContact();
            if(contact.Id != 0)
            {
                crmContact = _context.CrmContacts.Where(a => a.Id == contact.Id).FirstOrDefault();
            }
            else if (!contact.Email.Equals(""))
            {
                crmContact = _context.CrmContacts.Where(a => a.Email1.Contains(contact.Email) && a.IsDeleted == false).FirstOrDefault();
            }

            if (crmContact != null && crmContact.Id > 0)
            {
                crmContact.CrmClientId = contact.ClientId;
                crmContact.Name = contact.Name;
                crmContact.Email1 = contact.Email;
                crmContact.Phone1 = contact.Phone;
                crmContact.Department = contact.Department;
                crmContact.Position = contact.JobTitle;
                crmContact.Valid = true;
                crmContact.LastUpdatedBy = contact.UserId;
                crmContact.LastUpdated = now;

                _context.Entry(crmContact).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return crmContact.Id;
            }

            CrmContact crmContact2 = _context.CrmContacts.Where(a => a.Name.Contains(contact.Name) && a.Position.Contains(contact.JobTitle) && a.CrmClientId == contact.ClientId).FirstOrDefault();
            if (crmContact2 != null && crmContact2.Id > 0)
            {
                crmContact2.CrmClientId = contact.ClientId;
                crmContact2.Name = contact.Name;
                crmContact2.Email1 = contact.Email;
                crmContact2.Phone1 = contact.Phone;
                crmContact2.Department = contact.Department;
                crmContact2.Position = contact.JobTitle;
                crmContact2.Valid = true;
                crmContact2.LastUpdatedBy = contact.UserId;
                crmContact2.LastUpdated = now;

                _context.Entry(crmContact2).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return crmContact2.Id;
            }

            CrmContact contact1 = new CrmContact()
            {
                Name = contact.Name,
                Salutation = "",
                Email1 = contact.Email,
                Email2 = "",
                Email3 = "",
                Email4 = "",
                Phone1 = contact.Phone,
                Phone2 = "",
                Phone3 = "",
                Department = contact.Department,
                Position = contact.JobTitle,
                Valid = true,
                Source = source,
                CrmClientId = contact.ClientId,
                CreatedDate = now,
                CreatedBy = contact.UserId,
                LastUpdated = now,
                LastUpdatedBy = contact.UserId,
                IsDeleted = false,
                DeletedBy = 0
            };

            _context.CrmContacts.Add(contact1);
            await _context.SaveChangesAsync();
            return contact1.Id;
        }

        public int GetOrCreateIndustry(CsvContact contact, DateTime now, int userId)
        {
            if (!contact.Industry.Trim().Equals(""))
            {
                CrmIndustry crmIndustry = _context.CrmIndustries.Where(a => a.Industry.Contains(contact.Industry) && a.IsDeleted == false).FirstOrDefault();

                if (crmIndustry == null)
                {
                    CrmIndustry industry = new CrmIndustry()
                    {
                        Industry = contact.Industry,
                        CreatedDate = now,
                        CreatedBy = userId,
                        LastUpdated = now,
                        LastUpdatedBy = userId,
                        IsDeleted = false,
                        DeletedBy = 0
                    };
                    _context.CrmIndustries.Add(industry);
                    _context.SaveChanges();
                    return industry.Id;
                }

                return crmIndustry.Id;
            }

            CrmIndustry crmIndustry1 = _context.CrmIndustries.Where(a => a.Industry.Equals(" ") && !a.IsDeleted).FirstOrDefault();
            if (crmIndustry1 != null) return crmIndustry1.Id;

            return 1;
        }

        public int UpdateOrCreateClient(CsvContact contact, DateTime now, int userId, int industryId, string source)
        {
            CrmClient crmClient = new CrmClient();
            if (!contact.Company.Equals(""))
            {
                crmClient = _context.CrmClients.Where(a => a.Company.Contains(contact.Company) && a.IsDeleted == false).FirstOrDefault();
            }
            if (crmClient == null || crmClient.Id == 0)
            {
                CrmClient client = new CrmClient()
                {
                    //valid,,name,salutation,title,department,,,,phone1,phone2,phone3,phone4,,email1,email2,email3,email4,,industry,
                    Company = contact.Company,
                    Address1 = contact.Address1,
                    Address2 = contact.Address2,
                    Address3 = contact.Address3,
                    Phone = contact.Phone,
                    Fax = contact.Fax,
                    Website = contact.Website,
                    Remarks = contact.Remarks,
                    Source = source,
                    CrmIndustryId = industryId,
                    CreatedDate = now,
                    CreatedBy = userId,
                    LastUpdated = now,
                    LastUpdatedBy = userId,
                    IsDeleted = false,
                    DeletedBy = 0
                };
                _context.CrmClients.Add(client);
                _context.SaveChanges();
                return client.Id;
            }

            crmClient.Address1 = contact.Address1;
            crmClient.Address2 = contact.Address2;
            crmClient.Address3 = contact.Address3;
            crmClient.Phone = contact.Phone;
            crmClient.Fax = contact.Fax;
            crmClient.Website = contact.Website;
            crmClient.Remarks = contact.Remarks;
            _context.Entry(crmClient).State = EntityState.Modified;
            _context.SaveChanges();

            return crmClient.Id;
        }
        public int UpdateOrCreateContact(CsvContact contact, DateTime now, int userId, int clientId, string source)
        {
            CrmContact crmContact = new CrmContact();
            if (!contact.Email1.Equals(""))
            {
                crmContact = _context.CrmContacts.Where(a => a.Email1.Contains(contact.Email1) && a.IsDeleted == false).FirstOrDefault();
            }

            if (crmContact != null && crmContact.Id > 0)
            {
                UpdateContact(crmContact, contact, clientId, userId, source);
                return crmContact.Id;
            }

            CrmContact contact2 = _context.CrmContacts.Where(a => a.Name.Contains(contact.Name) && a.Position.Contains(contact.Title) && a.CrmClientId == clientId).FirstOrDefault();
            if (contact2 != null && contact2.Id > 0)
            {
                UpdateContact(contact2, contact, clientId, userId, source);
                return contact2.Id;
            }

            CrmContact contact1 = new CrmContact()
            {
                Name = contact.Name,
                Salutation = contact.Salutation == "-" ? "" : contact.Salutation,
                Email1 = contact.Email1,
                Email2 = contact.Email2,
                Email3 = contact.Email3,
                Email4 = contact.Email4,
                Phone1 = contact.Hp1,
                Phone2 = contact.Hp2,
                Phone3 = contact.Hp3,
                Department = contact.Department,
                Position = contact.Title,
                Valid = contact.Valid.Equals("V") || contact.Valid.Equals("v") ? true : false,
                Source = source,
                CrmClientId = clientId,
                CreatedDate = now,
                CreatedBy = userId,
                LastUpdated = now,
                LastUpdatedBy = userId,
                IsDeleted = false,
                DeletedBy = 0
            };

            _context.CrmContacts.Add(contact1);
            _context.SaveChanges();
            return contact1.Id;
        }
        public void UpdateContact(CrmContact crmContact, CsvContact contact, int clientId, int userId, string source)
        {
            var now = DateTime.Now;

            if (!(crmContact.Name.Equals(contact.Name) && crmContact.Salutation.Equals(contact.Salutation) &&
                crmContact.Email1.Equals(contact.Email1) && crmContact.Email2.Equals(contact.Email2) &&
                crmContact.Email3.Equals(contact.Email3) && crmContact.Phone1.Equals(contact.Hp1) &&
                crmContact.Phone2.Equals(contact.Hp2) && crmContact.Phone3.Equals(contact.Hp3) &&
                crmContact.Department.Equals(contact.Department) && crmContact.Position.Equals(contact.Title)) ||
                (crmContact.Valid == true && !contact.Valid.Equals("V")) || (crmContact.Valid == false && contact.Valid.Equals("V")))
            {
                crmContact.Name = contact.Name;
                crmContact.Salutation = contact.Salutation == "-" ? "" : contact.Salutation;
                crmContact.Email1 = contact.Email1;
                crmContact.Email2 = contact.Email2;
                crmContact.Email3 = contact.Email3;
                crmContact.Email4 = contact.Email4;
                crmContact.Phone1 = contact.Hp1;
                crmContact.Phone2 = contact.Hp2;
                crmContact.Phone3 = contact.Hp3;
                crmContact.Department = contact.Department;
                crmContact.Position = contact.Title;
                crmContact.Valid = contact.Valid.Equals("V") ? true : false;
                crmContact.Source = source;
                crmContact.CrmClientId = clientId;
                crmContact.CreatedDate = now;
                crmContact.CreatedBy = userId;
                crmContact.LastUpdated = now;
                crmContact.LastUpdatedBy = userId;
                crmContact.IsDeleted = false;
                crmContact.DeletedBy = 0;

                _context.Entry(crmContact).State = EntityState.Modified;
                _context.SaveChanges();
            }
        }


        public async Task<HttpResponseMessage> NotifyUsers(string baseURL, string endpoint, string token, List<NotificationRequest> payload)
        {
            // Call APi with token
            HttpClient request = new HttpClient();
            request.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await request.PostAsJsonAsync(new Uri(baseURL + endpoint).ToString(), payload);
        }
        public async Task<HttpResponseMessage> GetQuBisaAccess(string baseURL, string endpoint, string basicUsername, string basicPassword, string username, string password)
        {
            HttpClient request = new HttpClient();
            request.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Base64Encode($"{basicUsername}:{basicPassword}"));
            UsernamePassword payload = new UsernamePassword(username, password, 1, "OS");
            return await request.PostAsJsonAsync(new Uri(baseURL + endpoint).ToString(), payload);
        }

        public async Task<HttpResponseMessage> RegisterUserToQuBisa(string baseURL, string endpoint, string token, RegisterForumRequest payload)
        {
            // Call APi with token
            HttpClient request = new HttpClient();
            request.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await request.PostAsJsonAsync(new Uri(baseURL + endpoint).ToString(), payload);
        }

        private static string Base64Encode(string textToEncode)
        {
            byte[] textAsBytes = Encoding.UTF8.GetBytes(textToEncode);
            return Convert.ToBase64String(textAsBytes);
        }

        public string GetPassword(string nm)
        {
            string pattern = @"[^a-zA-Z]";
            string replacement = "";
            string result = Regex.Replace(nm, pattern, replacement);
            string pre = result.Substring(0, 3).ToUpper();
            return pre + "123";
        }
    }
}
