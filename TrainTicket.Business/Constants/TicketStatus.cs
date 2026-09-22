namespace TrainTicket.Business.Constants
{
    /// <summary>
    /// Các trạng thái hợp lệ của vé tàu trong hệ thống.
    /// Dùng các hằng số này thay cho chuỗi cứng rải rác trong code.
    /// </summary>
    public static class TicketStatus
    {
        /// <summary>Vé đã đặt, chờ thanh toán.</summary>
        public const string Pending = "Pending";

        /// <summary>Thanh toán thành công, vé đã xác nhận.</summary>
        public const string Confirmed = "Confirmed";

        /// <summary>Khách đã lên tàu (check-in).</summary>
        public const string Used = "Used";

        /// <summary>Vé đã bị hủy.</summary>
        public const string Cancelled = "Cancelled";
    }
}
