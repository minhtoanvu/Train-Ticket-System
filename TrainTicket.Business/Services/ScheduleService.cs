using System.Data;
using Microsoft.EntityFrameworkCore;
using TrainTicket.Business.DTOs;
using TrainTicket.Business.Interfaces;
using TrainTicket.Data.ADO;
using TrainTicket.Data.DbContexts;

namespace TrainTicket.Business.Services
{
    /// <summary>
    /// Nghiep vu tim chuyen tau va xem so do ghe theo lich trinh.
    /// Su dung AdoHelper de goi Stored Procedure.
    /// </summary>
    public class ScheduleService : IScheduleService
    {
        private readonly AdoHelper _adoHelper;
        private readonly TrainTicketDbContext _context;

        public ScheduleService(AdoHelper adoHelper, TrainTicketDbContext context)
        {
            _adoHelper = adoHelper;
            _context = context;
        }

        /// <inheritdoc/>
                public async Task<System.Collections.IList> GetAllStationsAsync()
        {
            return await _context.Stations
                .Where(x => x.IsActive == true)
                .OrderBy(x => x.StationName)
                .Select(x => new { x.StationId, x.StationName })
                .ToListAsync();
        }

        public async Task<DataTable> SearchSchedulesAsync(SearchScheduleDto request)
        {
            var parameters = new Dictionary<string, object?>
            {
                ["@GaDi"]   = request.GaDi,
                ["@GaDen"]  = request.GaDen,
                ["@NgayDi"] = request.NgayDi.Date
            };

            return await _adoHelper.ExecuteStoredProcedureAsync("sp_TimChuyenTau", parameters);
        }

        /// <inheritdoc/>
        public async Task<List<SeatMapDto>> GetSeatMapAsync(int scheduleId)
        {
            var parameters = new Dictionary<string, object?> { ["@ScheduleID"] = scheduleId };
            var table = await _adoHelper.ExecuteStoredProcedureAsync("sp_XemSoDoGhe", parameters);

            return table.Rows.Cast<System.Data.DataRow>().Select(row => new SeatMapDto
            {
                MaToa     = row.Field<string>("MaToa")    ?? string.Empty,
                LoaiToa   = row.Field<string>("LoaiToa")  ?? string.Empty,
                SoGhe     = row.Field<string>("SoGhe")    ?? string.Empty,
                LoaiGhe   = row.Field<string>("LoaiGhe")  ?? string.Empty,
                HangGhe   = row.Field<string>("HangGhe")  ?? "Economy",
                HasSocket = row["CoOCam"]  != DBNull.Value && Convert.ToBoolean(row["CoOCam"]),
                SeatID    = row["SeatID"]  == DBNull.Value ? 0 : Convert.ToInt32(row["SeatID"]),
                TrangThai = row.Field<string>("TrangThai") ?? string.Empty,
                GiaVe     = row["GiaVe"]   == DBNull.Value ? 0 : Convert.ToDecimal(row["GiaVe"])
            }).ToList();
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateScheduleStatusAsync(int scheduleId, string status, int? delayMinutes = null)
        {
            var parameters = new Dictionary<string, object?>
            {
                ["@ScheduleID"]   = scheduleId,
                ["@Status"]       = status,
                ["@DelayMinutes"] = (object?)delayMinutes ?? DBNull.Value
            };

            await _adoHelper.ExecuteNonQueryAsync(
                "UPDATE Schedules SET Status=@Status, DelayMinutes=ISNULL(@DelayMinutes,DelayMinutes) WHERE ScheduleID=@ScheduleID",
                parameters);

            return true;
        }
    }
}