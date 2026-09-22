using System.Collections.Generic;
using System.Threading.Tasks;

namespace TrainTicket.Business.Interfaces
{
    public class CustomerDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public interface ICustomerService
    {
        Task<CustomerDto?> GetProfileAsync(int userId);
        Task<bool> UpdateProfileAsync(int userId, string fullName);
        Task<List<CustomerDto>> GetAllCustomersAsync();
    }
}