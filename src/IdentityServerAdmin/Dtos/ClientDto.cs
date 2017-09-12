using System.Collections.Generic;
using IdentityServer4.EntityFramework.Entities;

namespace IdentityServerAdmin.Dtos
{
    public class ClientDto 
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Uri { get; set; }
        public bool RequireConsent { get; set; }
        public ClientSecretDto ClientSecret { get; set; }
        public List<string> AllowedGrantTypes { get; set; }
        public List<string> AllowedScopes { get; set; }
        public bool AllowOfflineAccess { get; set; }

    }
}
