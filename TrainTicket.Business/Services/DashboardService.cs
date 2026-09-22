using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TrainTicket.Business.Interfaces;
using TrainTicket.Data.DbContexts;

namespace TrainTicket.Business.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly TrainTicketDbContext _db;
        public DashboardService(TrainTicketDbContext db) => _db = db;

        public async Task<CustomerDashboardStatsDto> GetCustomerStatsAsync(int userId)
        {
            var total = await _db.Tickets.CountAsync(t => t.UserId == userId);
            var pending = await _db.Tickets.CountAsync(t => t.UserId == userId && t.Status == "Pending");
            var spent = await _db.Tickets
                .Where(t => t.UserId == userId && (t.Status == "Confirmed" || t.Status == "Used" || t.Status == "Paid" || t.Status == "Completed"))
                .SumAsync(t => (decimal?)t.FinalPrice) ?? 0;

            return new CustomerDashboardStatsDto
            {
                TotalTickets = total,
                PendingTickets = pending,
                TotalSpent = spent
            };
        }

                public async Task<System.Collections.IList> GetCustomerRecentTicketsAsync(int userId)
        {
            return await _db.Tickets
                .Include(t => t.Schedule).ThenInclude(s => s.Route)
                .Include(t => t.Seat)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(20)
                .Select(t => new
                {
                    TicketId = t.TicketId,
                    TicketCode = t.TicketCode,
                    Route = t.Schedule.Route.RouteName,
                    DepartureTime = t.Schedule.DepartureTime,
                    Seat = t.Seat.SeatNumber,
                    Price = t.FinalPrice,
                    Status = t.Status
                })
                .ToListAsync();
        }

        public async Task<AdminDashboardStatsDto> GetAdminStatsAsync()
        {
            var today = DateTime.Today;
            var users = await _db.Users.CountAsync(u => u.IsDeleted == false);
            var trains = await _db.Trains.CountAsync(t => t.IsActive == true);
            var routes = await _db.Routes.CountAsync(r => r.IsActive == true);
            var schedules = await _db.Schedules.CountAsync(s => s.IsActive == true);

            var todayTickets = await _db.Tickets
                .Where(t => t.BookedAt >= today && t.Status != "Cancelled")
                .ToListAsync();

            var ticketsSold = todayTickets.Count;
            var revenue = todayTickets.Sum(t => t.FinalPrice);

            return new AdminDashboardStatsDto
            {
                UserCount = users,
                ActiveTrains = trains,
                ActiveRoutes = routes,
                TotalSchedules = schedules,
                TicketsSoldToday = ticketsSold,
                RevenueToday = revenue
            };
        }

        public async Task<System.Collections.IList> GetAdminRecentTicketsAsync()
        {
            return await _db.Tickets
                .OrderByDescending(t => t.CreatedAt)
                .Take(20)
                .Select(t => new
                {
                    MaVe = t.TicketCode,
                    HanhKhach = t.PassengerName,
                    SoDienThoai = t.PassengerPhone,
                    NgayDat = t.CreatedAt,
                    GiaVe = t.FinalPrice,
                    TrangThai = t.Status
                })
                .ToListAsync();
        }
    }
}
