using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Test;
using IdentityServerAdmin.Dtos;
using IdentityServerAdmin.Interfaces;
using Microsoft.AspNetCore.Mvc;
using IdentityServerAdmin.Models.AccountViewModels;
using IdentityServerAdmin.Services;
using IdentityServerHost.Dtos.Test;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;

namespace IdentityServerAdmin.Controllers
{
    public class AccountController : Controller
    {

        private readonly IIdentityServerInteractionService _interaction;
        private readonly AccountService _account;
        private readonly IUserService _userService;
        private readonly TestUserStore _users;
        public AccountController(
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IHttpContextAccessor httpContextAccessor,
            IUserService userService,
            TestUserStore users = null)
        {
            // if the TestUserStore is not in DI, then we'll just use the global users collection
            _users = users ?? new TestUserStore(TestUsers.Users);
            _interaction = interaction;
            _userService = userService;
            _account = new AccountService(interaction, httpContextAccessor, clientStore);
        }

        /// <summary>
        /// Register
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Register(string returnUrl)
        {
            var vm = await _account.BuildRegisterViewModelAsync(returnUrl);
            return View(vm);
        }

        /// <summary>
        /// Regster
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                var createUserDto = new CreateUserDto()
                {
                    Username = model.UserName,
                    Password = model.Password,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    DomainOwner = model.ClientId
                };
                bool success = await _userService.CreateUserAsync(createUserDto);
                UserDto createdUser = await _userService.FindByUsernameAsync(createUserDto.Username);
                createdUser.Claims = await _userService.GetClaimsAsync(createdUser);
                if (success)
                {
                    if (returnUrl != null)
                    {
                        await HttpContext.Authentication.SignInAsync(createdUser.SubjectId, createdUser.Username);
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("Index", "Home");
                }
            }
            return null;
        }

        /// <summary>
        /// Show login page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl)
        {
            var vm = await _account.BuildLoginViewModelAsync(returnUrl);

            if (vm.IsExternalLoginOnly)
            {
                // only one option for logging in
                return await ExternalLogin(vm.ExternalProviders.First().AuthenticationScheme, returnUrl);
            }

            return View(vm);
        }

