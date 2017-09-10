using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServerAdmin.Dtos;

namespace IdentityServerAdmin.Interfaces
{
    public interface IClientService
    {
        Task<IList<ClientDto>> GetClients();
    }
}
