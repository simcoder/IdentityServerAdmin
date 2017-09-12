using System.Collections.Generic;
using IdentityServerAdmin.Dtos.Enums;

namespace IdentityServerAdmin.Dtos
{
    public class ClientCreateDto
    {
        public string ClientId { get; set; }

        public string ClientName { get; set; }

        public ClientSecretDto ClientSecret { get; set; }

        public AllowedGrantTypes AllowGrantTypes { get; set; }

        public string ClientUri { get; set; }

        public List<string> AllowedScopes { get; set; }

        public bool RequireConsent { get; set; }
        public bool AllowOfflineAccess { get; set; }
    }
}
