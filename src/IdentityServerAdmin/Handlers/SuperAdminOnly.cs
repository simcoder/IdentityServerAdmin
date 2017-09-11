using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace IdentityServerAdmin.Handlers
{
    /// <summary>
    /// This policy ensures current user is superadmin user 
    /// </summary>
    public class SuperAdminOnly : AuthorizationHandler<SuperAdminOnly>, IAuthorizationRequirement
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SuperAdminOnly requirement)
        {
            if (context.User.Identity.Name != "superadmin")
            {
                context.Fail();
            }
            else
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
