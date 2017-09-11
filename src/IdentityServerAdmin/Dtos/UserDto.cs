using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace IdentityServerAdmin.Dtos
{
    public class UserDto 
    {
        public string  SubjectId { get; set; }
        public string Username { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }

        public string Name { get; set; }

        public bool HasTemporaryPassword { get; set; }

        public string Password { get; set; }

        public bool IsSuperAdmin { get; set; }

        public IList<Claim> Claims { get; set; }

        public bool LockoutEnabled { get; set; }

        public DateTimeOffset? LockoutEnd { get; set; }

    }
}
