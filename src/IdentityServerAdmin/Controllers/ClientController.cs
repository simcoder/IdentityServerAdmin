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
    public class ClientController : Controller
    {
        private readonly IClientService _clientService;
        public ClientController(IClientService clientService)
        {
            _clientService = clientService;
        }
        public async Task<IActionResult> Index()
        {
            IList<ClientDto> clients = await _clientService.GetClientsAsync();

            return View(clients);
        }

        [HttpGet]
        public async Task<IActionResult> EditClient(int id)
        {
            ClientDto client = await _clientService.GetClientByIdAsync(id);
            return View(client);
        }
        [HttpPost]
        public async Task<IActionResult> EditClient(string id, EditUserDto user)
        {
            if (ModelState.IsValid)
            {
                bool success = await _clientService.EditClient(id, user);
                if (success)
                    return RedirectToAction("Index", "Client");
            }
            //TODO need validation
            return View(user);
        }

        [HttpGet]
        public IActionResult CreateClient(string id)
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> CreateClient(string id, ClientCreateDto client)
        {
            if (ModelState.IsValid)
            {
                bool success = await _clientService.CreateClientAsync(client);
                if (success)
                    return RedirectToAction("Index", "User");
            }
            //TODO need validation
            return View(client);
        }

    }
}
