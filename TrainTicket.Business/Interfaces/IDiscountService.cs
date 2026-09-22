using TrainTicket.Business.DTOs;

namespace TrainTicket.Business.Interfaces
{
    /// <summary>Nghiep vu ma giam gia.</summary>
    public interface IDiscountService
    {
        /// <summary>Lay thong tin ma giam gia dang hoat dong theo code. Tra ve null neu khong hop le.</summary>
        Task<DiscountDto?> GetDiscountByCodeAsync(string code);

        /// <summary>Tang so lan su dung cua ma giam gia khi dat ve thanh cong.</summary>
        Task<bool> ApplyDiscountAsync(string code);

        /// <summary>Lay danh sach tat ca ma giam gia dang hoat dong va con han.</summary>
        Task<IEnumerable<DiscountDto>> GetActiveDiscountsAsync();
    }
}