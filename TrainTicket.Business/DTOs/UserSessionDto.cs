using TrainTicket.Business.Constants;

namespace TrainTicket.Business.DTOs
{
    /// <summary>
    /// Thong tin phien lam viec sau khi dang nhap thanh cong.
    /// Duoc luu trong SessionManager va chia se xuong cac Form.
    /// </summary>
    public class UserSessionDto
    {
        public int          UserId   { get; set; }
        public string       FullName { get; set; } = string.Empty;
        public string       Email    { get; set; } = string.Empty;
        public bool         IsActive { get; set; }
        public List<string> Roles    { get; set; } = [];
        public DateTime     LoginAt  { get; set; } = DateTime.Now;

        // Chu cai dau ten hien thi tren avatar
        public string AvatarLetter =>
            FullName.Length > 0 ? FullName[0].ToString().ToUpper() : "?";

        // Helpers kiem tra quyen
        public bool IsAdmin    => Roles.Contains(RoleNames.Admin);
        public bool IsStaff    => Roles.Contains(RoleNames.Staff) || IsAdmin;
        public bool IsCustomer => Roles.Contains(RoleNames.Customer) || Roles.Contains(RoleNames.User);
    }
}