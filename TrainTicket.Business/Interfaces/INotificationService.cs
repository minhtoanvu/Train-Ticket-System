using System.Collections.Generic;
using System.Threading.Tasks;

namespace TrainTicket.Business.Interfaces
{
    public interface INotificationService
    {
        Task<int> GetUnreadCountAsync(int userId);
        Task<System.Collections.IList> GetRecentNotificationsAsync(int userId, int limit = 10);
        Task MarkAsReadAsync(int userId);
    }
}