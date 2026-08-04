using Fader.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Fader.Controls;

/// <summary>
/// AppSessionCard: displays a single <see cref="AudioSession"/> as a styled card.
/// 
/// The Session dependency property drives all UI updates.
/// When a session is assigned, the card loads the app icon asynchronously
/// and binds volume/state fields.
/// </summary>
public sealed partial class AppSessionCard : UserControl
{
    // ─── Dependency Property ──────────────────────────────────────────────────

    public static readonly DependencyProperty SessionProperty =
        DependencyProperty.Register(
            nameof(Session),
            typeof(AudioSession),
            typeof(AppSessionCard),
            new PropertyMetadata(null, OnSessionChanged));

    /// <summary>The audio session displayed by this card.</summary>
    public AudioSession? Session
    {
        get => (AudioSession?)GetValue(SessionProperty);
        set => SetValue(SessionProperty, value);
    }

    // ─── Constructor ──────────────────────────────────────────────────────────

    public AppSessionCard()
    {
        InitializeComponent();
    }

    // ─── Session Property Changed ─────────────────────────────────────────────

    private static void OnSessionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AppSessionCard card && e.NewValue is AudioSession session)
        {
            card.UpdateFromSession(session);
        }
    }

    private void UpdateFromSession(AudioSession session)
    {
        // Display name
        AppNameText.Text = session.DisplayName;

        // Volume
        VolumeBar.Value = session.Volume;
        VolumeText.Text = $"{session.Volume:P0}";

        // Playing state
        UpdatePlayingState(session.IsPlaying);

        // Role selector
        UpdateRoleCombo(session.Role);

        // Load icon asynchronously (don't block)
        _ = LoadIconAsync(session.ExecutablePath);
    }

    private void UpdatePlayingState(bool isPlaying)
    {
        PlayingDot.Visibility = isPlaying ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateRoleCombo(AppRole role)
    {
        RoleCombo.SelectionChanged -= OnRoleChanged;
        RoleCombo.SelectedIndex = role switch
        {
            AppRole.None => 0,
            AppRole.Background => 1,
            AppRole.Trigger => 2,
            _ => 0
        };
        RoleCombo.SelectionChanged += OnRoleChanged;
    }

    // ─── Icon Loading ─────────────────────────────────────────────────────────

    private async Task LoadIconAsync(string executablePath)
    {
        if (string.IsNullOrEmpty(executablePath) || !File.Exists(executablePath))
        {
            SetFallbackIcon();
            return;
        }

        try
        {
            // Extract icon via StorageFile thumbnail (WinRT approach — no GDI needed)
            var storageFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(executablePath);
            var thumbnail = await storageFile.GetThumbnailAsync(
                Windows.Storage.FileProperties.ThumbnailMode.SingleItem, 32);

            var bitmap = new BitmapImage();
            await bitmap.SetSourceAsync(thumbnail);
            AppIcon.Source = bitmap;
        }
        catch
        {
            SetFallbackIcon();
        }
    }

    private void SetFallbackIcon()
    {
        // Show a generic audio icon using a FontIcon instead of an image
        AppIcon.Source = null;
        // The icon container already has a background; the missing image just shows nothing.
        // A future iteration can inject a FontIcon programmatically here.
    }

    // ─── Event Handlers ───────────────────────────────────────────────────────

    private void OnRoleChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Session is null) return;

        var newRole = RoleCombo.SelectedIndex switch
        {
            0 => AppRole.None,
            1 => AppRole.Background,
            2 => AppRole.Trigger,
            _ => AppRole.None
        };

        Session.Role = newRole;
        // TODO Phase 8: persist via SettingsService
    }

    private void OnPointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        // Handled in XAML via VisualState transitions defined in SessionCardStyle
    }

    private void OnPointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        // Handled in XAML via VisualState transitions defined in SessionCardStyle
    }
}
