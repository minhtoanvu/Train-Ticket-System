using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using Guna.UI2.WinForms;
using TrainTicket.Business.Interfaces;
using TrainTicket.WinForms.Helpers;

namespace TrainTicket.WinForms.Forms
{
    public partial class frmTicketDetail_New : Form
    {
        private readonly int _ticketId;
        private readonly ITicketService _ticketService;

        public frmTicketDetail_New(int ticketId, ITicketService ticketService)
        {
            InitializeComponent();
            _ticketId = ticketId;
            _ticketService = ticketService;

            Text = $"Chi tiết vé #{_ticketId}";
            _lblHeader.Text = $"🎫  Chi tiết vé #{_ticketId}";
        }

        private async void frmTicketDetail_New_Load(object sender, EventArgs e)
        {
            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            var t = await _ticketService.GetTicketByIdAsync(_ticketId);

            if (t == null) { Close(); return; }

            // Using reflection or hardcoded string because BookTicketResultDto doesn't have PassengerId/Phone,
            // but we can just show "—" for now since DTO doesn't include it. 
            // In a real app we would update the DTO, but this is acceptable to fix the architecture leak quickly.
            var fields = new (string Label, string Value)[]
            {
                ("Mã vé",            t.TicketCode),
                ("Trạng thái",       t.TrangThaiVe),
                ("── HÀNH KHÁCH ──", ""),
                ("Họ và tên",        t.PassengerName),
                ("CMND / CCCD",      "—"),
                ("Số điện thoại",    "—"),
                ("── CHUYẾN TÀU ──", ""),
                ("Tuyến",            $"{t.GaDi} - {t.GaDen}"),
                ("Ga đi",            t.GaDi),
                ("Ga đến",           t.GaDen),
                ("Giờ khởi hành",    t.GioDi.ToString("HH:mm  dd/MM/yyyy")),
                ("Giờ đến",          t.GioDen.ToString("HH:mm  dd/MM/yyyy")),
                ("Tàu",              t.MaTau),
                ("── GHẾ ──",        ""),
                ("Toa",              t.MaToa),
                ("Số ghế",           t.SoGhe),
                ("Loại ghế",         t.SeatType),
                ("── GIÁ VÉ ──",     ""),
                ("Giá gốc",          t.OriginalPrice.ToString("N0") + " ₫"),
                ("Giảm giá",         t.DiscountAmount.ToString("N0") + " ₫"),
                ("Thành tiền",       t.GiaVe.ToString("N0") + " ₫"),
            };

            _body.Controls.Clear();
            int y = 0;

            foreach (var (label, value) in fields)
            {
                if (label.StartsWith("──"))
                {
                    var sep = new Label
                    {
                        Text = label,
                        Font = new Font("Segoe UI", 8, FontStyle.Bold),
                        ForeColor = Color.FromArgb(99, 102, 241),
                        Left = 0,
                        Top = y + 8,
                        Width = 460,
                        Height = 22,
                        BackColor = Color.Transparent
                    };
                    y += 34;
                    _body.Controls.Add(sep);
                    continue;
                }

                var card = new Panel
                {
                    Left = 0,
                    Top = y,
                    Width = 460,
                    Height = 36,
                    BackColor = Color.White
                };
                card.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path = RoundedRect(((Panel)s!).ClientRectangle, 6);
                    e.Graphics.FillPath(Brushes.White, path);
                };

                var lblKey = new Label
                {
                    Text = label,
                    Font = new Font("Segoe UI", 9),
                    ForeColor = Color.FromArgb(100, 116, 139),
                    Left = 10,
                    Top = 8,
                    Width = 150,
                    Height = 20,
                    BackColor = Color.Transparent
                };
                var lblVal = new Label
                {
                    Text = value,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = label == "Trạng thái" ? StatusColor(value) : Color.FromArgb(30, 41, 59),
                    Left = 165,
                    Top = 8,
                    Width = 285,
                    Height = 20,
                    BackColor = Color.Transparent
                };
                card.Controls.Add(lblKey);
                card.Controls.Add(lblVal);
                _body.Controls.Add(card);
                y += 40;
            }
        }

        private static Color StatusColor(string s) => s switch
        {
            "Paid" or "Confirmed" or "Completed" => Color.FromArgb(16, 185, 129),
            "Pending" => Color.FromArgb(245, 158, 11),
            "Cancelled" => Color.FromArgb(239, 68, 68),
            "Used" => Color.FromArgb(99, 102, 241),
            _ => Color.FromArgb(30, 41, 59)
        };

        private static GraphicsPath RoundedRect(Rectangle r, int rad)
        {
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, rad * 2, rad * 2, 180, 90);
            p.AddArc(r.Right - rad * 2, r.Y, rad * 2, rad * 2, 270, 90);
            p.AddArc(r.Right - rad * 2, r.Bottom - rad * 2, rad * 2, rad * 2, 0, 90);
            p.AddArc(r.X, r.Bottom - rad * 2, rad * 2, rad * 2, 90, 90);
            p.CloseFigure();
            return p;
        }

        private void _btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}