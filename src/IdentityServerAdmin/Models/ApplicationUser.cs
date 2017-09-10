using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace IdentityServerAdmin.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public bool IsSuperAdmin { get; set; }

        public bool IsTempPassword { get; set; }

        public string DomainOwner { get; set; }
    }
}
