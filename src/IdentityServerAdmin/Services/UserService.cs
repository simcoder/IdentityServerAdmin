using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServerAdmin.Dtos;
using IdentityServerAdmin.Interfaces;
using IdentityServerAdmin.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IdentityServerAdmin.Services
{
    /// <summary>
    ///     this class manages application user
    /// </summary>
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        /// <summary>
        /// </summary>
        /// <param name="userManager">Identity Server</param>
        public UserService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<UserDto> FindByUsernameAsync(string username)
        {
            var userEntity = await _userManager.FindByNameAsync(username);
            return userEntity != null ? MapUser(userEntity) : null;
        }

        public async Task<UserDto> FindUserByIdAsync(string id)
        {
            var userEntity = await _userManager.FindByIdAsync(id);

            return userEntity != null ? MapUser(userEntity) : null;
        }

        public async Task<bool> CheckPasswordAsync(UserDto user, string password)
        {
            var userEntity = await _userManager.FindByNameAsync(user.Username);

            return await _userManager.CheckPasswordAsync(userEntity, password);
        }

        public async Task<bool> CreateUserAsync(CreateUserDto user)
        {
            var addUserResult =
                await _userManager.CreateAsync(
                    new ApplicationUser
                    {
                        UserName = user.Username,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        IsTempPassword = user.IsTempPassword,
                        Email = user.Email,
                        DomainOwner = user.DomainOwner
                    }, user.Password);

            var appUser = await _userManager.FindByNameAsync(user.Username);

            //add default role
            var addUserRoleResult = await _userManager.AddToRoleAsync(appUser, "ApplicationUser");

            if (addUserResult.Succeeded && addUserRoleResult.Succeeded)
            {
                //add default claims
                await _userManager.AddClaimAsync(appUser, new Claim("name", user.Username));

                await _userManager.AddClaimAsync(appUser, new Claim(ClaimTypes.Role, "ApplicationUser"));

                return true;
            }

            return false;
        }

        public async Task<bool> EditUserAsync(string id, EditUserDto editUserDto)
        {
            var userEntity = await _userManager.FindByIdAsync(id);
            userEntity.FirstName = editUserDto.FirstName;
            userEntity.LastName = editUserDto.LastName;
            userEntity.Email = editUserDto.Email;
            userEntity.UserName = editUserDto.Username;
            var result = await _userManager.UpdateAsync(userEntity);
            return result.Succeeded;
        }

        public async Task<bool> ResetPasswordAsync(string modelUsername, string newPassword)
        {
            var user = await _userManager.FindByNameAsync(modelUsername);
            var result = await _userManager.ResetPasswordAsync(user, user.PasswordHash,newPassword);
            return result.Succeeded;
        }

        public async Task<List<UserDto>> GetUsersAsync()
        {
            var users = await _userManager.Users.Where(x => !x.IsSuperAdmin).ToListAsync();
            var mappedUsers = new List<UserDto>();
            foreach (var user in users)
                mappedUsers.Add(new UserDto
                {
                    Username = user.UserName,
                    SubjectId = user.Id,
                    HasTemporaryPassword = user.IsTempPassword,
                    Name = $"{user.FirstName} {user.LastName}"
                });
            return mappedUsers;
        }

        public async Task<List<Claim>> GetClaimsAsync(UserDto user)
        {
            var users = await _userManager.FindByNameAsync(user.Username);

            var result = await _userManager.GetClaimsAsync(users);

            return result.ToList();
        }

        public async Task<bool> ValidateUsernameAndPassword(string modelUsername, string modelPassword)
        {
            var user = await FindByUsernameAsync(modelUsername);

            return await CheckPasswordAsync(user, modelPassword);
        }

        private UserDto MapUser(ApplicationUser userEntity)
        {
            return new UserDto
            {
                SubjectId = userEntity.Id,
                Username = userEntity.UserName,
                Password = userEntity.PasswordHash,
                LockoutEnabled = userEntity.LockoutEnabled,
                LockoutEnd = userEntity.LockoutEnd,
                IsSuperAdmin = userEntity.IsSuperAdmin,
                FirstName = userEntity.FirstName,
                Email = userEntity.Email,
                LastName = userEntity.LastName
            };
        }
    }
}