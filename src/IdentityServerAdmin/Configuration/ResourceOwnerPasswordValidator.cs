using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Models;
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
            if (user != null && !user.IsSuperAdmin)
            {
                var isValidPassword = await _userManager.CheckPasswordAsync(user, context.Password);
                if (isValidPassword)
                {
                    IEnumerable<Claim> cs = await _userManager.GetClaimsAsync(user);
                    context.Result = new GrantValidationResult(
                        subject: user.SubjectId,
                        authenticationMethod: "customResourceOwnerValidation",
                        claims: cs);
                }
            }
            else
            {
                context.Result = new GrantValidationResult(
                    TokenRequestErrors.InvalidGrant,
                    "invalid custom credential");
            }
        }
    }

   
}
