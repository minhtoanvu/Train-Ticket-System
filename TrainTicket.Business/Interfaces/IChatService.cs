using System.Collections.Generic;
using System.Threading.Tasks;

namespace TrainTicket.Business.Interfaces
{
    public class ChatPartnerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ChatMessageDto
    {
        public int MessageId { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public System.DateTime SentAt { get; set; }
    }

    public interface IChatService
    {
        Task<List<ChatPartnerDto>> GetPartnersAsync(int userId, bool isCustomer, bool isStaff);
        Task<List<ChatMessageDto>> GetUnreadMessagesAsync(int currentUserId, int partnerId);
        Task MarkMessagesAsReadAsync(List<int> messageIds);
        Task<List<ChatMessageDto>> GetMessagesAsync(int currentUserId, int partnerId);
        Task SendMessageAsync(int senderId, int receiverId, string content);
    }
}
