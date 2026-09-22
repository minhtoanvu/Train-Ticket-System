using Microsoft.EntityFrameworkCore;
using TrainTicket.Business.DTOs;
using TrainTicket.Business.Interfaces;
using TrainTicket.Data.DbContexts;
using TrainTicket.Data.Entities;

namespace TrainTicket.Business.Services
{
    /// <summary>
    /// Quan ly danh muc: Tau, Ga, Tuyen duong, Lich trinh.
    /// Cac thao tac Create/Read/Update/Delete dung soft-delete (IsActive = false).
    /// </summary>
    public class CatalogService : ICatalogService
    {
        private readonly TrainTicketDbContext _db;

        public CatalogService(TrainTicketDbContext db) => _db = db;

        // ── TRAINS ────────────────────────────────────────

        /// <inheritdoc/>
        public async Task<List<TrainDto>> GetAllTrainsAsync()
        {
            var trains = await _db.Trains
                .Where(train => train.IsActive == true)
                .OrderBy(train => train.TrainCode)
                .ToListAsync();

            return trains.Select(MapToTrainDto).ToList();
        }

        /// <inheritdoc/>
        public async Task<TrainDto?> GetTrainByIdAsync(int id)
        {
            var train = await _db.Trains.FindAsync(id);
            return train == null ? null : MapToTrainDto(train);
        }

