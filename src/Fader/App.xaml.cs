using Fader.Core.Audio;
using Fader.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Serilog;

namespace Fader;

/// <summary>
/// Application entry point and DI composition root.
/// 
/// All services are registered here as singletons and resolved through
/// the service provider. ViewModels are also registered so they can
/// receive services via constructor injection.
/// </summary>
public partial class App : Application
{
    // ─── Service Provider (composition root) ─────────────────────────────────

    /// <summary>Global service provider. Accessible app-wide via App.Services.</summary>
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>The main application window. Kept alive for the lifetime of the process.</summary>
    public static MainWindow MainWindow { get; private set; } = null!;

    // ─── Constructor ──────────────────────────────────────────────────────────

    public App()
    {
        InitializeComponent();
        ConfigureServices();
    }

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        UnhandledException += (s, e) =>
        {
            var logger = App.Services.GetService<Microsoft.Extensions.Logging.ILogger<App>>();
            if (logger != null)
            {
                logger.LogCritical(e.Exception, "Unhandled UI Exception: {Message}", e.Message);
            }
        };

        MainWindow = new MainWindow();
        MainWindow.Activate();
    }

    // ─── DI Configuration ─────────────────────────────────────────────────────

    /// <summary>
    /// Builds the DI container. All registrations must be done here before the
    /// first window is opened. New services added in future phases are added here.
    /// </summary>
    private static void ConfigureServices()
    {
        // ── Logging (Serilog → Microsoft.Extensions.Logging bridge) ──────────
        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                path: Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Fader", "logs", "fader-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        var services = new ServiceCollection();

        // Logging
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(serilogLogger, dispose: true);
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // ── Core Audio Services ────────────────────────────────────────────────

        // ── Core Services ─────────────────────────────────────────────────────────
        services.AddSingleton<Fader.Core.Services.ISettingsService, Fader.Core.Services.SettingsService>();
        services.AddSingleton<IAudioSessionManager, AudioSessionManager>();
        
        // ── Ducking Engine ──────────────────────────────────────────────────────
        services.AddSingleton<FadeEngine>();
        services.AddSingleton<AudioMonitor>();
        services.AddSingleton<DuckingService>();

        // ── ViewModels ────────────────────────────────────────────────────────

        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<SettingsViewModel>();

        // ── Build ──────────────────────────────────────────────────────────────

        Services = services.BuildServiceProvider();

        // ── Force Startup ──────────────────────────────────────────────────────
        // Resolve the DuckingService immediately so it begins listening to 
        // AudioMonitor and AudioSessionManager events in the background.
        _ = Services.GetRequiredService<DuckingService>();
    }
}
