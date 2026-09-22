using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using TrainTicket.WinForms.Helpers; // de dung UiNotifier

namespace TrainTicket.WinForms.Helpers
{
    /// <summary>
    /// Bat loi toan cuc cho ung dung WinForms.
    /// Dam bao ung dung khong bi crash vang ra Desktop khi gap Exception.
    /// Ghi log ra file logs/error.txt.
    /// </summary>
    public static class GlobalExceptionHandler
    {
        private static readonly string LogDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        private static readonly string LogFile = Path.Combine(LogDir, "error.txt");

        public static void Initialize()
        {
            if (!Directory.Exists(LogDir))
                Directory.CreateDirectory(LogDir);

            // Bat loi tu UI Thread
            Application.ThreadException += OnThreadException;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            // Bat loi tu Background Threads
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            LogException(e.Exception);
            ShowError(e.Exception);
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogException(ex);
                ShowError(ex);
            }
        }

        private static void LogException(Exception ex)
        {
            try
            {
                var msg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n";
                File.AppendAllText(LogFile, msg);
            }
            catch { /* Ignore loi ghi log */ }
        }

        private static void ShowError(Exception ex)
        {
            MessageBox.Show(
                $"Da xay ra su co bat ngo.\nChi tiet: {ex.Message}\n\nVui long thu lai hoac lien he quan tri vien.",
                "Loi he thong",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}