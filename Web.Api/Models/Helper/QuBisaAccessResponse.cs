using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Web.Api.Core.Dto;

namespace KDMApi.Models.Helper
{
    public class QuBisaAccessResponse
    {
        /*
         {  "accessToken": {    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhZG1pbmdtbCIsImp0aSI6Ijk4ZGEyZDgyLWNkNzQtNGVjMC1iMDQzLWI4YmE2OTYyMjhjYyIsImlhdCI6MTYyNDE5NjU1OSwicm9sIjoiYXBpX2FjY2VzcyIsImlkIjoiZmQ5NWFmMjAtMTczOC00NzQ5LWJmYzQtYjY5YWVlMzYzMzcyIiwibmJmIjoxNjI0MTk2NTU4LCJleHAiOjE2MjQyMDM3NTgsImlzcyI6IlF1QmlzYS5jb20iLCJhdWQiOiJodHRwczovL3F1YmlzYS5jb20vIn0.DgSDgJqjsTkn0jAgogEpNncaH-gpQ0NsVaNz5E6kAzg",    "expiresIn": 7200  },  
        "refreshToken": "Bcr+5OqLomd04M8e9+w0zsIbFCneYpy17Mt40dweknY=",  
        "id": 1023,  
        "userName": "admingml",  
        "profilePic": "",  "email": "admingml@gmlperformance.co.id",  
        "roleId": 1,  
        "firstName": "Admin GML Performance"}
         */
        public AccessToken AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public int Id { get; set; }
        public string UserName { get; set; }
        public string ProfilePic { get; set; }
        public string Email { get; set; }
        public int RoleId { get; set; }
        public string FirstName { get; set; }
    }
}
