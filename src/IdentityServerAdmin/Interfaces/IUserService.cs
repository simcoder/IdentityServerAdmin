using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServerAdmin.Dtos;

namespace IdentityServerAdmin.Interfaces
{
    public interface IUserService
    {
        Task<UserDto> FindByUsernameAsync(string username);

        Task<bool> CheckPasswordAsync(UserDto user, string password);

        Task<List<Claim>> GetClaimsAsync(UserDto user);

        Task<bool> ValidateUsernameAndPassword(string modelUsername, string modelPassword);

        Task<bool> CreateUserAsync(UserDto user);
    }
}
