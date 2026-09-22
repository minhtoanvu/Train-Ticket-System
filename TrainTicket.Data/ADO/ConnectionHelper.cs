using System.Text.Json;

namespace TrainTicket.Data.ADO
{
    /// <summary>
    /// Quan ly connection string SQL Server.
    /// Ho tro nhieu moi truong (HQ, North, Central, South) va doc tu appsettings.json.
    /// </summary>
    public static class ConnectionHelper
    {
        private static string _current = DefaultConnection;

        // Connection string mac dinh (localhost, database TrainTicketDB)
        public static string DefaultConnection => BuildConnectionString("localhost", "TrainTicketDB");
        public static string NorthConnection   => BuildConnectionString("localhost", "TrainTicketDB_North");
        public static string CentralConnection => BuildConnectionString("localhost", "TrainTicketDB_Central");
        public static string SouthConnection   => BuildConnectionString("localhost", "TrainTicketDB_South");

        /// <summary>Connection string hien tai, co the thay doi luc runtime (khi chon vung).</summary>
        public static string CurrentConnectionString
        {
            get => _current;
            set => _current = value;
        }

        /// <summary>Tao connection string chuan voi Windows Auth hoac SQL Auth.</summary>
        public static string BuildConnectionString(
            string server, string database,
            string? user = null, string? password = null)
        {
            if (user != null && password != null)
                return $"Server={server};Database={database};User Id={user};Password={password};" +
                       "TrustServerCertificate=True;";

            return $"Server={server};Database={database};" +
                   "Trusted_Connection=True;TrustServerCertificate=True;";
        }

        /// <summary>Tai connection string tu appsettings.json neu file ton tai.</summary>
        public static void LoadFromConfig(string configPath = "appsettings.json")
        {
            try
            {
                if (!File.Exists(configPath)) return;

                var json = File.ReadAllText(configPath);
                var doc  = JsonDocument.Parse(json);
                var conn = doc.RootElement
                    .GetProperty("ConnectionStrings")
                    .GetProperty("DefaultConnection")
                    .GetString();

                if (!string.IsNullOrEmpty(conn))
                    _current = conn;
            }
            catch
            {
                // Giu nguyen default neu file loi hoac thieu key
            }
        }

        /// <summary>Kiem tra xem connection string hien tai co ket noi duoc khong.</summary>
        public static bool IsValid()
        {
            try { return new AdoHelper(_current).TestConnection(); }
            catch { return false; }
        }
    }
}