using System.Threading.Tasks;
using IdentityServer4.Validation;
using IdentityServerAdmin.Interfaces;

namespace IdentityServerAdmin.Configuration
{
    public class ResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
    {
        private readonly IUserService _userManager;

        public ResourceOwnerPasswordValidator(IUserService userService)
        {
            _userManager = userService;
        }
        
        public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
        {
            var user = await _userManager.FindByUsernameAsync(context.UserName);
            //validate resource owner is who he say he is
            await _userManager.CheckPasswordAsync(user, context.Password);
           
        }
    }

   
}
