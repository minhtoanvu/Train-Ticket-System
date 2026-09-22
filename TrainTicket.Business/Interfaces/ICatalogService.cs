using TrainTicket.Business.DTOs;

namespace TrainTicket.Business.Interfaces
{
    /// <summary>
    /// Quan ly danh muc he thong: Tau, Ga, Tuyen duong, Lich trinh.
    /// </summary>
    public interface ICatalogService
    {
        // ── TRAINS ──────────────────────────────────────────
        /// <summary>Lay danh sach tau dang hoat dong.</summary>
        Task<List<TrainDto>> GetAllTrainsAsync();
        /// <summary>Lay thong tin tau theo ID. Tra ve null neu khong tim thay.</summary>
        Task<TrainDto?> GetTrainByIdAsync(int id);
        /// <summary>Tao moi (ID=0) hoac cap nhat tau. Tra ve false neu khong tim thay ban ghi.</summary>
        Task<bool> SaveTrainAsync(TrainDto train);
        /// <summary>Xoa mem tau theo ID (IsActive = false).</summary>
        Task<bool> DeleteTrainAsync(int id);

        // ── STATIONS ────────────────────────────────────────
        /// <summary>Lay danh sach ga dang hoat dong.</summary>
        Task<List<StationDto>> GetAllStationsAsync();
        /// <summary>Lay thong tin ga theo ID. Tra ve null neu khong tim thay.</summary>
        Task<StationDto?> GetStationByIdAsync(int id);
        /// <summary>Tao moi hoac cap nhat ga.</summary>
        Task<bool> SaveStationAsync(StationDto station);
        /// <summary>Xoa mem ga.</summary>
        Task<bool> DeleteStationAsync(int id);

        // ── ROUTES ──────────────────────────────────────────
        /// <summary>Lay danh sach tuyen duong dang hoat dong.</summary>
        Task<List<RouteDto>> GetAllRoutesAsync();
        /// <summary>Lay thong tin tuyen duong theo ID.</summary>
        Task<RouteDto?> GetRouteByIdAsync(int id);
        /// <summary>Tao moi hoac cap nhat tuyen duong.</summary>
        Task<bool> SaveRouteAsync(RouteDto route);
        /// <summary>Xoa mem tuyen duong.</summary>
        Task<bool> DeleteRouteAsync(int id);

        // ── SCHEDULES ───────────────────────────────────────
        /// <summary>Lay danh sach lich trinh dang hoat dong.</summary>
        Task<List<ScheduleDto>> GetAllSchedulesAsync();
        /// <summary>Lay thong tin lich trinh theo ID.</summary>
        Task<ScheduleDto?> GetScheduleByIdAsync(int id);
        /// <summary>Tao moi hoac cap nhat lich trinh.</summary>
        Task<bool> SaveScheduleAsync(ScheduleDto schedule);
        /// <summary>Xoa mem lich trinh.</summary>
        Task<bool> DeleteScheduleAsync(int id);
    }
}