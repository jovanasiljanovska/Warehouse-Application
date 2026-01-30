using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warehouse.Domain.Identity;
using Warehouse.Repository.Interface;

namespace Warehouse.Repository.Implementation
{
    public class UserRepository : IUserRepository
    {

        private readonly ApplicationDbContext _context;
        private readonly DbSet<WarehouseApplicationUser> entites;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
            this.entites = _context.Set<WarehouseApplicationUser>();
        }

        public WarehouseApplicationUser GetUserById(string id)
        {
            return entites.First(ent => ent.Id == id);
        }

        public List<WarehouseApplicationUser> GetUsersByRole(string roleName)
        {
            // joins: AspNetUsers <- AspNetUserRoles -> AspNetRoles
            var query =
                from u in _context.Users
                join ur in _context.UserRoles on u.Id equals ur.UserId
                join r in _context.Roles on ur.RoleId equals r.Id
                where r.Name == roleName
                select u;

            return query.Distinct().ToList();
        }
    }
}