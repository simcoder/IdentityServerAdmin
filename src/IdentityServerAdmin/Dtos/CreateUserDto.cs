using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServerAdmin.Dtos
{
    public class CreateUserDto
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public bool IsTempPassword { get; set; }

        public string Email { get; set; }

        public string DomainOwner { get; set; }
    }
}
