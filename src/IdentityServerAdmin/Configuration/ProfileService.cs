using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServerAdmin.Dtos;
using IdentityServerAdmin.Interfaces;

namespace IdentityServerAdmin.Configuration
{
    public class ProfileService : IProfileService
    {
        private readonly IUserService _userManager;

        public ProfileService(IUserService userService)
        {
            _userManager = userService;
        }

         public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var subject = context.Subject;
            if (subject == null) throw new ArgumentNullException(nameof(context.Subject));


            Claim claim = subject.FindFirst("name");

            var user = await _userManager.FindByUsernameAsync(claim.Value);
            var cp = await GetClaimsFromUser(user);
            if (user == null)
                throw new ArgumentException("Invalid subject identifier");

            var claims = cp.Claims.ToList();
 
            context.IssuedClaims = claims;
        }

        public async Task IsActiveAsync(IsActiveContext context)
        {
            var subject = context.Subject;
            if (subject == null) throw new ArgumentNullException(nameof(context.Subject));

            Claim claim = subject.FindFirst("name");
            //if claim isnt null then thi is a user
            if (claim != null)
            {
                var user = await _userManager.FindByUsernameAsync(claim.Value);

                context.IsActive = false;

                if (user != null)
                {
                    // enable this for security stamp
                    //if (_userManager.SupportsUserSecurityStamp)
                    //{
                    //    var security_stamp = subject.Claims.Where(c => c.Type == "security_stamp").Select(c => c.Value).SingleOrDefault();
                    //    if (security_stamp != null)
                    //    {
                    //        var db_security_stamp = await _userManager.GetSecurityStampAsync(user);
                    //        if (db_security_stamp != security_stamp)
                    //            return;
                    //    }
                    //}

                    context.IsActive =
                        !user.LockoutEnabled ||
                        !user.LockoutEnd.HasValue ||
                        user.LockoutEnd <= DateTime.Now;
                }
            }
            
           
        }

        public async Task<ClaimsPrincipal> GetClaimsFromUser(UserDto user)
        {
            var claimsIdentity= new ClaimsIdentity();

            claimsIdentity.AddClaims(await _userManager.GetClaimsAsync(user));

            //if (_userManager.SupportsUserEmail)
            //{
            //    claims.AddRange(new[]
            //    {
            //        new Claim(JwtClaimTypes.Email, user.Email),
            //        new Claim(JwtClaimTypes.EmailVerified, user.EmailConfirmed ? "true" : "false", ClaimValueTypes.Boolean)
            //    });
            //}

            //if (_userManager.SupportsUserPhoneNumber && !string.IsNullOrWhiteSpace(user.PhoneNumber))
            //{
            //    claims.AddRange(new[]
            //    {
            //        new Claim(JwtClaimTypes.PhoneNumber, user.PhoneNumber),
            //        new Claim(JwtClaimTypes.PhoneNumberVerified, user.PhoneNumberConfirmed ? "true" : "false", ClaimValueTypes.Boolean)
            //    });
            //}

            //if (_userManager.SupportsUserClaim)
            //{
            //    claims.AddRange(await _userManager.GetClaimsAsync(user));
            //}

            //if (_userManager.SupportsUserRole)
            //{
            //    var roles = await _userManager.GetRolesAsync(user);
            //    claims.AddRange(roles.Select(role => new Claim(JwtClaimTypes.Role, role)));
            //}

            return new ClaimsPrincipal(claimsIdentity);
        }
    }
}
