using RealEstate.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstate.Domain.Interfaces;

public interface IClientRepository
{
    Task<Client?> GetByEmailAsync(string email);
    Task<Client> CreateAsync(Client client);
    Task<bool> ExistsAsync(string email);
}
