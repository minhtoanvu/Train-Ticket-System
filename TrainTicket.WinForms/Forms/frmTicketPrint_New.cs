using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using TrainTicket.Business.Interfaces;
using TrainTicket.WinForms.Helpers;

namespace TrainTicket.WinForms.Forms
{
    public partial class frmTicketPrint_New : Form
    {
        private readonly int _ticketId;
        private readonly ITicketService _ticketService;

        private string _ticketCode = "";
        private string _passenger = "";
        private string _phone = "";
        private string _idNum = "";
        private string _route = "";
        private string _departure = "";
        private string _arrival = "";
        private string _trainCode = "";
        private string _carriage = "";
        private string _seat = "";
        private string _seatType = "";
        private string _price = "";
        private string _status = "";
        private string _bookedAt = "";

        private readonly PrintDocument _printDoc = new();

        public frmTicketPrint_New(int ticketId, ITicketService ticketService)
        {
            InitializeComponent();
            _ticketId = ticketId;
            _ticketService = ticketService;

            Text = $"In vé #{_ticketId}";
            _lblHeader.Text = $"🖨️  In vé / Xem trước — Vé #{_ticketId}";

            _preview.Document = _printDoc;
            _printDoc.PrintPage += PrintDoc_PrintPage;
        }

        private async void frmTicketPrint_New_Load(object sender, EventArgs e)
        {
            await LoadAndPreviewAsync();
        }

        private async System.Threading.Tasks.Task LoadAndPreviewAsync()
        {
            var t = await _ticketService.GetTicketByIdAsync(_ticketId);

            if (t == null) { Close(); return; }

            _ticketCode = t.TicketCode;
            _passenger = t.PassengerName;
            _phone = "—"; // Not in DTO
            _idNum = "—"; // Not in DTO
            _route = $"{t.GaDi} - {t.GaDen}";
            _departure = t.GioDi.ToString("HH:mm  dd/MM/yyyy");
            _arrival = t.GioDen.ToString("HH:mm  dd/MM/yyyy");
            _trainCode = t.MaTau;
            _carriage = t.MaToa;
            _seat = t.SoGhe;
            _seatType = t.SeatType;
            _price = t.GiaVe.ToString("N0") + " ₫";
            _status = t.TrangThaiVe;
            _bookedAt = "—"; // Not in DTO

            _preview.InvalidatePreview();
        }

        private void PrintDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            var g = e.Graphics!;
            int x = 40;
            int y = 30;
            int w = 500;

            using var bgBrush = new SolidBrush(Color.FromArgb(30, 41, 59));
            g.FillRectangle(bgBrush, x, y, w, 52);
            g.DrawString("🚂 TRAIN TICKET — VÉ TÀU HỎA",
                new Font("Segoe UI", 14, FontStyle.Bold), Brushes.White, x + 12, y + 12);

            y += 60;

            g.DrawString($"Mã vé: {_ticketCode}",
                new Font("Segoe UI", 12, FontStyle.Bold), Brushes.Black, x, y);
            g.DrawString($"Trạng thái: {_status}",
                new Font("Segoe UI", 10), Brushes.Gray, x + 300, y + 2);
            y += 30;

            using var linePen = new Pen(Color.FromArgb(200, 200, 200));
            g.DrawLine(linePen, x, y, x + w, y);
            y += 10;

            void Section(string title)
            {
                y += 6;
                g.DrawString(title, new Font("Segoe UI", 9, FontStyle.Bold),
                    new SolidBrush(Color.FromArgb(99, 102, 241)), x, y);
                y += 20;
            }

            void Row(string label, string value)
            {
                g.DrawString(label + ":", new Font("Segoe UI", 9), Brushes.Gray, x + 10, y);
                g.DrawString(value, new Font("Segoe UI", 9, FontStyle.Bold), Brushes.Black, x + 155, y);
                y += 22;
            }

            Section("THÔNG TIN HÀNH KHÁCH");
            Row("Họ và tên", _passenger);
            Row("CMND / CCCD", _idNum);
            Row("Số điện thoại", _phone);

            g.DrawLine(linePen, x, y, x + w, y); y += 8;

            Section("THÔNG TIN CHUYẾN TÀU");
            Row("Tuyến đường", _route);
            Row("Giờ khởi hành", _departure);
            Row("Giờ đến dự kiến", _arrival);
            Row("Số hiệu tàu", _trainCode);

            g.DrawLine(linePen, x, y, x + w, y); y += 8;

            Section("THÔNG TIN GHẾ");
            Row("Toa", _carriage);
            Row("Số ghế", _seat);
            Row("Loại ghế", _seatType);

            g.DrawLine(linePen, x, y, x + w, y); y += 8;

            Section("GIÁ VÉ");
            Row("Thành tiền", _price);
            Row("Ngày đặt vé", _bookedAt);

            y += 10;
            g.DrawLine(linePen, x, y, x + w, y); y += 12;

            g.DrawString("Cảm ơn quý khách đã sử dụng dịch vụ TrainTicket!",
                new Font("Segoe UI", 9, FontStyle.Italic),
                Brushes.Gray, x, y);

            e.HasMorePages = false;
        }

        private void _btnPrint_Click(object sender, EventArgs e)
        {
            using var dlg = new PrintDialog { Document = _printDoc };
            if (dlg.ShowDialog() == DialogResult.OK) _printDoc.Print();
        }

        private void _btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}