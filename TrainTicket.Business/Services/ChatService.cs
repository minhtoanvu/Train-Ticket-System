using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TrainTicket.Business.Interfaces;
using TrainTicket.Data.DbContexts;
using TrainTicket.Data.Entities;
using System;

namespace TrainTicket.Business.Services
{
    public class ChatService : IChatService
    {
        private readonly TrainTicketDbContext _db;
        public ChatService(TrainTicketDbContext db) => _db = db;

        public async Task<List<ChatPartnerDto>> GetPartnersAsync(int userId, bool isCustomer, bool isStaff)
        {
            var partners = new List<ChatPartnerDto>();

            if (isCustomer && !isStaff)
            {
                var staffUsers = await _db.Users
                    .Where(u => u.UserRoles.Any(r => r.Role.RoleName == "Staff" || r.Role.RoleName == "Admin")
                             && u.IsDeleted == false && u.IsActive == true)
                    .Select(u => new ChatPartnerDto { Id = u.UserId, Name = u.FullName })
                    .ToListAsync();
                partners.AddRange(staffUsers);
            }
            else if (isStaff)
            {
                var customers = await _db.Users
                    .Where(u => u.UserRoles.Any(r => r.Role.RoleName == "Customer" || r.Role.RoleName == "User")
                             && u.IsDeleted == false)
                    .Select(u => new ChatPartnerDto { Id = u.UserId, Name = u.FullName })
                    .ToListAsync();
                partners.AddRange(customers);
            }

            return partners;
        }

        public async Task<List<ChatMessageDto>> GetUnreadMessagesAsync(int currentUserId, int partnerId)
        {
            return await _db.ChatMessages
                .Where(m => m.SenderId == partnerId && m.ReceiverId == currentUserId && m.IsRead == false)
                .Select(m => new ChatMessageDto
                {
                    MessageId = m.MessageId,
                    SenderId = m.SenderId,
                    ReceiverId = m.ReceiverId,
                    Content = m.Content ?? "",
                    IsRead = m.IsRead ?? false,
                    SentAt = m.SentAt ?? DateTime.Now
                })
                .ToListAsync();
        }

        public async Task MarkMessagesAsReadAsync(List<int> messageIds)
        {
            var messages = await _db.ChatMessages.Where(m => messageIds.Contains(m.MessageId)).ToListAsync();
            foreach (var m in messages) m.IsRead = true;
            if (messages.Any()) await _db.SaveChangesAsync();
        }

        public async Task<List<ChatMessageDto>> GetMessagesAsync(int currentUserId, int partnerId)
        {
            return await _db.ChatMessages
                .AsNoTracking()
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == partnerId)
                         || (m.SenderId == partnerId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.SentAt)
                .Select(m => new ChatMessageDto
                {
                    MessageId = m.MessageId,
                    SenderId = m.SenderId,
                    ReceiverId = m.ReceiverId,
                    Content = m.Content ?? "",
                    IsRead = m.IsRead ?? false,
                    SentAt = m.SentAt ?? DateTime.Now
                })
                .ToListAsync();
        }

        public async Task SendMessageAsync(int senderId, int receiverId, string content)
        {
            _db.ChatMessages.Add(new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content,
                IsRead = false,
                SentAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
        }
    }
}