        /// <inheritdoc/>
        public async Task<bool> SaveTrainAsync(TrainDto dto)
        {
            if (dto.TrainId == 0)
            {
                var newTrain = new Train
                {
                    TrainCode = dto.TrainCode,
                    TrainName = dto.TrainName,
                    TrainType = dto.TrainType,
                    CreatedAt = DateTime.Now,
                    IsActive  = true
                };
                await _db.Trains.AddAsync(newTrain);
            }
            else
            {
                var existing = await _db.Trains.FindAsync(dto.TrainId);
                if (existing == null) return false;

                existing.TrainCode = dto.TrainCode;
                existing.TrainName = dto.TrainName;
                existing.TrainType = dto.TrainType;
            }

            await _db.SaveChangesAsync();
            return true;
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteTrainAsync(int id)
        {
            var train = await _db.Trains.FindAsync(id);
            if (train == null) return false;

            train.IsActive = false;
            await _db.SaveChangesAsync();
            return true;
        }

        // ── STATIONS ──────────────────────────────────────

        /// <inheritdoc/>
        public async Task<List<StationDto>> GetAllStationsAsync()
        {
            var stations = await _db.Stations
                .Where(station => station.IsActive == true)
                .OrderBy(station => station.City)
                .ThenBy(station => station.StationName)
                .ToListAsync();

            return stations.Select(MapToStationDto).ToList();
        }

        /// <inheritdoc/>
        public async Task<StationDto?> GetStationByIdAsync(int id)
        {
            var station = await _db.Stations.FindAsync(id);
            return station == null ? null : MapToStationDto(station);
        }

        /// <inheritdoc/>
        public async Task<bool> SaveStationAsync(StationDto dto)
        {
            if (dto.StationId == 0)
            {
                var newStation = new Station
                {
                    StationCode = dto.StationCode,
                    StationName = dto.StationName,
                    City        = dto.City,
                    Address     = dto.Address,
                    CreatedAt   = DateTime.Now,
                    IsActive    = true
                };
                await _db.Stations.AddAsync(newStation);
            }
            else
            {
                var existing = await _db.Stations.FindAsync(dto.StationId);
                if (existing == null) return false;

                existing.StationCode = dto.StationCode;
                existing.StationName = dto.StationName;
                existing.City        = dto.City;
                existing.Address     = dto.Address;
            }

            await _db.SaveChangesAsync();
            return true;
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteStationAsync(int id)
        {
            var station = await _db.Stations.FindAsync(id);
            if (station == null) return false;

            station.IsActive = false;
            await _db.SaveChangesAsync();
            return true;
        }

        // ── ROUTES ────────────────────────────────────────

        /// <inheritdoc/>
        public async Task<List<RouteDto>> GetAllRoutesAsync()
        {
            var routes = await _db.Routes
                .Include(route => route.DepartureStationNavigation)
                .Include(route => route.ArrivalStationNavigation)
                .Where(route => route.IsActive == true)
                .OrderBy(route => route.RouteName)
                .ToListAsync();

            return routes.Select(MapToRouteDto).ToList();
        }

        /// <inheritdoc/>
        public async Task<RouteDto?> GetRouteByIdAsync(int id)
        {
            var route = await _db.Routes
                .Include(r => r.DepartureStationNavigation)
                .Include(r => r.ArrivalStationNavigation)
                .FirstOrDefaultAsync(r => r.RouteId == id);

            return route == null ? null : MapToRouteDto(route);
        }

        /// <inheritdoc/>
        public async Task<bool> SaveRouteAsync(RouteDto dto)
        {
            if (dto.RouteId == 0)
            {
                var newRoute = new Data.Entities.Route
                {
                    RouteName        = dto.RouteName,
                    DepartureStation = dto.DepartureStation,
                    ArrivalStation   = dto.ArrivalStation,
                    Distance         = dto.Distance,
                    CreatedAt        = DateTime.Now,
                    IsActive         = true
                };
                await _db.Routes.AddAsync(newRoute);
            }
            else
            {
                var existing = await _db.Routes.FindAsync(dto.RouteId);
                if (existing == null) return false;

                existing.RouteName        = dto.RouteName;
                existing.DepartureStation = dto.DepartureStation;
                existing.ArrivalStation   = dto.ArrivalStation;
                existing.Distance         = dto.Distance;
            }

            await _db.SaveChangesAsync();
            return true;
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteRouteAsync(int id)
        {
            var route = await _db.Routes.FindAsync(id);
            if (route == null) return false;

            route.IsActive = false;
            await _db.SaveChangesAsync();
            return true;
        }

        // ── SCHEDULES ─────────────────────────────────────

        /// <inheritdoc/>
        public async Task<List<ScheduleDto>> GetAllSchedulesAsync()
        {
            var schedules = await _db.Schedules
                .Include(schedule => schedule.Train)
                .Include(schedule => schedule.Route)
                    .ThenInclude(route => route.DepartureStationNavigation)
                .Include(schedule => schedule.Route)
                    .ThenInclude(route => route.ArrivalStationNavigation)
                .Where(schedule => schedule.IsActive == true)
                .OrderByDescending(schedule => schedule.DepartureTime)
                .ToListAsync();

            return schedules.Select(MapToScheduleDto).ToList();
        }

        /// <inheritdoc/>
        public async Task<ScheduleDto?> GetScheduleByIdAsync(int id)
        {
            var schedule = await _db.Schedules
                .Include(sch => sch.Train)
                .Include(sch => sch.Route)
                    .ThenInclude(route => route.DepartureStationNavigation)
                .Include(sch => sch.Route)
                    .ThenInclude(route => route.ArrivalStationNavigation)
                .FirstOrDefaultAsync(sch => sch.ScheduleId == id);

            return schedule == null ? null : MapToScheduleDto(schedule);
        }

        /// <inheritdoc/>
        public async Task<bool> SaveScheduleAsync(ScheduleDto dto)
        {
            if (dto.ScheduleId == 0)
            {
                var newSchedule = new Schedule
                {
                    TrainId       = dto.TrainId,
                    RouteId       = dto.RouteId,
                    DepartureTime = dto.DepartureTime,
                    ArrivalTime   = dto.ArrivalTime,
                    Status        = dto.Status,
                    CreatedAt     = DateTime.Now,
                    IsActive      = true
                };
                await _db.Schedules.AddAsync(newSchedule);
            }
            else
            {
                var existing = await _db.Schedules.FindAsync(dto.ScheduleId);
                if (existing == null) return false;

                existing.TrainId       = dto.TrainId;
                existing.RouteId       = dto.RouteId;
                existing.DepartureTime = dto.DepartureTime;
                existing.ArrivalTime   = dto.ArrivalTime;
                existing.Status        = dto.Status;
            }

            await _db.SaveChangesAsync();
            return true;
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteScheduleAsync(int id)
        {
            var schedule = await _db.Schedules.FindAsync(id);
            if (schedule == null) return false;

            schedule.IsActive = false;
            await _db.SaveChangesAsync();
            return true;
        }

        // ── Private Mappers ───────────────────────────────

        private static TrainDto MapToTrainDto(Train train) => new()
        {
            TrainId   = train.TrainId,
            TrainCode = train.TrainCode,
            TrainName = train.TrainName,
            TrainType = train.TrainType,
            IsActive  = train.IsActive ?? false,
            CreatedAt = train.CreatedAt ?? DateTime.Now
        };

        private static StationDto MapToStationDto(Station station) => new()
        {
            StationId   = station.StationId,
            StationCode = station.StationCode,
            StationName = station.StationName,
            City        = station.City,
            Address     = station.Address,
            IsActive    = station.IsActive ?? false,
            CreatedAt   = station.CreatedAt ?? DateTime.Now
        };

        private static RouteDto MapToRouteDto(Data.Entities.Route route) => new()
        {
            RouteId              = route.RouteId,
            RouteName            = route.RouteName,
            DepartureStation     = route.DepartureStation,
            ArrivalStation       = route.ArrivalStation,
            Distance             = route.Distance,
            IsActive             = route.IsActive ?? false,
            CreatedAt            = route.CreatedAt ?? DateTime.Now,
            DepartureStationName = route.DepartureStationNavigation?.StationName ?? string.Empty,
            ArrivalStationName   = route.ArrivalStationNavigation?.StationName ?? string.Empty
        };

        private static ScheduleDto MapToScheduleDto(Schedule schedule) => new()
        {
            ScheduleId    = schedule.ScheduleId,
            TrainId       = schedule.TrainId,
            RouteId       = schedule.RouteId,
            DepartureTime = schedule.DepartureTime,
            ArrivalTime   = schedule.ArrivalTime,
            Status        = schedule.Status ?? "Scheduled",
            IsActive      = schedule.IsActive ?? false,
            CreatedAt     = schedule.CreatedAt ?? DateTime.Now,
            TrainName     = schedule.Train?.TrainName ?? string.Empty,
            RouteName     = schedule.Route?.RouteName ?? string.Empty
        };
    }
}