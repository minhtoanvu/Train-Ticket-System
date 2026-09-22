using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using Guna.UI2.WinForms;
using TrainTicket.Business.Interfaces;
using TrainTicket.Data.Entities;
using TrainTicket.WinForms.Helpers;

namespace TrainTicket.WinForms.Forms
{
    public partial class frmChat_New : Form, IThemeableForm
    {
        private readonly IChatService _chatService;
        private int _chatPartnerId;
        private string _chatPartnerName = "";
        private List<(int Id, string Name)> _partners = new();

        // SemaphoreSlim(1,1) = mutex – chỉ 1 luồng vào cùng 1 lúc, không block UI
        private readonly SemaphoreSlim _loadLock = new(1, 1);
        // Đếm số bong bóng đã vẽ, chỉ vẽ thêm tin mới
        private int _renderedCount = 0;

        public frmChat_New(IChatService chatService)
        {
            InitializeComponent();
            _chatService = chatService;

            _lstUsers.DrawItem += LstUsers_DrawItem;
            _lstUsers.DrawMode = DrawMode.OwnerDrawFixed;
            _lstUsers.ItemHeight = 38;

            _pollTimer.Interval = 5000;
            _txtMessage.KeyDown += _txtMessage_KeyDown;

            lblContacts.Text = "👤  Liên hệ";
            _lblChatWith.Text = "Chọn người cần liên hệ từ danh sách bên trái";
            _lstUsers.Click += _lstUsers_Click;
        }

        private async void frmChat_New_Load(object sender, EventArgs e)
        {
            await LoadPartnersAsync();
            _pollTimer.Start();
            ApplyTheme();
        }

        private void LstUsers_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _partners.Count) return;
            e.DrawBackground();

            var isSelected = (e.State & DrawItemState.Selected) != 0;
            var bg = isSelected ? UiTheme.Primary : UiTheme.Surface;

            using var brush = new SolidBrush(bg);
            e.Graphics.FillRectangle(brush, e.Bounds);

            using var fg = new SolidBrush(isSelected ? Color.White : UiTheme.TextPrimary);
            e.Graphics.DrawString($"  \U0001f4ac  {_partners[e.Index].Name}",
                new Font("Segoe UI", 10), fg,
                e.Bounds.X, e.Bounds.Y + (e.Bounds.Height - 16) / 2);

