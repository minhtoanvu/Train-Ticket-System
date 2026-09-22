using System;
using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using TrainTicket.Business.Interfaces;
using TrainTicket.WinForms.Helpers;

namespace TrainTicket.WinForms.Forms
{
    public partial class frmPayments_New : Form, IThemeableForm
    {
        private readonly int _ticketId;
        private readonly ITicketService _ticketService;
        private LoadingOverlay? _loadingOverlay;
        private bool _isInitialized = false; // Cờ ngăn chặn các sự kiện kích hoạt sớm trước khi Form Load xong

        public frmPayments_New(int ticketId, ITicketService ticketService)
        {
            InitializeComponent();
            _ticketId = ticketId;
            _ticketService = ticketService;

            // Thiết lập trạng thái hiển thị ban đầu cho Form
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;

            Text = $"Xác nhận thanh toán - Vé #{_ticketId}";
            _lblInfo.Text = $"Xác nhận thanh toán cho vé số {_ticketId}.";

            // Ép ảnh QR luôn co giãn tỉ lệ chuẩn, không bị vỡ hay mất góc cụm QR
            if (_picQrCode != null)
            {
                _picQrCode.SizeMode = PictureBoxSizeMode.Zoom;
            }

            // Đăng ký thủ công sự kiện Load của Form
            this.Load += frmPayments_New_Load;

            // Đăng ký sự kiện ComboBox an toàn
            _cboPaymentMethod.SelectedIndexChanged -= _cboPaymentMethod_SelectedIndexChanged;
            _cboPaymentMethod.SelectedIndexChanged += _cboPaymentMethod_SelectedIndexChanged;

            ApplyTheme();
        }

        private async void frmPayments_New_Load(object? sender, EventArgs e)
        {
            // Triệt tiêu thuộc tính tự sụp Form của các nút bấm nếu lỡ bị cấu hình nhầm trong Designer
            _btnConfirm.DialogResult = DialogResult.None;
            if (btnClose != null) btnClose.DialogResult = DialogResult.None;

            // Khởi tạo lớp phủ Loading sau khi Handle đồ họa của Form đã sẵn sàng
            _loadingOverlay = new LoadingOverlay(this);

            // Gán dữ liệu mặc định an toàn cho ComboBox
            if (_cboPaymentMethod.Items.Count > 0 && _cboPaymentMethod.SelectedIndex < 0)
            {
                _cboPaymentMethod.SelectedIndex = 0;
            }

            // Bật cờ cho phép nạp QR
            _isInitialized = true;

            // Bắt đầu gọi nạp mã QR động từ mạng về
            await UpdateQrCodeAsync();
        }

        // =============================================
        // CẤU HÌNH NGÂN HÀNG — Chỉnh ở đây khi cần đổi TK
        // =============================================
        private const string BANK_BIN      = "970415";          // VietinBank
        private const string BANK_ACCOUNT  = "123456789";        // Số tài khoản nhà ga
        private const string ACCOUNT_NAME  = "CONG TY DUONG SAT TRAINTICKET";

        /// <summary>
        /// Tải ảnh QR VietQR qua HttpClient (await được, có fallback lỗi rõ ràng)
        /// </summary>
        private async System.Threading.Tasks.Task UpdateQrCodeAsync()
        {
            if (!_isInitialized) return;

            string paymentMethod = _cboPaymentMethod.SelectedItem?.ToString() ?? "";

            // Thanh toán tiền mặt: ẩn QR, hiện hướng dẫn
            bool isCash = paymentMethod.Contains("Tiền mặt") || paymentMethod == "Cash";
            if (isCash)
            {
                _picQrCode.Image = null;
                _lblInfo.Text = $"Vé #{_ticketId}: Vui lòng thanh toán tiền mặt tại quầy ga trước giờ tàu chạy.";
                return;
            }

            try
            {
                _loadingOverlay?.Show("Đang tạo mã QR thanh toán...");

                // Lấy thông tin vé từ DB
                var ticket = await _ticketService.GetTicketByIdAsync(_ticketId);
                if (ticket == null)
                {
                    UiNotifier.ErrorToast("Không tìm thấy thông tin vé.");
                    _picQrCode.Image = null;
                    return;
                }

                decimal soTien = ticket.GiaVe;
                string maVe    = ticket.TicketCode;

                if (soTien <= 0)
                {
                    UiNotifier.ErrorToast("Số tiền thanh toán không hợp lệ.");
                    return;
                }

                _lblInfo.Text = $"Vé #{_ticketId} ({maVe})  |  Số tiền: {soTien:N0} VNĐ";

                string tenTK  = Uri.EscapeDataString(ACCOUNT_NAME);
                string noiDung = Uri.EscapeDataString($"THANHTOAN VE {maVe}");
                string qrUrl  = $"https://api.vietqr.io/image/{BANK_BIN}-{BANK_ACCOUNT}-compact.jpg" +
                                $"?amount={soTien:F0}&addInfo={noiDung}&accountName={tenTK}";

                // Dùng HttpClient + await để tải ảnh, có timeout 10 giây
                using var httpClient = new System.Net.Http.HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);

                var imageBytes = await httpClient.GetByteArrayAsync(qrUrl);

                using var ms = new System.IO.MemoryStream(imageBytes);
                var bmp = new System.Drawing.Bitmap(ms);

                // Phải dùng clone vì MemoryStream bị dispose sau using
                _picQrCode.Image?.Dispose();
                _picQrCode.Image = (System.Drawing.Bitmap)bmp.Clone();
            }
            catch (System.Net.Http.HttpRequestException)
            {
                _picQrCode.Image = null;
                _lblInfo.Text = "Không thể tải mã QR (kiểm tra kết nối mạng). Bạn có thể nhập mã GD thủ công.";
                UiNotifier.ErrorToast("Không tải được mã QR — kiểm tra kết nối mạng.");
            }
            catch (TaskCanceledException)
            {
                _picQrCode.Image = null;
                _lblInfo.Text = "Tải mã QR quá thời gian (timeout 10s). Vui lòng thử lại.";
                UiNotifier.ErrorToast("Tải mã QR quá thời gian. Thử lại nhé!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[QR Error]: {ex.Message}");
                _picQrCode.Image = null;
                _lblInfo.Text = "Không thể tải mã QR. Vui lòng liên hệ nhân viên hỗ trợ.";
            }
            finally
            {
                _loadingOverlay?.Hide();
            }
        }

