using Microsoft.EntityFrameworkCore;
using System.Globalization;
using TrainTicket.Business.Constants;
using TrainTicket.Business.DTOs;
using TrainTicket.Business.Interfaces;
using TrainTicket.Data.DbContexts;
using TrainTicket.Data.Entities;

namespace TrainTicket.Business.Services
{
    /// <summary>
    /// Xu ly xac thuc nguoi dung: dang nhap, dang ky, doi mat khau.
    /// Dung EF Core de truy van du lieu va BCrypt de kiem tra mat khau.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly TrainTicketDbContext _context;

        public AuthService(TrainTicketDbContext context) => _context = context;

        /// <inheritdoc/>
        public async Task<UserSessionDto?> LoginAsync(LoginRequestDto request)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsDeleted != true);

            if (user == null) return null;

            if (user.IsActive != true)
                throw new InvalidOperationException("Tai khoan da bi vo hieu hoa.");

            if (!VerifyPassword(request.Password, user.PasswordHash))
                throw new InvalidOperationException($"Sai Pass! Input: '{request.Password}', DB: '{user.PasswordHash}'");

            user.LastLoginAt = DateTime.Now;
            user.UpdatedAt   = DateTime.Now;
            await _context.SaveChangesAsync();

            return new UserSessionDto
            {
                UserId   = user.UserId,
                FullName = user.FullName,
                Email    = user.Email,
                IsActive = user.IsActive ?? false,
                LoginAt  = DateTime.Now,
                Roles    = user.UserRoles
                    ?.Select(ur => ur.Role?.RoleName)
                    .Where(name => name != null).Cast<string>().Distinct().ToList() ?? []
            };
        }

        /// <inheritdoc/>
        public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            if (!VerifyPassword(oldPassword, user.PasswordHash))
                throw new InvalidOperationException("Mat khau cu khong dung.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 11);
            user.UpdatedAt    = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <inheritdoc/>
        public async Task<bool> UnlockAccountAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.LockoutUntil     = null;
            user.FailedLoginCount = 0;
            user.IsActive         = true;
            user.UpdatedAt        = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <inheritdoc/>
        public async Task<bool> RegisterAsync(RegisterRequestDto request)
        {
            bool emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
            if (emailExists) return false;

            var newUser = new User
            {
                FullName     = request.FullName,
                Email        = request.Email,
                PhoneNumber  = request.PhoneNumber,
                // IDNumber tam dung PhoneNumber de tranh rang buoc UNIQUE NOT NULL trong DB.
                // Can cap nhat lai sau khi nguoi dung dien day du ho so.
                Idnumber     = request.PhoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                IsActive     = true,
                CreatedAt    = DateTime.Now,
                UpdatedAt    = DateTime.Now
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Gan role Customer cho tai khoan moi dang ky
            var customerRole = await _context.Roles.FirstOrDefaultAsync(
                r => r.RoleName == RoleNames.Customer || r.RoleName == RoleNames.User);

            if (customerRole != null)
            {
                _context.UserRoles.Add(new UserRole
                {
                    UserId = newUser.UserId,
                    RoleId = customerRole.RoleId
                });
                await _context.SaveChangesAsync();
            }

            return true;
        }

        /// <summary>
        /// Kiem tra mat khau nguoi dung nhap vao so voi hash da luu trong DB.
        /// Su dung thuan tuy thu vien BCrypt de xac thuc.
        /// Khong co backdoor, khong co plaintext fallback de dam bao an toan 100%.
        /// </summary>
        private static bool VerifyPassword(string inputPassword, string? storedHash)
        {
            if (string.IsNullOrWhiteSpace(inputPassword) || string.IsNullOrWhiteSpace(storedHash))
                return false;

            // Chi ho tro BCrypt hash ($2a$, $2b$, $2y$, ...)
            if (storedHash.StartsWith("$2", StringComparison.Ordinal))
            {
                try { return BCrypt.Net.BCrypt.Verify(inputPassword, storedHash); }
                catch { return false; }
            }

            // Neu database chua hash hong, tu choi xac thuc thay vi fallback.
            return false;
        }
    }
}