using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrainTicket.Business.Interfaces;
using TrainTicket.Business.Services;
using TrainTicket.Data.ADO;
using TrainTicket.Data.DbContexts;
using TrainTicket.WinForms.Forms;
using TrainTicket.WinForms.Helpers;

namespace TrainTicket.WinForms
{
    internal static class Program
    {
        /// <summary>
        /// ServiceProvider dung chung de resolve Dependency Injection cho Form va Service.
        /// </summary>
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Dang ky Global Exception Handler
            GlobalExceptionHandler.Initialize();

            // Doc theme (sang/toi) da luu tu lan chay truoc
            UiTheme.Load();

            // Tao DI container va dang ky tat ca dependency
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();

            // Mo form dang nhap
            using var scope     = ServiceProvider.CreateScope();
            var loginForm = scope.ServiceProvider.GetRequiredService<frmLogin_new>();
            Application.Run(loginForm);
        }

        /// <summary>
        /// Dang ky tat ca dependency vao DI container.
        /// Gom: DbContext, AdoHelper, Business Services va WinForms.
        /// </summary>
        public static void ConfigureServices(IServiceCollection services)
        {
            // TenantProvider cung cap vung hien tai cho Global Query Filter
            services.AddSingleton<ITenantProvider, TenantProvider>();

            // EF Core DbContext - dung lambda de lay connection string hien tai luc runtime
            services.AddDbContext<TrainTicketDbContext>((_, options) =>
                options.UseSqlServer(ConnectionHelper.CurrentConnectionString),
                ServiceLifetime.Scoped);

            // ADO.NET helper - dung cho cac nghiep vu can goi Stored Procedure
            services.AddScoped(_ => new AdoHelper(ConnectionHelper.CurrentConnectionString));

            // Business Services
            services.AddScoped<IAuthService,         AuthService>();
            services.AddScoped<ICustomerService,     CustomerService>();
            services.AddScoped<IChatService,         ChatService>();
            services.AddScoped<IDashboardService,    DashboardService>();
            services.AddScoped<IScheduleService,     ScheduleService>();
            services.AddScoped<ITicketService,       TicketService>();
            services.AddScoped<IReportService,       ReportService>();
            services.AddScoped<ICatalogService,      CatalogService>();
            services.AddScoped<IDiscountService,     DiscountService>();
            services.AddScoped<INotificationService, NotificationService>();

            // WinForms
            services.AddScoped<frmLogin_new>();
            services.AddScoped<frmMain_New>();
            services.AddScoped<frmSearch_new>();
            services.AddScoped<frmSeatMap_New>();
            services.AddScoped<frmBookingConfirm_New>();
            services.AddScoped<frmReports_New>();
            services.AddScoped<frmTickets_New>();
            services.AddScoped<frmPayments_New>();
            services.AddScoped<frmPaymentHistory_New>();
            services.AddScoped<frmCustomerDashboard_New>();
            services.AddScoped<frmCustomerProfile_New>();
            services.AddScoped<frmChat_New>();
            services.AddScoped<frmTrains_New>();
            services.AddScoped<frmStations_New>();
            services.AddScoped<frmRoutes_New>();
            services.AddScoped<frmSchedules_New>();
        }
    }
}