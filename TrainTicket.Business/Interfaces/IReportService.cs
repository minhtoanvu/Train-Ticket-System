using System.Data;
using TrainTicket.Business.DTOs;

namespace TrainTicket.Business.Interfaces
{
    /// <summary>Nghiep vu lay du lieu bao cao doanh thu va thong ke.</summary>
    public interface IReportService
    {
        /// <summary>Bao cao doanh thu co the loc theo nam, thang, tuyen.</summary>
        Task<DataTable> GetRevenueReportAsync(ReportFilterDto filter);

        /// <summary>Top N tuyen duong co doanh thu cao nhat trong nam.</summary>
        Task<DataTable> GetTopRoutesAsync(int year, int topN = 5);

        /// <summary>Thong ke ve va doanh thu trong ngay cu thế.</summary>
        Task<DataTable> GetDailySummaryAsync(DateTime date);

        Task<System.Collections.IList> GetPaymentHistoryAsync(string? method, string? status, string? keyword);
    }
}