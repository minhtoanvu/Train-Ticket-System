using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TrainTicket.Business.Interfaces;
using TrainTicket.Data.DbContexts;

namespace TrainTicket.Business.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly TrainTicketDbContext _db;
        public CustomerService(TrainTicketDbContext db) => _db = db;

        public async Task<CustomerDto?> GetProfileAsync(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return null;
            return new CustomerDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber ?? "",
                IsActive = user.IsActive ?? false
            };
        }

        public async Task<bool> UpdateProfileAsync(int userId, string fullName)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return false;
            user.FullName = fullName;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<CustomerDto>> GetAllCustomersAsync()
        {
            var customersRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == "Customer" || r.RoleName == "User");
            if (customersRole == null) return new List<CustomerDto>();

            return await _db.Users
                .Where(u => u.UserRoles.Any(ur => ur.RoleId == customersRole.RoleId) && u.IsDeleted == false)
                .Select(u => new CustomerDto
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber ?? "",
                    IsActive = u.IsActive ?? false
                })
                .ToListAsync();
        }
    }
}