using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using Guna.UI2.WinForms;
using TrainTicket.Business.Interfaces;
using TrainTicket.WinForms.Helpers;

namespace TrainTicket.WinForms.Forms
{
    public partial class frmPaymentHistory_New : Form, IThemeableForm
    {
        private readonly IReportService _reportService;

        public frmPaymentHistory_New(IReportService reportService)
        {
            InitializeComponent();
            _reportService = reportService;

            _grid.CellFormatting += Grid_CellFormatting;
            ApplyTheme();
        }

        private async void frmPaymentHistory_New_Load(object sender, EventArgs e)
        {
            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                var keyword = _txtSearch.Text.Trim().ToLower();
                var method  = _cboMethod.SelectedIndex > 0 ? _cboMethod.SelectedItem?.ToString() : null;
                var status  = _cboStatus.SelectedIndex > 0 ? _cboStatus.SelectedItem?.ToString() : null;

                var rowsObj = await _reportService.GetPaymentHistoryAsync(method, status, keyword);
                var rows = ((IEnumerable<dynamic>)rowsObj).ToList();

                _grid.DataSource = rows;

                if (_grid.Columns.Contains("Amount"))
                    _grid.Columns["Amount"].DefaultCellStyle.Format = "N0";
                if (_grid.Columns.Contains("RefundAmount"))
                    _grid.Columns["RefundAmount"].DefaultCellStyle.Format = "N0";
                if (_grid.Columns.Contains("PaidAt"))
                    _grid.Columns["PaidAt"].DefaultCellStyle.Format = "HH:mm dd/MM/yyyy";

                var total = rows.Sum(r => r.Amount);
                _lblTotal.Text = $"  Tổng {rows.Count} giao dịch  |  Tổng thu: {total:N0} ₫";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[frmPaymentHistory_New Error]: {ex.Message}");
                UiNotifier.ErrorToast("Thao tác thất bại. Vui lòng thử lại sau.");
            }
        }

        private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_grid.Columns[e.ColumnIndex].Name != "Status") return;
            if (e.CellStyle != null)
            {
                e.CellStyle.ForeColor = e.Value?.ToString() switch
                {
                    "Completed" => Color.FromArgb(16, 185, 129),
                    "Pending"   => Color.FromArgb(245, 158, 11),
                    "Refunded"  => Color.FromArgb(139, 92, 246),
                    "Failed"    => Color.FromArgb(239, 68, 68),
                    _           => Color.FromArgb(30, 41, 59)
                };
                e.CellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            }
        }

        private async void _txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await LoadAsync();
            }
        }

        private async void _cboMethod_SelectedIndexChanged(object sender, EventArgs e)
        {
            await LoadAsync();
        }

        private async void _cboStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            await LoadAsync();
        }

        private async void _btnRefresh_Click(object sender, EventArgs e)
        {
            await LoadAsync();
        }

        public void ApplyTheme() 
        { 
            BackColor = UiTheme.Background;
            if (_grid != null) UiTheme.StyleGrid(_grid);
        }
    }
}
