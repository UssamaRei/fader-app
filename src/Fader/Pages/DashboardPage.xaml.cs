using Fader.Core.Models;
using Fader.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace Fader.Pages;

/// <summary>
/// Dashboard page — the main screen of Fader.
/// Displays all active audio sessions and quick settings summary.
/// </summary>
public sealed partial class DashboardPage : Page
{
    // ─── ViewModel ────────────────────────────────────────────────────────────

    public DashboardViewModel ViewModel { get; }

    // ─── Constructor ──────────────────────────────────────────────────────────

    public DashboardPage()
    {
        ViewModel = App.Services.GetRequiredService<DashboardViewModel>();
        DataContext = ViewModel;
        InitializeComponent();

        // Trigger initial session scan when page is loaded
        Loaded += OnPageLoaded;
    }

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        // Animate status dot
        StartStatusDotAnimation();

        // Run initial refresh
        await ViewModel.RefreshCommand.ExecuteAsync(null);
    }

    // ─── Animations ───────────────────────────────────────────────────────────

    /// <summary>
    /// Gently pulses the green status dot to indicate the app is live.
    /// Uses a storyboard so it's fully GPU-composited.
    /// </summary>
    private void StartStatusDotAnimation()
    {
        var scaleXAnim = new DoubleAnimation
        {
            From = 1.0,
            To = 1.3,
            Duration = TimeSpan.FromSeconds(1.4),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };

        var scaleYAnim = new DoubleAnimation
        {
            From = 1.0,
            To = 1.3,
            Duration = TimeSpan.FromSeconds(1.4),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };

        var storyboard = new Storyboard();
        storyboard.Children.Add(scaleXAnim);
        storyboard.Children.Add(scaleYAnim);

        Storyboard.SetTarget(scaleXAnim, StatusDot);
        Storyboard.SetTargetProperty(scaleXAnim, "(UIElement.RenderTransform).(ScaleTransform.ScaleX)");
        Storyboard.SetTarget(scaleYAnim, StatusDot);
        Storyboard.SetTargetProperty(scaleYAnim, "(UIElement.RenderTransform).(ScaleTransform.ScaleY)");

        StatusDot.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
        storyboard.Begin();
    }

}
