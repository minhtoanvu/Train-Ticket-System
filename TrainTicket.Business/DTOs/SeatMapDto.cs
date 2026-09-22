namespace TrainTicket.Business.DTOs
{
    /// <summary>
    /// Du lieu mot ghe trong so do ghe, tra ve tu sp_XemSoDoGhe.
    /// </summary>
    public class SeatMapDto
    {
        // Trang thai ghe khi ghe chua co nguoi dat
        private const string StatusAvailable = "Trong";

        public string  MaToa     { get; set; } = string.Empty;
        public string  LoaiToa   { get; set; } = string.Empty;
        public string  SoGhe     { get; set; } = string.Empty;
        public string  LoaiGhe   { get; set; } = string.Empty;
        public string  HangGhe   { get; set; } = "Economic";
        public bool    HasSocket  { get; set; }
        public int     SeatID    { get; set; }
        public string  TrangThai { get; set; } = string.Empty;
        public decimal GiaVe     { get; set; }

        public bool IsAvailable => TrangThai == StatusAvailable;
    }
}