        private async void _cboPaymentMethod_SelectedIndexChanged(object? sender, EventArgs e)
        {
            await UpdateQrCodeAsync();
        }

        private async void _btnConfirm_Click(object sender, EventArgs e)
        {
            _btnConfirm.Enabled = false;

            try
            {
                _loadingOverlay?.Show("Đang xác nhận thanh toán...");

                var paymentMethod = _cboPaymentMethod.SelectedItem?.ToString() ?? "";
                var isCash = paymentMethod.Contains("Tiền mặt") || paymentMethod == "Cash";

                // Validate mã giao dịch bắt buộc khi thanh toán online
                var transactionId = _txtTransactionId.Text.Trim();
                if (!isCash && string.IsNullOrWhiteSpace(transactionId))
                {
                    UiNotifier.Info("Vui lòng nhập mã giao dịch từ ngân hàng/ví điện tử trước khi xác nhận.");
                    _btnConfirm.Enabled = true;
                    _loadingOverlay?.Hide();
                    return;
                }

                // Validate độ dài mã giao dịch hợp lý
                if (!isCash && (transactionId.Length < 4 || transactionId.Length > 64))
                {
                    UiNotifier.Info("Mã giao dịch không hợp lệ (4-64 ký tự).");
                    _btnConfirm.Enabled = true;
                    _loadingOverlay?.Hide();
                    return;
                }

                // Hỏi xác nhận một lần cuối
                var confirm = MessageBox.Show(
                    $"Xác nhận thanh toán cho vé #{_ticketId}?\n" +
                    $"Phương thức: {paymentMethod}\n" +
                    (isCash ? "" : $"Mã GD: {transactionId}"),
                    "Xác nhận thanh toán",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm != DialogResult.Yes)
                {
                    _btnConfirm.Enabled = true;
                    _loadingOverlay?.Hide();
                    return;
                }

                var finalTransactionId = isCash
                    ? $"CASH_PAY_{_ticketId}_{DateTime.Now:yyyyMMddHHmmss}"
                    : transactionId;

                var success = await _ticketService.ConfirmPaymentAsync(_ticketId, finalTransactionId);

                if (success)
                {
                    UiNotifier.SuccessToast("🎉 Thanh toán thành công! Vé đã được kích hoạt.");
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    UiNotifier.ErrorToast("Xác nhận thất bại. Vé không tồn tại, đã được xử lý, hoặc đã hết hạn.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Payment Confirm Error]: {ex.Message}");
                UiNotifier.ErrorToast("Hệ thống gặp sự cố khi xử lý thanh toán. Vui lòng thử lại sau.");
            }
            finally
            {
                _loadingOverlay?.Hide();
                _btnConfirm.Enabled = true;
            }
        }

        public void ApplyTheme()
        {
            BackColor = UiTheme.Background;
            _lblInfo.ForeColor = UiTheme.TextPrimary;
            _lblInfo.BackColor = Color.Transparent;
            _btnConfirm.FillColor = UiTheme.Primary;
            _btnConfirm.HoverState.FillColor = UiTheme.PrimaryHover;
            _card.FillColor = UiTheme.Surface;

            if (lblPaymentMethod != null) lblPaymentMethod.ForeColor = UiTheme.TextSecondary;
            if (lblTransactionId != null) lblTransactionId.ForeColor = UiTheme.TextSecondary;
            if (lblQrCode != null) lblQrCode.ForeColor = UiTheme.TextSecondary;
            if (btnClose != null) btnClose.ForeColor = UiTheme.TextPrimary;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}