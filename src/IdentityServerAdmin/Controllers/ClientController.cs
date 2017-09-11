using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityServerAdmin.Controllers
{
    [Authorize(Policy = "SuperAdminOnly")]
    public class ClientController : Controller
    {

    }
}
