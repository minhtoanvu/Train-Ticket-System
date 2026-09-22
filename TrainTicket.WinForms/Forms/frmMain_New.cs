using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using TrainTicket.Business.Interfaces;
using TrainTicket.WinForms.Helpers;

namespace TrainTicket.WinForms.Forms
{
    public partial class frmMain_New : Form
    {
        private readonly IScheduleService _scheduleService;
        private readonly ITicketService _ticketService;
        private readonly IReportService _reportService;
        private readonly ICatalogService _catalogService;
        private readonly INotificationService _notificationService;

        private Form? _activeForm;
        private bool _sidebarCollapsed = false;
        private readonly System.Windows.Forms.Timer _notiTimer = new();
        // Giữ scope hiện tại để dispose khi chuyển form, tránh memory leak
        private IServiceScope? _activeScope;

        public frmMain_New(IScheduleService scheduleService, ITicketService ticketService,
            IReportService reportService, ICatalogService catalogService, INotificationService notificationService)
        {
            InitializeComponent();

            _scheduleService = scheduleService;
            _ticketService = ticketService;
            _reportService = reportService;
            _catalogService = catalogService;
            _notificationService = notificationService;

            this.Load += FrmMain_New_Load;

            // LOGIC NÚT GẦM SIDEBAR
            btnCollapse.Click += BtnCollapse_Click;
            btnTheme.Click += BtnTheme_Click;
            btnLogout.Click += BtnLogout_Click;

            // ĐỊNH TUYẾN: MỞ FORM TƯƠNG ỨNG VÀO GIỮA MÀN HÌNH
            if (btnSearch != null) btnSearch.Click += (_, _) => OpenChildFormFromDI<frmSearch_new>("Tìm chuyến tàu");
            if (btnTickets != null) btnTickets.Click += (_, _) => OpenChildFormFromDI<frmTickets_New>("Quản lý vé");
            if (btnPayments != null)
            {
                // [ĐÃ FIX] Nút thanh toán của Staff: mở danh sách vé Pending để chọn vé cần thanh toán
                // Không mở trực tiếp frmPayments_New(0) vì sẽ crash do không có TicketID hợp lệ
                btnPayments.Click += (_, _) =>
                {
                    var ticketsForm = _activeScope?.ServiceProvider.GetService<frmTickets_New>()
                                  ?? Program.ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<frmTickets_New>();
                    OpenChildForm(ticketsForm, "Quản lý vé");
                    UiNotifier.InfoToast("💬 Chọn vé có trạng thái Pending và bấm 'Xác nhận thanh toán' để xử lý.");
                };
            }
            if (btnReports != null) btnReports.Click += (_, _) => OpenChildFormFromDI<frmReports_New>("Báo cáo");
            if (btnTrains != null) btnTrains.Click += (_, _) => OpenChildFormFromDI<frmTrains_New>("Tàu hỏa");
            if (btnStations != null) btnStations.Click += (_, _) => OpenChildFormFromDI<frmStations_New>("Nhà ga");
            if (btnRoutes != null) btnRoutes.Click += (_, _) => OpenChildFormFromDI<frmRoutes_New>("Tuyến đường");
            if (btnSchedules != null) btnSchedules.Click += (_, _) => OpenChildFormFromDI<frmSchedules_New>("Lịch trình");
            if (btnPayHistory != null) btnPayHistory.Click += (_, _) => OpenChildFormFromDI<frmPaymentHistory_New>("Lịch sử thanh toán");
            if (btnMyTickets != null) btnMyTickets.Click += (_, _) => OpenChildFormFromDI<frmCustomerDashboard_New>("Vé của tôi");
            if (btnProfile != null) btnProfile.Click += (_, _) => OpenChildFormFromDI<frmCustomerProfile_New>("Hồ sơ cá nhân");
            if (btnChat != null) btnChat.Click += (_, _) => OpenChildFormFromDI<frmChat_New>("Hỗ trợ trò chuyện");

            // Setup chuông gọi ảo (Chống trơ giao diện)
            _notiTimer.Interval = 60000;
            _notiTimer.Tick += async (_, _) => await RefreshNotiBadgeAsync();
        }

