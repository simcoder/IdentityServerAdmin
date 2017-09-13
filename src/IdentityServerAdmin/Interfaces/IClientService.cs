using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServerAdmin.Dtos;

namespace IdentityServerAdmin.Interfaces
{
    public interface IClientService
    {
        Task<IList<ClientDto>> GetClientsAsync();

        Task<ClientDto> GetClientByIdAsync(int id);

        Task<bool> CreateClientAsync(ClientCreateDto client);
        Task<bool> EditClient(string id, EditUserDto user);
    }
}