        /// <summary>
        /// Handle postback from username/password login
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginInputModel model)
        {
            if (ModelState.IsValid)
            {
                var isValidUsernameAndPassword = await _userService.ValidateUsernameAndPassword(model.Username, model.Password);
                if (isValidUsernameAndPassword)
                {
                    AuthenticationProperties props = null;
                    // only set explicit expiration here if persistent. 
                    // otherwise we reply upon expiration configured in cookie middleware.
                    if (AccountOptions.AllowRememberLogin && model.RememberLogin)
                    {
                        props = new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.Add(AccountOptions.RememberMeLoginDuration)
                        };
                    };

                    // issue authentication cookie with subject ID and username
                    UserDto user = await _userService.FindByUsernameAsync(model.Username);
                    if (!user.IsSuperAdmin)
                        user.Claims = await _userService.GetClaimsAsync(user);


                    await HttpContext.Authentication.SignInAsync(user.SubjectId, user.Username, props);

                    // make sure the returnUrl is still valid, and if yes - redirect back to authorize endpoint
                    if (_interaction.IsValidReturnUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }
                    //regular application users should not be allowed to admin tool
                    if (user.IsSuperAdmin )
                    {
                        if (model.ReturnUrl != null && Url.IsLocalUrl(model.ReturnUrl))
                        {
                            return Redirect(model.ReturnUrl);
                        }
                        return Redirect("~/");
                    }
                    
                }

                ModelState.AddModelError(string.Empty, AccountOptions.InvalidCredentialsErrorMessage);
            }

            // something went wrong, show form with error
            var vm = await _account.BuildLoginViewModelAsync(model);
            return View(vm);
        }

        /// <summary>
        /// Show logout page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Logout(string logoutId)
        {
            var vm = await _account.BuildLogoutViewModelAsync(logoutId);

            if (vm.ShowLogoutPrompt == false)
            {
                // no need to show prompt
                return await Logout(vm);
            }

            return View("Logout", vm);
        }

        /// <summary>
        /// Handle logout page postback
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(LogoutInputModel model)
        {
            var vm = await _account.BuildLoggedOutViewModelAsync(model.LogoutId);
            if (vm.TriggerExternalSignout)
            {
                string url = Url.Action("Logout", new { logoutId = vm.LogoutId });
                try
                {
                    // hack: try/catch to handle social providers that throw
                    await HttpContext.Authentication.SignOutAsync(vm.ExternalAuthenticationScheme,
                        new AuthenticationProperties { RedirectUri = url });
                }
                catch (NotSupportedException) // this is for the external providers that don't have signout
                {
                }
                catch (InvalidOperationException) // this is for Windows/Negotiate
                {
                }
            }

            //TODO add admin role to all admins and check role instead
            if (User.Identity.Name == "superadmin")
            {
                await HttpContext.Authentication.SignOutAsync();
                return RedirectToAction("Login");
            }
            await HttpContext.Authentication.SignOutAsync();
            return View("LoggedOut", vm);
        }

        /// <summary>
        /// initiate roundtrip to external authentication provider
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExternalLogin(string provider, string returnUrl)
        {
            returnUrl = Url.Action("ExternalLoginCallback", new { returnUrl = returnUrl });

            // windows authentication is modeled as external in the asp.net core authentication manager, so we need special handling
            if (AccountOptions.WindowsAuthenticationSchemes.Contains(provider))
            {
                // but they don't support the redirect uri, so this URL is re-triggered when we call challenge
                if (HttpContext.User is WindowsPrincipal)
                {
                    var props = new AuthenticationProperties();
                    props.Items.Add("scheme", HttpContext.User.Identity.AuthenticationType);

                    var id = new ClaimsIdentity(provider);
                    id.AddClaim(new Claim(ClaimTypes.NameIdentifier, HttpContext.User.Identity.Name));
                    id.AddClaim(new Claim(ClaimTypes.Name, HttpContext.User.Identity.Name));

                    await HttpContext.Authentication.SignInAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme, new ClaimsPrincipal(id), props);

                    return Redirect(returnUrl);
                }
                else
                {
                    // this triggers all of the windows auth schemes we're supporting so the browser can use what it supports
                    return new ChallengeResult(AccountOptions.WindowsAuthenticationSchemes);
                }
            }
            else
            {
                // start challenge and roundtrip the return URL
                var props = new AuthenticationProperties
                {
                    RedirectUri = returnUrl,
                    Items = { { "scheme", provider } }
                };
                return new ChallengeResult(provider, props);
            }
        }

        /// <summary>
        /// Post processing of external authentication
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl)
        {
            // read external identity from the temporary cookie
            var info = await HttpContext.Authentication.GetAuthenticateInfoAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme);
            var tempUser = info?.Principal;
            if (tempUser == null)
            {
                throw new Exception("External authentication error");
            }

            // retrieve claims of the external user
            var claims = tempUser.Claims.ToList();

            // try to determine the unique id of the external user - the most common claim type for that are the sub claim and the NameIdentifier
            // depending on the external provider, some other claim type might be used
            var userIdClaim = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Subject);
            if (userIdClaim == null)
            {
                userIdClaim = claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
            }
            if (userIdClaim == null)
            {
                throw new Exception("Unknown userid");
            }

            // remove the user id claim from the claims collection and move to the userId property
            // also set the name of the external authentication provider
            claims.Remove(userIdClaim);
            var provider = info.Properties.Items["scheme"];
            var userId = userIdClaim.Value;

            // check if the external user is already provisioned
            var user = _users.FindByExternalProvider(provider, userId);
            if (user == null)
            {
                // this sample simply auto-provisions new external user
                // another common approach is to start a registrations workflow first
                user = _users.AutoProvisionUser(provider, userId, claims);
            }

            var additionalClaims = new List<Claim>();

            // if the external system sent a session id claim, copy it over
            var sid = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.SessionId);
            if (sid != null)
            {
                additionalClaims.Add(new Claim(JwtClaimTypes.SessionId, sid.Value));
            }

            // if the external provider issued an id_token, we'll keep it for signout
            AuthenticationProperties props = null;
            var id_token = info.Properties.GetTokenValue("id_token");
            if (id_token != null)
            {
                props = new AuthenticationProperties();
                props.StoreTokens(new[] { new AuthenticationToken { Name = "id_token", Value = id_token } });
            }

            // issue authentication cookie for user
            await HttpContext.Authentication.SignInAsync(user.SubjectId, user.Username, provider, props, additionalClaims.ToArray());

            // delete temporary cookie used during external authentication
            await HttpContext.Authentication.SignOutAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme);

            // validate return URL and redirect back to authorization endpoint
            if (_interaction.IsValidReturnUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return Redirect("~/");
        }
    }
}
