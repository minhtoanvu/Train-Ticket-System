using System.Data;
using TrainTicket.Business.Constants;
using TrainTicket.Business.DTOs;
using TrainTicket.Business.Interfaces;
using TrainTicket.Data.ADO;
using TrainTicket.Data.DbContexts;

namespace TrainTicket.Business.Services
{
    /// <summary>
    /// Xu ly nghiep vu ve tau: dat ve, huy ve, xac nhan thanh toan, check-in.
    /// Su dung ADO.NET (AdoHelper) de goi cac Stored Procedure trong DB.
    /// </summary>
    public class TicketService : ITicketService
    {
        private readonly AdoHelper _adoHelper;
        private readonly TrainTicketDbContext _context;

        public TicketService(AdoHelper adoHelper, TrainTicketDbContext context)
        {
            _adoHelper = adoHelper;
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<BookTicketResultDto?> BookTicketAsync(BookTicketRequestDto request)
        {
            var parameters = new Dictionary<string, object?>
            {
                ["@UserID"]          = request.UserID,
                ["@ScheduleID"]      = request.ScheduleID,
                ["@SeatIDs"]         = request.SeatID.ToString(),
                ["@PassengerNames"]  = request.PassengerName,
                ["@PassengerIDs"]    = request.PassengerID,
                ["@PassengerPhones"] = (object?)request.PassengerPhone ?? DBNull.Value,
                ["@PaymentMethod"]   = request.PaymentMethod,
                ["@DiscountCode"]    = (object?)request.DiscountCode ?? DBNull.Value,
            };

            var table = await _adoHelper.ExecuteStoredProcedureAsync("sp_DatVe", parameters);
            if (table.Rows.Count == 0) return null;

            var row = table.Rows[0];
            return new BookTicketResultDto
            {
                TicketID      = row.Field<int>("TicketID"),
                TicketCode    = row.Field<string>("TicketCode") ?? string.Empty,
                PassengerName = row.Field<string>("PassengerName") ?? string.Empty,
                GiaVe         = row.Field<decimal>("FinalPrice"),
                OriginalPrice = row.Field<decimal>("FinalPrice"),
                DiscountAmount = 0,
                SoGhe         = row.Field<string>("SeatNumber") ?? string.Empty,
                MaToa         = row.Field<string>("CarriageCode") ?? string.Empty,
                GaDi          = row.Field<string>("GaDi") ?? string.Empty,
                GaDen         = row.Field<string>("GaDen") ?? string.Empty,
                SeatType      = string.Empty,
                TrangThaiVe   = TicketStatus.Pending,
                GioDi         = DateTime.MinValue,
                GioDen        = DateTime.MinValue,
                MaTau         = string.Empty,
            };
        }

        /// <inheritdoc/>
        public async Task<CancelTicketResultDto> CancelTicketAsync(
            int ticketId, int userId, string? cancelReason = null)
        {
            var parameters = new Dictionary<string, object?>
            {
                ["@TicketID"]    = ticketId,
                ["@UserID"]      = userId,
                ["@CancelReason"] = (object?)cancelReason ?? DBNull.Value
            };

            try
            {
                var table = await _adoHelper.ExecuteStoredProcedureAsync("sp_HuyVe", parameters);
                if (table.Rows.Count == 0)
                    return new CancelTicketResultDto { Success = false, Message = "Khong the huy ve." };

                var row           = table.Rows[0];
                decimal refundPct = row.Field<decimal>("RefundPercent");
                decimal refundAmt = row.Field<decimal>("RefundAmount");

                return new CancelTicketResultDto
                {
                    Success       = row.Field<int>("Success") == 1,
                    RefundPercent = refundPct,
                    RefundAmount  = refundAmt,
                    Message       = $"Hoan tien {refundPct:F0}% = {refundAmt:N0} VND"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CancelTicket] {ex.Message}");
                return new CancelTicketResultDto
                {
                    Success = false,
                    Message = "He thong dang ban hoac loi ket noi. Vui long thu lai sau."
                };
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ConfirmPaymentAsync(int ticketId, string? transactionId = null)
        {
            var parameters = new Dictionary<string, object?>
            {
                ["@TicketID"]     = ticketId,
                ["@TransactionID"] = (object?)transactionId ?? DBNull.Value
            };

            try
            {
                var table = await _adoHelper.ExecuteStoredProcedureAsync("sp_XacNhanThanhToan", parameters);
                if (table.Rows.Count == 0) return false;

                // SP co the tra ve cot Success (1/0) hoac chi tra ve 1 row de bao thanh cong
                if (table.Columns.Contains("Success"))
                    return table.Rows[0].Field<int>("Success") == 1;

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfirmPayment] {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<DataTable> GetTicketsAsync(
            int? userId     = null,
            string? status  = null,
            DateTime? from  = null,
            DateTime? to    = null,
            string? ticketCode = null)
        {
            var parameters = new Dictionary<string, object?>
            {
                ["@UserID"]  = (object?)userId ?? DBNull.Value,
                ["@Status"]  = (object?)status ?? DBNull.Value,
                ["@TuNgay"]  = (object?)from?.Date ?? DBNull.Value,
                ["@DenNgay"] = (object?)to?.Date ?? DBNull.Value,
                ["@MaVe"]    = (object?)ticketCode ?? DBNull.Value,
            };

            return await _adoHelper.ExecuteStoredProcedureAsync("sp_LayDanhSachVe", parameters);
        }

        /// <inheritdoc/>
        public async Task<bool> CheckInAsync(string ticketCode)
        {
            var parameters = new Dictionary<string, object?> { ["@TicketCode"] = ticketCode };
            try
            {
                var table = await _adoHelper.ExecuteStoredProcedureAsync("sp_CheckIn", parameters);
                return table.Rows.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<decimal> CalculatePriceAsync(int scheduleId, string seatType, string? discountCode)
        {
            var parameters = new Dictionary<string, object?>
            {
                ["@ScheduleID"]  = scheduleId,
                ["@SeatType"]    = seatType,
                ["@DiscountCode"] = (object?)discountCode ?? DBNull.Value
            };

            var table = await _adoHelper.ExecuteQueryAsync(
                @"SELECT dbo.fn_TinhGiaVe(
                    (SELECT Price FROM SchedulePrices WHERE ScheduleID=@ScheduleID AND SeatType=@SeatType),
                    @DiscountCode) AS FinalPrice", parameters);

            if (table.Rows.Count == 0 || table.Rows[0]["FinalPrice"] == DBNull.Value)
                return 0m;

            return Convert.ToDecimal(table.Rows[0]["FinalPrice"]);
        }

        /// <inheritdoc/>
                public async Task<TrainTicket.Data.Entities.Ticket?> GetTicketEntityAsync(int ticketId)
        {
            return await _context.Tickets.FindAsync(ticketId);
        }

        public async Task<bool> UpdatePassengerInfoAsync(int ticketId, string name, string idNum, string phone)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null) return false;
            ticket.PassengerName = name;
            ticket.PassengerId = idNum;
            ticket.PassengerPhone = phone;
            ticket.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<BookTicketResultDto?> GetTicketByIdAsync(int ticketId)
        {
            var parameters = new Dictionary<string, object?> { ["@TicketID"] = ticketId };

            var table = await _adoHelper.ExecuteQueryAsync(
                "SELECT TicketID, TicketCode, FinalPrice FROM Tickets WHERE TicketID = @TicketID",
                parameters);

            if (table.Rows.Count == 0) return null;

            var row = table.Rows[0];
            return new BookTicketResultDto
            {
                TicketID   = row.Field<int>("TicketID"),
                TicketCode = row.Field<string>("TicketCode") ?? string.Empty,
                GiaVe      = row.Field<decimal>("FinalPrice")
            };
        }
    }
}