        private async void FrmMain_New_Load(object? sender, EventArgs e)
        {
            var user = SessionManager.CurrentUser;
            var region = SessionManager.CurrentRegion;

            // XÚC DỮ LIỆU LÊN HEADER
            lblWelcome.Text = $"👋 Xin chào, {(user != null ? user.FullName : "Khách")}!";
            lblAvatarLetter.Text = user != null && user.FullName.Length > 0 ? user.FullName.Substring(0, 1).ToUpper() : "?";
            lblRegionBadge.Text = $" {region} ";

            // Tô màu cờ Khu Vực (Vd: North Xanh lục, Central Cam...)
            lblRegionBadge.BackColor = region switch
            {
                "North" => Color.FromArgb(16, 185, 129),
                "Central" => Color.FromArgb(245, 158, 11),
                "South" => Color.FromArgb(59, 130, 246),
                _ => Color.FromArgb(156, 163, 175)
            };

            // LOGIC PHÂN QUYỀN (Che nút)
            bool isAdmin = user?.IsAdmin ?? false;
            bool isStaff = user?.IsStaff ?? false;
            bool isGuest = user?.IsCustomer ?? true;

            if(btnMyTickets != null) btnMyTickets.Visible = isGuest;
            if(btnProfile != null) btnProfile.Visible = isGuest;
            if(btnChat != null) btnChat.Visible = true; // Trả về hiển thị cho cả staff
            if(btnTickets != null) btnTickets.Visible = !isGuest;
            if(btnPayments != null) btnPayments.Visible = !isGuest;
            if(btnReports != null) btnReports.Visible = isStaff || isAdmin;
            if(btnPayHistory != null) btnPayHistory.Visible = isStaff || isAdmin;
            if(btnStations != null) btnStations.Visible = isAdmin;
            if(btnRoutes != null) btnRoutes.Visible = isAdmin;
            if(btnSchedules != null) btnSchedules.Visible = isAdmin;
            if(btnTrains != null) btnTrains.Visible = isAdmin;

            // Start up trơn tru!
            _notiTimer.Start();
            await RefreshNotiBadgeAsync();
            OpenChildFormFromDI<frmSearch_new>("Tìm chuyến tàu");
        }

        // [ĐÃ FIX] Giữ scope vào field, dispose scope cũ trước khi tạo scope mới -> tránh memory leak
        private void OpenChildFormFromDI<T>(string title) where T : Form
        {
            _activeScope?.Dispose();
            _activeScope = Program.ServiceProvider.CreateScope();
            var childForm = _activeScope.ServiceProvider.GetRequiredService<T>();
            OpenChildForm(childForm, title);
        }

        private void OpenChildForm(Form childForm, string title)
        {
            if (_activeForm != null)
            {
                _activeForm.Close();
                _activeForm = null;
            }

            lblBreadcrumb.Text = $"🏠 > {title}";
            _activeForm = childForm;
            childForm.TopLevel = false;
            childForm.FormBorderStyle = FormBorderStyle.None;
            childForm.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(childForm);
            pnlContent.Tag = childForm;
            childForm.BringToFront();
            childForm.Show();

            HighlightActiveButton(title);
        }

        private void HighlightActiveButton(string title)
        {
            var btnList = new[]
            {
                btnSearch, btnTickets, btnPayments, btnMyTickets, btnProfile, btnChat,
                btnReports, btnPayHistory, btnStations, btnRoutes, btnSchedules, btnTrains
            };

            foreach (var b in btnList)
            {
                if (b != null) b.FillColor = Color.Transparent;
            }

            var hit = Array.Find(btnList, b => b != null && b.Text.Contains(title.Split(' ')[0], StringComparison.OrdinalIgnoreCase));
            if (hit != null) hit.FillColor = Color.FromArgb(50, 99, 102, 241);
        }

