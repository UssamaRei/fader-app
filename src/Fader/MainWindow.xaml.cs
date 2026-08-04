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

    public MainWindow()
    {
        InitializeComponent();

        SetupWindow();
        SetupBackdrop();
    }

    // ─── Window Setup ─────────────────────────────────────────────────────────

    private void SetupWindow()
    {
        // Set a minimum size appropriate for the dashboard layout
        var appWindow = AppWindow;
        appWindow.Title = "Fader";
        appWindow.Resize(new SizeInt32(520, 760));

        // Center on screen
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            var titleBar = appWindow.TitleBar;
            titleBar.ExtendsContentIntoTitleBar = true;
            // Title bar button colors will be set to match Mica after backdrop is applied
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
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
        // Dispose backdrop resources cleanly
        if (_micaController is not null)
        {
            _micaController.Dispose();
            _micaController = null;
        }
        Activated -= OnWindowActivated;
    }
}
