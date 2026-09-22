using TrainTicket.Business.DTOs;

namespace TrainTicket.Business.Interfaces
{
    /// <summary>
    /// Nghiep vu xac thuc nguoi dung: dang nhap, dang ky, doi mat khau, mo khoa tai khoan.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Dang nhap voi email va mat khau.
        /// Tra ve thong tin phien neu hop le; null neu sai thong tin.
        /// Nem <see cref="InvalidOperationException"/> neu tai khoan bi khoa.
        /// </summary>
        Task<UserSessionDto?> LoginAsync(LoginRequestDto request);

        /// <summary>Dang ky tai khoan moi. Tra ve false neu email da ton tai.</summary>
        Task<bool> RegisterAsync(RegisterRequestDto request);

        /// <summary>Doi mat khau. Nem exception neu mat khau cu sai.</summary>
        Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);

        /// <summary>Mo khoa tai khoan bi khoa (Admin).</summary>
        Task<bool> UnlockAccountAsync(int userId);
    }
}