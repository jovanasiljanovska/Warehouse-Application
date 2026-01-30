using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warehouse.Domain.Identity;

namespace Warehouse.Repository.Interface
{
    public interface IUserRepository
    {
        WarehouseApplicationUser GetUserById(string id);
        List<WarehouseApplicationUser> GetUsersByRole(string roleName);
    }
}