        // HÀM HIỂN THị DANH SÁCH THÔNG BÁO THẬ SỰ TỪ DB
        private async Task ShowNotificationsAsync()
        {
            if (SessionManager.CurrentUser == null) return;
            try
            {
                var uid = SessionManager.CurrentUser.UserId;
                var notisObj = await _notificationService.GetRecentNotificationsAsync(uid);
                var notis = ((IEnumerable<dynamic>)notisObj).ToList();

                if (notis.Count == 0)
                {
                    UiNotifier.InfoToast("🔔 Không có thông báo nào.");
                    lblNotiBadge.Visible = false;
                    return;
                }

                var sb = new System.Text.StringBuilder();
                foreach (var n in notis)
                {
                    var readMark = n.IsRead == true ? "[✔]" : "[●]";
                    var time = n.CreatedAt?.ToString("HH:mm dd/MM") ?? "";
                    sb.AppendLine($"{readMark} [{time}] {n.Title}");
                    if (!string.IsNullOrWhiteSpace(n.Body))
                        sb.AppendLine($"     {n.Body}");
                    sb.AppendLine();
                }

                MessageBox.Show(sb.ToString(), "🔔 Thông báo của bạn",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Đánh dấu tất cả là đã đọc
                await _notificationService.MarkAsReadAsync(uid);

                lblNotiBadge.Visible = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowNotifications Error]: {ex.Message}");
                UiNotifier.ErrorToast("Đặng tải thông báo thất bại. Vui lòng thử lại.");
            }
        }

        // HÀM LÀM MỚI BADGE THÔNG BÁO: đếm số thông báo chưa đọc thật sự từ DB
        private async Task RefreshNotiBadgeAsync()
        {
            if (SessionManager.CurrentUser == null)
            {
                lblNotiBadge.Visible = false;
                return;
            }

            try
            {
                var uid = SessionManager.CurrentUser.UserId;
                var count = await _notificationService.GetUnreadCountAsync(uid);

                lblNotiBadge.Text = count > 99 ? "99+" : count.ToString();
                lblNotiBadge.Visible = count > 0;
            }
            catch
            {
                lblNotiBadge.Visible = false;
            }
        }

        private void BtnTheme_Click(object? sender, EventArgs e)
        {
            UiTheme.Toggle();
            btnTheme.Text = UiTheme.IsDark ? "  ☀  Chế độ sáng" : "  🌙  Chế độ tối";
        }

        private void BtnCollapse_Click(object? sender, EventArgs e)
        {
            _sidebarCollapsed = !_sidebarCollapsed;
            pnlSidebar.Width = _sidebarCollapsed ? 64 : 220;
            pnlBrand.Visible = !_sidebarCollapsed; // Ẩn luôn cụm Logo
            btnCollapse.Text = _sidebarCollapsed ? "  ▶" : "  ◀  Thu gọn";

            // Xóa chữ khi kéo hẹp, giữ lại đúng Emoji
            foreach (Control ctrl in pnlSidebar.Controls)
            {
                if (ctrl is Guna.UI2.WinForms.Guna2Button btn && btn.Name != "btnCollapse")
                {
                    if (_sidebarCollapsed && btn.Text.Length > 3) btn.Tag = btn.Text;
                    btn.Text = _sidebarCollapsed ? btn.Text.Substring(0, 3) : btn.Tag?.ToString();
                }
            }
        }

        private void BtnLogout_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show("Bạn muốn đăng xuất ư?", "Đăng xuất", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _notiTimer.Stop();
                _activeScope?.Dispose();
                _activeScope = null;
                SessionManager.Clear();

                this.Hide();
                // Scope riêng cho loginForm, sống lâu dài hơn -> không dùng using
                var loginScope = Program.ServiceProvider.CreateScope();
                var loginForm = loginScope.ServiceProvider.GetRequiredService<frmLogin_new>();
                loginForm.FormClosed += (s, args) =>
                {
                    loginScope.Dispose();
                    this.Close();
                };
                loginForm.Show();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _notiTimer.Stop();
            _activeScope?.Dispose();
            base.OnFormClosed(e);
        }
    }
}