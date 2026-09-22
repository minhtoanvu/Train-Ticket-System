using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using ClosedXML.Excel;
using Guna.UI2.WinForms;
using TrainTicket.Business.DTOs;
using TrainTicket.Business.Interfaces;
using TrainTicket.WinForms.Helpers;

namespace TrainTicket.WinForms.Forms
{
    public partial class frmReports_New : Form, IThemeableForm
    {
        private readonly IReportService _reportService;
        private LoadingOverlay? _loadingOverlay;

        public frmReports_New(IReportService reportService)
        {
            InitializeComponent();
            _reportService = reportService;

            if (_chart == null)
            {
                _chart = new Chart();
                _chart.Dock = DockStyle.Fill;
                var chartArea = new ChartArea("MainArea");
                _chart.ChartAreas.Add(chartArea);
                splitContainer.Panel2.Controls.Add(_chart);
            }

            _loadingOverlay = new LoadingOverlay(this);
            ApplyTheme();
            
            // Thay vì dùng Load, dùng Shown để đảm bảo toàn bộ UI control đã render xong hoàn toàn
            this.Shown += frmReports_New_Shown;
        }

        private async void frmReports_New_Shown(object? sender, EventArgs e)
        {
            _numYear.Value = DateTime.Now.Year;
            _cboMonth.SelectedIndex = 0; // Tất cả các tháng
            
            // Gọi hàm nạp dữ liệu trực tiếp thay vì giả lập click chuột
            await LoadReportDataAsync();
        }

        public void ApplyTheme()
        {
            BackColor = UiTheme.Background;
            _topPanel.FillColor = UiTheme.Surface;
            _bodyPanel.FillColor = UiTheme.Background;

            _btnLoad.FillColor = UiTheme.Primary;
            _btnLoad.HoverState.FillColor = UiTheme.PrimaryHover;
            _btnExportExcel.FillColor = Color.FromArgb(16, 185, 129);
            _btnExportExcel.HoverState.FillColor = Color.FromArgb(5, 150, 105);

            lblYear.ForeColor = UiTheme.TextSecondary;
            lblYear.BackColor = Color.Transparent;
            lblMonth.ForeColor = UiTheme.TextSecondary;
            lblMonth.BackColor = Color.Transparent;

            UiTheme.StyleGrid(_grid);
        }

        private async void _btnLoad_Click(object sender, EventArgs e)
        {
            await LoadReportDataAsync();
        }

        // Tách biệt logic tải dữ liệu để tái sử dụng an toàn
        private async System.Threading.Tasks.Task LoadReportDataAsync()
        {
            try
            {
                _loadingOverlay?.Show("Đang tải báo cáo...");

                int? month = null;
                if (_cboMonth.SelectedIndex > 0)
                    month = int.Parse(_cboMonth.SelectedItem!.ToString()!);

                var filter = new ReportFilterDto
                {
                    Year = (int)_numYear.Value,
                    Month = month,
                    RouteID = null
                };

                var table = await _reportService.GetRevenueReportAsync(filter);
                _grid.DataSource = table;
                
                // Định dạng lại tên cột cho dễ đọc
                if (_grid.Columns.Contains("Thang")) _grid.Columns["Thang"].HeaderText = "Tháng";
                if (_grid.Columns.Contains("SoVeBan")) _grid.Columns["SoVeBan"].HeaderText = "Số vé bán";
                if (_grid.Columns.Contains("GiaTrungBinh")) 
                {
                    _grid.Columns["GiaTrungBinh"].HeaderText = "Giá TB (VNĐ)";
                    _grid.Columns["GiaTrungBinh"].DefaultCellStyle.Format = "N0";
                }
                if (_grid.Columns.Contains("TongDoanhThu")) 
                {
                    _grid.Columns["TongDoanhThu"].HeaderText = "Doanh thu (VNĐ)";
                    _grid.Columns["TongDoanhThu"].DefaultCellStyle.Format = "N0";
                }
                if (_grid.Columns.Contains("DoanhThu")) 
                {
                    _grid.Columns["DoanhThu"].HeaderText = "Doanh thu (VNĐ)";
                    _grid.Columns["DoanhThu"].DefaultCellStyle.Format = "N0";
                }
                if (_grid.Columns.Contains("RouteName")) _grid.Columns["RouteName"].HeaderText = "Tuyến đường";
                
                // Ẩn cột trùng lặp hoặc không cần thiết
                if (_grid.Columns.Contains("TuyenDuong") && _grid.Columns.Contains("RouteName")) 
                    _grid.Columns["TuyenDuong"].Visible = false;
                if (_grid.Columns.Contains("Nam")) _grid.Columns["Nam"].Visible = false; // Đã chọn năm ở combobox
                
                // Vẽ biểu đồ
                BindChart(table);
                
                UiNotifier.InfoToast($"Đã tải {table.Rows.Count} dòng báo cáo.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[frmReports_New Error]: {ex.Message}");
                UiNotifier.ErrorToast("Thao tác thất bại. Vui lòng thử lại sau.");
            }
            finally
            {
                _loadingOverlay?.Hide();
            }
        }

