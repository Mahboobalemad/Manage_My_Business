using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShopsManagement.Data;
using ShopsManagement.Data.Services;
using ShopsManagement.Domain.Enums;
using ShopsManagement.Domain.Services;
using ShopsManagement.UI.Services;
using ShopsManagement.UI.ViewModels;


namespace ShopsManagement.UI;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        _serviceProvider = serviceCollection.BuildServiceProvider();

        // Ensure SQLite Database is initialized
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();

            // Seed default main branch if no branch exists
            if (!await dbContext.Branches.AnyAsync())
            {
                dbContext.Branches.Add(new Domain.Entities.Branch
                {
                    Name = "الفرع الرئيسي",
                    Notes = "الفرع التلقائي للنظام"
                });
                await dbContext.SaveChangesAsync();
            }

            // Automated Backup on Application Startup — respects frequency from Settings
            try
            {
                var backupService   = scope.ServiceProvider.GetRequiredService<IBackupService>();
                var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();

                if (settingsService.ShouldRunAutoBackup())
                {
                    await backupService.CreateBackupAsync(BackupType.Automatic);
                    settingsService.RecordAutoBackupDone();
                }
            }
            catch
            {
                // Non-blocking fallback for startup backup
            }
        }

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        var viewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
        await viewModel.InitializeAsync();
        mainWindow.DataContext = viewModel;
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        // Automated Backup on Application Exit — respects frequency from Settings
        if (_serviceProvider != null)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var backupService   = scope.ServiceProvider.GetRequiredService<IBackupService>();
                var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();

                if (settingsService.ShouldRunAutoBackup())
                {
                    await backupService.CreateBackupAsync(BackupType.Automatic);
                    settingsService.RecordAutoBackupDone();
                }
            }
            catch
            {
                // Silent exit
            }
        }

        base.OnExit(e);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Database Context
        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ShopsManagement.db");
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Domain Services
        services.AddScoped<IBranchService, BranchService>();
        services.AddScoped<IDailyActivityService, DailyActivityService>();
        services.AddScoped<ITraderService, TraderService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IZakatService, ZakatService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IReportExportService, ReportExportService>();
        services.AddScoped<IBackupService, BackupService>();

        // App Settings (singleton — shared across ViewModels)
        services.AddSingleton<ISettingsService, SettingsService>();

        // ViewModels
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<BranchesViewModel>(sp => new BranchesViewModel(
            sp.GetRequiredService<IBranchService>(),
            sp.GetRequiredService<IDailyActivityService>(),
            sp.GetRequiredService<IInvoiceService>(),
            sp.GetRequiredService<IEmployeeService>(),
            sp.GetRequiredService<IBackupService>()));
        services.AddTransient<DailyActivityViewModel>();
        services.AddTransient<InvoicesViewModel>();
        services.AddTransient<TradersViewModel>(sp => new TradersViewModel(
            sp.GetRequiredService<ITraderService>(),
            sp.GetRequiredService<IBackupService>()));
        services.AddTransient<EmployeesViewModel>();
        services.AddTransient<ZakatViewModel>();
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<BackupViewModel>(sp => new BackupViewModel(
            sp.GetRequiredService<IBackupService>(),
            sp.GetRequiredService<ISettingsService>()));
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<MainWindowViewModel>();

        // Views
        services.AddSingleton<MainWindow>();
    }
}
