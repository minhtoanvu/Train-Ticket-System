namespace TrainTicket.Business.Constants
{
    /// <summary>
    /// Tên các vai trò (role) trong hệ thống.
    /// Dùng các hằng số này thay cho chuỗi cứng rải rác trong code.
    /// </summary>
    public static class RoleNames
    {
        public const string Admin    = "Admin";
        public const string Staff    = "Staff";
        public const string Customer = "Customer";

        /// <summary>
        /// Alias cũ của Customer (dùng trong dữ liệu seed lịch sử).
        /// </summary>
        public const string User = "User";
    }
}
