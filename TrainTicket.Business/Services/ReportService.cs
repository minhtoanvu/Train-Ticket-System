using System.Data;
using Microsoft.EntityFrameworkCore;
using TrainTicket.Business.Constants;
using TrainTicket.Business.DTOs;
using TrainTicket.Business.Interfaces;
using TrainTicket.Data.ADO;
using TrainTicket.Data.DbContexts;

namespace TrainTicket.Business.Services
{
    /// <summary>
    /// Lay du lieu bao cao doanh thu, tuyen duong va thong ke ngay.
    /// Su dung AdoHelper de goi Stored Procedure va truy van nhanh.
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly AdoHelper _adoHelper;
        private readonly TrainTicketDbContext _context;

        public ReportService(AdoHelper adoHelper, TrainTicketDbContext context)
        {
            _adoHelper = adoHelper;
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<DataTable> GetRevenueReportAsync(ReportFilterDto filter)
        {
            var parameters = new Dictionary<string, object?>
            {
                ["@Nam"]     = filter.Year,
                ["@Thang"]   = filter.Month,
                ["@RouteID"] = filter.RouteID
            };

            return await _adoHelper.ExecuteStoredProcedureAsync("sp_BaoCaoDoanhThu", parameters);
        }

        /// <inheritdoc/>
        public async Task<DataTable> GetTopRoutesAsync(int year, int topN = 5)
        {
            var parameters = new Dictionary<string, object?>
            {
                ["@Nam"]  = year,
                ["@TopN"] = topN
            };

            return await _adoHelper.ExecuteQueryAsync(
                @"SELECT TOP (@TopN)
                    r.RouteName,
                    COUNT(*)          AS SoVe,
                    SUM(t.FinalPrice) AS DoanhThu
                  FROM Tickets t
                  JOIN Schedules sc ON t.ScheduleID = sc.ScheduleID
                  JOIN Routes    r  ON sc.RouteID   = r.RouteID
                  WHERE YEAR(t.BookedAt) = @Nam
                    AND t.Status IN ('Confirmed', 'Used')
                  GROUP BY r.RouteName
                  ORDER BY DoanhThu DESC",
                parameters);
        }

        /// <inheritdoc/>
        public async Task<DataTable> GetDailySummaryAsync(DateTime date)
        {
            var parameters = new Dictionary<string, object?> { ["@Date"] = date.Date };

            return await _adoHelper.ExecuteQueryAsync(
                @"SELECT
                    COUNT(CASE WHEN Status IN ('Confirmed','Used') THEN 1 END) AS SoVe,
                    SUM(CASE WHEN Status IN ('Confirmed','Used') THEN FinalPrice ELSE 0 END) AS DoanhThu,
                    COUNT(CASE WHEN Status = 'Cancelled' THEN 1 END) AS SoVeHuy
                  FROM Tickets
                  WHERE CAST(BookedAt AS DATE) = @Date",
                parameters);
        }

        public async Task<System.Collections.IList> GetPaymentHistoryAsync(string? method, string? status, string? keyword)
        {
            var query = _context.Payments.Include(p => p.Ticket).AsQueryable();
            if (method != null) query = query.Where(p => p.PaymentMethod == method);
            if (status != null) query = query.Where(p => p.Status == status);
            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(p => p.Ticket.TicketCode.ToLower().Contains(keyword)
                                      || (p.TransactionId != null && p.TransactionId.ToLower().Contains(keyword)));
                                      
            return await query
                .OrderByDescending(p => p.PaidAt)
                .Select(p => new
                {
                    PaymentId     = p.PaymentId,
                    TicketCode    = p.Ticket.TicketCode,
                    Amount        = p.Amount,
                    PaymentMethod = p.PaymentMethod,
                    Status        = p.Status,
                    TransactionId = p.TransactionId ?? "➖",
                    PaidAt        = p.PaidAt,
                    RefundAmount  = p.RefundAmount,
                    Note          = p.Note ?? "➖"
                })
                .ToListAsync();
        }
    }
}