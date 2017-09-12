using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Entities;
using IdentityServerAdmin.Dtos;
using IdentityServerAdmin.Dtos.Enums;
using IdentityServerAdmin.Interfaces;
using Microsoft.EntityFrameworkCore;
using Client = IdentityServer4.EntityFramework.Entities.Client;

namespace IdentityServerAdmin.Services
{
    public class ClientService : IClientService
    {
        private readonly ConfigurationDbContext _context;

        public ClientService(ConfigurationDbContext context)
        {
            _context = context;
        }

        public async Task<IList<ClientDto>> GetClients()
        {
            using (_context)
            {
                var clientEntities = await _context.Clients.ToListAsync();
                IList<ClientDto> clients = new List<ClientDto>();
                foreach (var clientEntity in clientEntities)
                    clients.Add(MapClientEntityToDto(clientEntity));
                return clients;
            }
        }

        public async Task<ClientDto> GetClientById(int id)
        {
            using (_context)
            {
                var clientEntity = await _context.Clients.SingleAsync(x => x.Id == id);

                return MapClientEntityToDto(clientEntity);
            }
        }

        public async Task<ClientDto> CreateClient(ClientCreateDto client)
        {
            using (_context)
            {
                var clientToCreate = new Client
                {
                    ClientId = client.ClientId,
                    ClientName = client.ClientName,

                    RequireConsent = client.RequireConsent,
                    AllowedScopes = client.AllowedScopes.Select(allowedScope => new ClientScope { Scope = allowedScope }).ToList(),
                    ClientSecrets =
                    {
                        new ClientSecret{Value = client.ClientSecret.Value, Expiration = client.ClientSecret.ExpirationDate}
                    },

                    RedirectUris = new List<ClientRedirectUri>
                    {
                        new ClientRedirectUri {RedirectUri = $"{client.ClientUri}/signin-oidc"}
                    },
                    PostLogoutRedirectUris = new List<ClientPostLogoutRedirectUri>
                    {
                        new ClientPostLogoutRedirectUri {PostLogoutRedirectUri = $"{client.ClientUri}/signout-callback-oidc"}
                    },
                    AllowOfflineAccess = client.AllowOfflineAccess
                };
                switch (client.AllowGrantTypes)
                {
                    case AllowedGrantTypes.ClientCredentials:
                        clientToCreate.AllowedGrantTypes = new List<ClientGrantType>()
                        {
                            new ClientGrantType
                            {
                                GrantType = "client_credentials"
                            }
                        }; 
                        break;
                    case AllowedGrantTypes.HybridAndClientCredential:
                        clientToCreate.AllowedGrantTypes = new List<ClientGrantType>()
                        {
                            new ClientGrantType
                            {
                                GrantType = "hybrid"
                            },
                            new ClientGrantType()
                            {
                                GrantType = "client_credentials"
                            }
                        };
                        break;
                    case AllowedGrantTypes.ResourceOwnerPassword:
                        clientToCreate.AllowedGrantTypes = new List<ClientGrantType>()
                        {
                            new ClientGrantType
                            {
                                GrantType = "password"
                            }
                        }; 
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                await _context.Clients.AddAsync(clientToCreate);
                await _context.SaveChangesAsync();
                return MapClientEntityToDto(clientToCreate);
            }
        }

        private static ClientDto MapClientEntityToDto(Client client)
        {
            return new ClientDto
            {
                Id = client.ClientId,
                Name = client.ClientName,
                Uri = client.ClientUri,
                RequireConsent = client.RequireConsent,
                ClientSecret = new ClientSecretDto
                {
                    Value = client.ClientSecrets.First().Value,
                    ExpirationDate = client.ClientSecrets.First().Expiration
                },
                AllowOfflineAccess = client.AllowOfflineAccess,
                AllowedGrantTypes = client.AllowedGrantTypes.Select(clientGrantType => clientGrantType.GrantType)
                    .ToList()
            };
        }
    }
}