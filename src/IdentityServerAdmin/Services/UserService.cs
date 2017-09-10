using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServerAdmin.Data;
using IdentityServerAdmin.Dtos;
using IdentityServerAdmin.Interfaces;
using IdentityServerAdmin.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IdentityServerAdmin.Services
{
    public class UserService: IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
       
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="userManager">Identity Server</param>
        public UserService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
            
        }
        public Task<UserDto> FindByUsernameAsync(string username)
        {
            
                try
                {
                    ApplicationUser result = _context.Users
                        .SingleOrDefault(x => x.UserName == username);
                    var user = new UserDto()
                    {
                        SubjectId = result.Id,
                        Password = result.PasswordHash,
                        Username = result.UserName,
                        LockoutEnabled = result.LockoutEnabled,
                        LockoutEnd = result.LockoutEnd

                    };
                    return Task.FromResult(user);
                }
                catch (Exception ex)
                {
                    
                }

 

            return null;
        }

        public async Task<bool> CheckPasswordAsync(UserDto user, string password)
        {
            var userEntity = _context.Users.SingleOrDefault(x => x.UserName == user.Username);

            return await _userManager.CheckPasswordAsync(userEntity, password);
        }

        public async Task<bool> CreateUserAsync(UserDto user)
        {
            IdentityResult result = await _userManager.CreateAsync(new ApplicationUser {UserName = user.Username}, user.Password);
            ApplicationUser appUser = await _userManager.FindByNameAsync(user.Username);

            await _userManager.AddClaimAsync(appUser, new Claim("name", user.Username));
            return result.Succeeded;
        }

        public Task<List<Claim>> GetClaimsAsync(UserDto user)
        {
            using (var ctx = _context)
            {
                var result = ctx.Users
                    .Where(x => x.UserName == user.Username).Include("Claims").SingleOrDefault();
                var result2 = result.Claims.AsQueryable();
                var fr = new List<Claim>();
                foreach (IdentityUserClaim<string> claim in result2)
                {
                   
                   fr.Add(new Claim(claim.ClaimType,claim.ClaimValue));
                }
                return Task.FromResult(fr);
            }
        }

        public async Task<bool> ValidateUsernameAndPassword(string modelUsername, string modelPassword)
        {
            UserDto user = await FindByUsernameAsync(modelUsername);

            return await CheckPasswordAsync(user, modelPassword);
        }

    }

   
}
