using Microsoft.EntityFrameworkCore;
using TrainTicket.Business.DTOs;
using TrainTicket.Business.Interfaces;
using TrainTicket.Data.DbContexts;
using TrainTicket.Data.Entities;

namespace TrainTicket.Business.Services
{
    /// <summary>
    /// Quan ly ma giam gia: tra cuu, ap dung va lay danh sach dang hoat dong.
    /// </summary>
    public class DiscountService : IDiscountService
    {
        private readonly TrainTicketDbContext _context;

        public DiscountService(TrainTicketDbContext context) => _context = context;

        /// <inheritdoc/>
        public async Task<DiscountDto?> GetDiscountByCodeAsync(string code)
        {
            var now = DateTime.Now;
            var discount = await _context.Discounts.FirstOrDefaultAsync(
                d => d.Code == code
                  && d.IsActive == true
                  && d.ValidFrom <= now
                  && d.ValidTo   >= now);

            return discount == null ? null : MapToDto(discount);
        }

        /// <inheritdoc/>
        public async Task<bool> ApplyDiscountAsync(string code)
        {
            var now = DateTime.Now;
            var discount = await _context.Discounts.FirstOrDefaultAsync(d => d.Code == code);

            if (discount == null || discount.IsActive != true || discount.ValidTo < now)
                return false;

            if (discount.MaxUses.HasValue && discount.UsedCount >= discount.MaxUses.Value)
                return false;

            discount.UsedCount++;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<DiscountDto>> GetActiveDiscountsAsync()
        {
            var now = DateTime.Now;
            var discounts = await _context.Discounts
                .Where(d => d.IsActive == true && d.ValidFrom <= now && d.ValidTo >= now)
                .ToListAsync();

            return discounts.Select(MapToDto);
        }

        private static DiscountDto MapToDto(Discount d) => new()
        {
            DiscountId   = d.DiscountId,
            Code         = d.Code,
            Description  = d.Description,
            DiscountType = d.DiscountType,
            Amount       = d.Amount,
            MinPrice     = d.MinPrice ?? 0m,
            MaxUses      = d.MaxUses,
            UsedCount    = d.UsedCount ?? 0,
            ValidFrom    = d.ValidFrom,
            ValidTo      = d.ValidTo,
            IsActive     = d.IsActive ?? false,
            CreatedAt    = d.CreatedAt ?? DateTime.Now
        };
    }
}