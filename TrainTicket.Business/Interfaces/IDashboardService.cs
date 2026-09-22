using System.Threading.Tasks;

namespace TrainTicket.Business.Interfaces
{
    public class CustomerDashboardStatsDto
    {
        public int TotalTickets { get; set; }
        public int PendingTickets { get; set; }
        public decimal TotalSpent { get; set; }
    }

    public class AdminDashboardStatsDto
    {
        public int UserCount { get; set; }
        public int ActiveTrains { get; set; }
        public int ActiveRoutes { get; set; }
        public int TicketsSoldToday { get; set; }
        public decimal RevenueToday { get; set; }
        public int TotalSchedules { get; set; }
    }

    public interface IDashboardService
    {
        Task<CustomerDashboardStatsDto> GetCustomerStatsAsync(int userId);
        Task<System.Collections.IList> GetCustomerRecentTicketsAsync(int userId);
        Task<AdminDashboardStatsDto> GetAdminStatsAsync();
        Task<System.Collections.IList> GetAdminRecentTicketsAsync();
    }
}
