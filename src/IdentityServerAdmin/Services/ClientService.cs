using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Entities;
using IdentityServerAdmin.Dtos;
using IdentityServerAdmin.Interfaces;
using Microsoft.EntityFrameworkCore;

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
            List<Client> clientEntities = await _context.Clients.ToListAsync();
            IList<ClientDto> clients = new List<ClientDto>();
            foreach (Client clientEntity in clientEntities)
            {
                clients.Add(new ClientDto
                {
                    Name = clientEntity.ClientName,
                    Uri = clientEntity.ClientUri
                });
            }
            return clients;
        }
    }
}
