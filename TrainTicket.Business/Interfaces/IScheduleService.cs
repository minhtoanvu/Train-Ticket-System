using System.Data;
using TrainTicket.Business.DTOs;

namespace TrainTicket.Business.Interfaces
{
    /// <summary>
    /// Nghiep vu tim kiem chuyen tau va quan ly so do ghe.
    /// </summary>
    public interface IScheduleService
    {
        /// <summary>Tim chuyen theo ga di, ga den, ngay di.</summary>
        Task<System.Collections.IList> GetAllStationsAsync();
        Task<DataTable> SearchSchedulesAsync(SearchScheduleDto request);

        /// <summary>Lay danh sach ghe va trang thai cua mot chuyen tau.</summary>
        Task<List<SeatMapDto>> GetSeatMapAsync(int scheduleId);

        /// <summary>Cap nhat trang thai chuyen tau (On Time / Delayed / Cancelled).</summary>
        Task<bool> UpdateScheduleStatusAsync(int scheduleId, string status, int? delayMinutes = null);
    }
}