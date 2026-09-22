using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TrainTicket.Business.Interfaces;
using TrainTicket.Data.DbContexts;

namespace TrainTicket.Business.Services
{
    public class NotificationService : INotificationService
    {
        private readonly TrainTicketDbContext _context;

        public NotificationService(TrainTicketDbContext context)
        {
            _context = context;
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications
                .AsNoTracking()
                .CountAsync(n => n.UserId == userId && n.IsRead == false);
        }

        public async Task<System.Collections.IList> GetRecentNotificationsAsync(int userId, int limit = 10)
        {
            return await _context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .Select(n => new { n.Title, n.Body, n.CreatedAt, n.IsRead })
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(int userId)
        {
            var unread = await _context.Notifications
                .Where(n => n.UserId == userId && n.IsRead == false)
                .ToListAsync();
            unread.ForEach(n => n.IsRead = true);
            if (unread.Any()) await _context.SaveChangesAsync();
        }
    }
}