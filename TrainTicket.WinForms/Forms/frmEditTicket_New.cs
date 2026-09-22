using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using Guna.UI2.WinForms;
using TrainTicket.Business.Interfaces;
using TrainTicket.WinForms.Helpers;

namespace TrainTicket.WinForms.Forms
{
    public partial class frmEditTicket_New : Form
    {
        private readonly int _ticketId;
        private readonly ITicketService _ticketService;

        public bool Saved { get; private set; }

        public frmEditTicket_New(int ticketId, ITicketService ticketService)
        {
            InitializeComponent();
            _ticketId = ticketId;
            _ticketService = ticketService;

            Text = $"Sửa thông tin vé #{_ticketId}";
            _lblHeader.Text = $"📝  Sửa thông tin hành khách — Vé #{_ticketId}";
        }

        private async void frmEditTicket_New_Load(object sender, EventArgs e)
        {
            await LoadTicketAsync();
        }

        private async Task LoadTicketAsync()
        {
            var ticket = await _ticketService.GetTicketEntityAsync(_ticketId);
            if (ticket == null) { Close(); return; }

            _txtName.Text  = ticket.PassengerName ?? "";
            _txtIdNum.Text = ticket.PassengerId   ?? "";
            _txtPhone.Text = ticket.PassengerPhone ?? "";

            var canEdit = ticket.Status is "Pending" or "Paid" or "Confirmed";
            _btnSave.Enabled = canEdit;
            _lblInfo.Text = canEdit
                ? $"Trạng thái vé: {ticket.Status} — Có thể chỉnh sửa"
                : $"❌  Vé trạng thái \"{ticket.Status}\" — Không thể sửa";
            _lblInfo.ForeColor = canEdit
                ? Color.FromArgb(16, 185, 129)
                : Color.FromArgb(239, 68, 68);
        }

        private async void _btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtName.Text))
            {
                MessageBox.Show("Tên hành khách không được để trống!", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var success = await _ticketService.UpdatePassengerInfoAsync(
                _ticketId, _txtName.Text.Trim(), _txtIdNum.Text.Trim(), _txtPhone.Text.Trim());

            if (!success) return;

            Saved = true;
            MessageBox.Show("✅ Đã cập nhật thông tin hành khách thành công!", "Thành công",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }

        private void _btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}