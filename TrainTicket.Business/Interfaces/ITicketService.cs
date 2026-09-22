using System.Data;
using TrainTicket.Business.DTOs;

namespace TrainTicket.Business.Interfaces
{
    /// <summary>
    /// Nghiep vu ve tau: dat ve, huy ve, xac nhan thanh toan, check-in.
    /// </summary>
    public interface ITicketService
    {
        /// <summary>Dat ve moi qua stored procedure sp_DatVe. Tra ve null neu that bai.</summary>
        Task<BookTicketResultDto?> BookTicketAsync(BookTicketRequestDto request);

        /// <summary>Huy ve va tinh hoan tien theo chinh sach.</summary>
        Task<CancelTicketResultDto> CancelTicketAsync(int ticketId, int userId, string? cancelReason = null);

        /// <summary>Xac nhan thanh toan cho ve. Tra ve false neu SP bao that bai.</summary>
        Task<bool> ConfirmPaymentAsync(int ticketId, string? transactionId = null);

        /// <summary>Lay danh sach ve theo bo loc. Tat ca tham so la tuy chon.</summary>
        Task<DataTable> GetTicketsAsync(
            int? userId = null, string? status = null,
            DateTime? from = null, DateTime? to = null, string? ticketCode = null);

        /// <summary>Check-in ve theo ma ve. Tra ve true neu thanh cong.</summary>
        Task<bool> CheckInAsync(string ticketCode);

        /// <summary>Tinh gia ve sau khi ap dung ma giam gia.</summary>
        Task<decimal> CalculatePriceAsync(int scheduleId, string seatType, string? discountCode);

        /// <summary>Lay chi tiet ve theo ID, dung cho man hinh hien thi QR thanh toan.</summary>
        Task<BookTicketResultDto?> GetTicketByIdAsync(int ticketId);
        Task<TrainTicket.Data.Entities.Ticket?> GetTicketEntityAsync(int ticketId);
        Task<bool> UpdatePassengerInfoAsync(int ticketId, string name, string idNum, string phone);
    }
}