            e.DrawFocusRectangle();
        }

        private async Task LoadPartnersAsync()
        {
            var me = SessionManager.CurrentUser!;
            _partners.Clear();

            var staffFlag = me.IsCustomer && !me.IsStaff;
            var partners = await _chatService.GetPartnersAsync(me.UserId, me.IsCustomer, me.IsStaff);
            
            _partners = partners.Select(p => (p.Id, p.Name)).ToList();
            _lblChatWith.Text = staffFlag ? "Chon nhan vien ho tro" : "Chon khach hang de ho tro";

            _lstUsers.Items.Clear();
            foreach (var p in _partners) _lstUsers.Items.Add(p.Name);

            if (_partners.Count > 0)
            {
                _lstUsers.SelectedIndex = 0;
                await SelectPartnerAsync(force: true);
            }
        }

        private async void _lstUsers_Click(object? sender, EventArgs e)
        {
            await SelectPartnerAsync(force: false);
        }

        private async Task SelectPartnerAsync(bool force = false)
        {
            if (_lstUsers.SelectedIndex < 0) return;

            var p = _partners[_lstUsers.SelectedIndex];

            // Neu chon lai dung nguoi dang chat thi khong lam gi
            if (!force && _chatPartnerId == p.Id) return;

            _chatPartnerId   = p.Id;
            _chatPartnerName = p.Name;
            _lblChatWith.Text = $"\U0001f4ac  Chat voi: {_chatPartnerName}";

            // Reset dem va xoa vung chat khi chuyen nguoi
            _renderedCount = 0;
            _chatArea.Controls.Clear();

            await LoadMessagesAsync();

            // Danh dau tin nhan da doc
            try
            {
                var me = SessionManager.CurrentUser!.UserId;
                var unread = await _chatService.GetUnreadMessagesAsync(me, _chatPartnerId);

                if (unread.Any())
                {
                    await _chatService.MarkMessagesAsReadAsync(unread.Select(m => m.MessageId).ToList());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Chat MarkRead Error]: {ex.Message}");
            }
        }

        private async Task LoadMessagesAsync()
        {
            // WaitAsync(0) = thu vao lock khong block – neu dang busy thi bo qua
            if (!await _loadLock.WaitAsync(0)) return;

            try
            {
                var me = SessionManager.CurrentUser!.UserId;
                var currentPartnerId = _chatPartnerId; // Snapshot de tranh thay doi mid-await

                var msgs = await _chatService.GetMessagesAsync(me, currentPartnerId);

                // Neu partner da thay doi trong luc await thi bo qua ket qua nay
                if (currentPartnerId != _chatPartnerId) return;

                // Chi ve them tin moi
                if (msgs.Count <= _renderedCount) return;

                _chatArea.SuspendLayout();

                for (int i = _renderedCount; i < msgs.Count; i++)
                {
                    var msg = msgs[i];
                    bool isMine = msg.SenderId == me;
                    AddBubble(msg.Content, isMine, msg.SentAt);
                }

                _renderedCount = msgs.Count;

                _chatArea.ResumeLayout();
                _chatArea.PerformLayout();

                if (_chatArea.Controls.Count > 0)
                    _chatArea.ScrollControlIntoView(_chatArea.Controls[_chatArea.Controls.Count - 1]);
            }
            finally
            {
                _loadLock.Release();
            }
        }

        private void AddBubble(string text, bool isMine, DateTime? time)
        {
            var timeStr = time.HasValue ? time.Value.ToString("HH:mm") : "";

            var lbl = new Label
            {
                Text = $"{text}\n\n{timeStr}",
                Font = new Font("Segoe UI", 10),
                AutoSize = true,
                Padding = new Padding(12, 10, 12, 10),
                BackColor = isMine ? UiTheme.Primary : UiTheme.SurfaceVariant,
                ForeColor = isMine ? Color.White : UiTheme.TextPrimary,
                Cursor = Cursors.Default
            };

            var row = new Panel
            {
                Width = _chatArea.Width - 25,
                BackColor = Color.Transparent,
                Padding = new Padding(8, 4, 8, 4)
            };

            int maxLabelWidth = (int)(row.Width * 0.65);
            lbl.MaximumSize = new Size(maxLabelWidth, 0);
            row.Controls.Add(lbl);
            row.Height = lbl.Height + 12;

            lbl.Location = isMine
                ? new Point(row.Width - lbl.Width - 8, 4)
                : new Point(8, 4);

            row.Resize += (_, _) =>
            {
                row.Width = _chatArea.Width - 25;
                lbl.MaximumSize = new Size((int)(row.Width * 0.65), 0);
                row.Height = lbl.Height + 12;
                lbl.Left = isMine ? row.Width - lbl.Width - 8 : 8;
            };

            _chatArea.Controls.Add(row);
        }

        private async void _btnSend_Click(object? sender, EventArgs e)
        {
            if (_chatPartnerId == 0)
            {
                UiNotifier.ErrorToast("Vui long chon nguoi can lien he truoc!");
                return;
            }

            var text = _txtMessage.Text.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;

            // Tat nut gui ngay de nguoi dung khong spam click
            _btnSend.Enabled = false;
            _txtMessage.Clear();

            try
            {
                var me = SessionManager.CurrentUser!.UserId;
                await _chatService.SendMessageAsync(me, _chatPartnerId, text);
                await LoadMessagesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Chat Send Error]: {ex.Message}");
                UiNotifier.ErrorToast("Gui tin nhan that bai. Vui long thu lai.");
                _txtMessage.Text = text;
            }
            finally
            {
                _btnSend.Enabled = true;
            }
        }

        private void _txtMessage_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                _btnSend_Click(sender, EventArgs.Empty);
            }
        }

        private async void _pollTimer_Tick(object sender, EventArgs e)
        {
            if (_chatPartnerId > 0)
                await LoadMessagesAsync();
        }

        private void frmChat_New_FormClosed(object sender, FormClosedEventArgs e)
        {
            _pollTimer.Stop();
            _loadLock.Dispose();
        }

        public void ApplyTheme()
        {
            BackColor = UiTheme.Background;

            _leftPanel.FillColor  = UiTheme.Surface;
            lblContacts.BackColor = UiTheme.Surface;
            lblContacts.ForeColor = UiTheme.TextPrimary;

            _lstUsers.BackColor = UiTheme.Surface;
            _lstUsers.ForeColor = UiTheme.TextPrimary;

            _rightPanel.FillColor = UiTheme.Background;
            _lblChatWith.BackColor = Color.Transparent;
            _lblChatWith.ForeColor = UiTheme.TextPrimary;

            _chatArea.BackColor = UiTheme.Background;

            _bottomBar.BackColor = UiTheme.Surface;
            _txtMessage.FillColor = UiTheme.SurfaceVariant;
            _txtMessage.ForeColor = UiTheme.TextPrimary;
            _btnSend.FillColor = UiTheme.Primary;
        }
    }
}