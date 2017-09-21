using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServerAdmin.Dtos;
using IdentityServerAdmin.Interfaces;
using IdentityServerAdmin.Models.AccountViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityServerAdmin.Controllers
{
    [Authorize(Policy = "SuperAdminOnly")]
    public class UserController  : Controller
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }
        public async Task<IActionResult> Index()
        {
            List<UserDto> users = await _userService.GetUsersAsync();

            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(string username)
        {
            UserDto user = await _userService.FindByUsernameAsync(username);
            return View(user);
        }
        [HttpPost]
        public async Task<IActionResult> EditUser(string id, EditUserDto user)
        {
            if (ModelState.IsValid)
            {
                bool success = await _userService.EditUserAsync(id,user);
                if(success)
                  return RedirectToAction("Index", "User");
            }
            //TODO need validation
            return View(user);
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> CreateUser(CreateUserDto user)
        {
            if (ModelState.IsValid)
            {
                bool success = await _userService.CreateUserAsync(user);
                if (success)
                    return RedirectToAction("Index", "User");
            }
            //TODO need validation
            return View(user);
        }

        [HttpGet]
        public IActionResult ResetPassword(string username)
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                bool success = await _userService.ResetPasswordAsync(model.Username,model.Password);
                if (success)
                    return RedirectToAction("Index", "User");
            }
            //TODO need validation
            return View();
        }
    }
}
