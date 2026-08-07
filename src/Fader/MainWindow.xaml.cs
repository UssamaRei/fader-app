using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT;
using Windows.Graphics;

namespace Fader;

/// <summary>
/// The application's single main window.
/// 
/// Responsibilities:
/// - Apply Mica backdrop (with Acrylic fallback on Windows 10)
/// - Set window size, title bar, and icon
/// - Host the NavigationView for shell navigation
/// - Handle minimize-to-tray behavior (Phase 7)
/// </summary>
public sealed partial class MainWindow : Window
{
    private MicaController? _micaController;
    private SystemBackdropConfiguration? _backdropConfig;

    public CommunityToolkit.Mvvm.Input.IRelayCommand ShowWindowCommand { get; }

    public MainWindow()
    {
        ShowWindowCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(ShowAppWindow);
        InitializeComponent();

        SetupWindow();
        SetupBackdrop();

        // Navigate to dashboard initially
        RootFrame.Navigate(typeof(Pages.DashboardPage));
        
        // Explicitly force the window to show and bring to front
        AppWindow.Show();
        Activate();
    }



    // ─── Window Setup ─────────────────────────────────────────────────────────

    private void SetupWindow()
    {
        // Set a minimum size appropriate for the dashboard layout
        var appWindow = AppWindow;
        appWindow.Title = "Fader";
        appWindow.SetIcon(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets\\AppIcon.ico"));
        appWindow.Resize(new SizeInt32(520, 760));

        // Center on screen
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            var titleBar = appWindow.TitleBar;
            titleBar.ExtendsContentIntoTitleBar = true;
            
            // Title bar button colors will be set to match Mica after backdrop is applied
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            // Dynamically adjust our custom title bar grid to not overlap system buttons
            TitleBarGrid.Padding = new Thickness(0, 0, titleBar.RightInset, 0);
        }
    }

    // ─── Mica Backdrop ────────────────────────────────────────────────────────

    private void SetupBackdrop()
    {
        // Mica is only available on Windows 11 22000+
        if (MicaController.IsSupported())
        {
            _backdropConfig = new SystemBackdropConfiguration
            {
                IsInputActive = true,
                Theme = SystemBackdropTheme.Dark
            };

            _micaController = new MicaController { Kind = MicaKind.Base };

            // Register for activation changes so backdrop matches window focus state
            Activated += OnWindowActivated;
            Closed += OnWindowClosed;

            _micaController.AddSystemBackdropTarget(
                this.As<ICompositionSupportsSystemBackdrop>());
            _micaController.SetSystemBackdropConfiguration(_backdropConfig);
        }
        else
        {
            // Windows 10 fallback: set a dark background colour
            // AcrylicController will be added in a follow-up pass
            RootGrid.Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["ApplicationPageBackgroundThemeBrush"];
        }
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        if (_backdropConfig is null) return;
        _backdropConfig.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        // Don't actually exit the app when the user closes the window.
        // Instead, hide it to the system tray.
        args.Handled = true;
        AppWindow.Hide();
    }

    private void ShowAppWindow()
    {
        // Ensure the window comes to the foreground
        AppWindow.Show();
        Activate(); // Brings window to front
    }

    private void MenuExit_Click(object sender, RoutedEventArgs e)
    {
        // Dispose backdrop resources cleanly
        if (_micaController is not null)
        {
            _micaController.Dispose();
            _micaController = null;
        }
        
        TrayIcon?.Dispose();

        // Gracefully shut down background services to prevent CLR assert failure
        if (App.Services is IDisposable disposableProvider)
        {
            disposableProvider.Dispose();
        }

        Application.Current.Exit();
    }
}
