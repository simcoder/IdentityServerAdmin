using System;

namespace IdentityServerAdmin.Dtos
{
    public class ClientSecretDto
    {
        public string Value { get; set; }
        public DateTime? ExpirationDate { get; set; }
    }
}
