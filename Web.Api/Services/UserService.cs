using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Web.Api.Core.Dto.UseCaseRequests;
using Web.Api.Core.Interfaces.UseCases;
using Web.Api.Models.Settings;
using Web.Api.Presenters;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using Web.Api.Infrastructure.Identity;
using AutoMapper;
using Web.Api.Core.Interfaces.Gateways.Repositories;
using Microsoft.AspNetCore.Identity;
using KDMApi.DataContexts;
using KDMApi.Models;
using KDMApi.Models.Helper;

namespace KDMApi.Services
{
    public class UserService
    {
        private readonly IRegisterUserUseCase _registerUserUseCase;
        private readonly RegisterUserPresenter _registerUserPresenter;
        private readonly UserManager<AppUser> _userManager;
        private readonly IMapper _mapper;
        private readonly IUserRepository _userRepository;
        private readonly DefaultContext _context;

        public UserService(IRegisterUserUseCase registerUserUseCase, RegisterUserPresenter registerUserPresenter, UserManager<AppUser> userManager, IUserRepository userRepository, IMapper mapper, DefaultContext context)
        {
            _registerUserUseCase = registerUserUseCase;
            _registerUserPresenter = registerUserPresenter;
            _mapper = mapper;
            _userRepository = userRepository;
            _userManager = userManager;
            _context = context;
        }

        public User GetUserByUsername(string username)
        {
            // Yang deleted tetap diketemukan lagi
            return _context.Users.Where(a => a.UserName.Equals(username.Trim())).FirstOrDefault();
        }

        public User GetUserByIdentity(string identity)
        {
            return _context.Users.Where(a => a.IdentityId.Equals(identity.Trim())).FirstOrDefault();
        }

        public async Task<AspNetUser> UpdatePassword(string username, string newPassword)
        {
            var user = await _userRepository.FindByName(username);

            string newpass = _userManager.PasswordHasher.HashPassword(_mapper.Map<AppUser>(user), newPassword);
            AspNetUser user1 = _context.AspNetUsers.FirstOrDefault(a => a.UserName == user.UserName);

            if (user1 != null)
            {
                if (!user1.PasswordHash.Equals(newpass))
                {
                    user1.PasswordHash = newpass;
                    _context.Entry(user1).State = EntityState.Modified;

                    await _context.SaveChangesAsync();
                }

                return user1;
            }

            return null;
        }

        public async Task<int> AddUser(string name, string email, string phone, string password, int roleId)
        {
            try
            {
                await _registerUserUseCase.Handle(new RegisterUserRequest(name, "", email, email, password), _registerUserPresenter);
                var result = _registerUserPresenter.ContentResult;

                if (result.StatusCode == 200)
                {
                    UserRegister nu = JsonConvert.DeserializeObject<UserRegister>(result.Content);
                    if (nu != null)
                    {
                        User u = GetUserByIdentity(nu.Id);

                        u.RoleID = roleId;
                        u.Phone = phone;
                        u.Email = email;
                        u.IsDeleted = false;
                        _context.Entry(u).State = EntityState.Modified;
                        await _context.SaveChangesAsync();

                        return u.ID;
                    }
                }
                else
                {
                    return 0;
                }
            }
            catch
            {
                return 0;
            }
            return 0;
        }

    }

}