        private void _btnExportExcel_Click(object sender, EventArgs e)
        {
            if (_grid.Rows.Count == 0)
            {
                UiNotifier.ErrorToast("Không có dữ liệu để xuất Excel.");
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "Excel Workbook|*.xlsx",
                Title = "Lưu báo cáo doanh thu",
                FileName = $"BaoCaoDoanhThu_{_numYear.Value}_{(_cboMonth.SelectedIndex > 0 ? _cboMonth.SelectedItem : "TatCa")}.xlsx"
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                _loadingOverlay?.Show("Đang xuất file Excel...");
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Doanh Thu");

                // Tiêu đề
                ws.Cell(1, 1).Value = "BÁO CÁO DOANH THU BÁN VÉ TÀU";
                ws.Cell(1, 1).Style.Font.Bold = true;
                ws.Cell(1, 1).Style.Font.FontSize = 16;
                ws.Range(1, 1, 1, _grid.Columns.Count)
                  .Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Thông tin filter
                ws.Cell(2, 1).Value = $"Năm: {_numYear.Value}";
                ws.Cell(3, 1).Value = $"Tháng: {(_cboMonth.SelectedIndex > 0 ? _cboMonth.SelectedItem : "Tất cả")}";
                ws.Cell(4, 1).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";

                // Header cột
                int colIndex = 1;
                foreach (DataGridViewColumn col in _grid.Columns)
                {
                    var cell = ws.Cell(6, colIndex);
                    cell.Value = col.HeaderText;
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    colIndex++;
                }

                // Dữ liệu
                int rowIndex = 7;
                foreach (DataGridViewRow row in _grid.Rows)
                {
                    colIndex = 1;
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        var xlCell = ws.Cell(rowIndex, colIndex);
                        xlCell.Value = cell.Value?.ToString();
                        xlCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        colIndex++;
                    }
                    rowIndex++;
                }

                ws.Columns().AdjustToContents();
                wb.SaveAs(sfd.FileName);
                UiNotifier.SuccessToast("Xuất Excel thành công!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[frmReports_New Error]: {ex.Message}");
                UiNotifier.ErrorToast("Thao tác thất bại. Vui lòng thử lại sau.");
            }
            finally
            {
                _loadingOverlay?.Hide();
            }
        }

        private void BindChart(DataTable table)
        {
            // Cải tiến 1: Khống chế Null-check phòng thủ cho cả _chart và Series
            if (_chart == null) return;

            // Nếu trong designer chưa tạo kịp Series "DoanhThu", code sẽ tự tạo tránh crash
            if (_chart.Series.IndexOf("DoanhThu") == -1)
            {
                var newSeries = _chart.Series.Add("DoanhThu");
                newSeries.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Column;
                newSeries.Color = UiTheme.Primary; // Đổi màu cột
                newSeries.IsValueShownAsLabel = true; // Hiện số trên đầu cột
                newSeries.LabelFormat = "N0";
                newSeries.LegendText = "Doanh Thu (VNĐ)";
                
                if (_chart.Titles.Count == 0)
                {
                    _chart.Titles.Add(new System.Windows.Forms.DataVisualization.Charting.Title
                    {
                        Text = "Biểu đồ Doanh Thu theo Tháng",
                        Font = new Font("Segoe UI", 12, FontStyle.Bold),
                        ForeColor = UiTheme.TextPrimary
                    });
                }
                
                var area = _chart.ChartAreas[0];
                area.AxisY.LabelStyle.Format = "N0";
                area.AxisX.MajorGrid.LineColor = UiTheme.Divider;
                area.AxisY.MajorGrid.LineColor = UiTheme.Divider;
                area.AxisX.LabelStyle.ForeColor = UiTheme.TextSecondary;
                area.AxisY.LabelStyle.ForeColor = UiTheme.TextSecondary;
            }

            Series series = _chart.Series["DoanhThu"];
            series.Points.Clear();

            if (table == null || table.Rows.Count == 0) return;

            // Cải tiến 2: Kiểm tra cấu trúc cột 1 lần duy nhất ngoài vòng lặp (Tăng hiệu năng)
            bool hasThang = table.Columns.Contains("Thang");
            
            // Hỗ trợ cả 2 tên cột (DoanhThu hoặc TongDoanhThu) tùy phiên bản Database
            string revCol = table.Columns.Contains("DoanhThu") ? "DoanhThu" : 
                           (table.Columns.Contains("TongDoanhThu") ? "TongDoanhThu" : "");
            
            foreach (DataRow row in table.Rows)
            {
                var month = hasThang ? Convert.ToString(row["Thang"]) : "?";
                var revenue = !string.IsNullOrEmpty(revCol)
                    ? Convert.ToDouble(row[revCol] == DBNull.Value ? 0 : row[revCol])
                    : 0d;

                series.Points.AddXY($"T{month}", revenue);
            }
        }
    }
}
