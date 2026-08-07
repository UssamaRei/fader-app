#pragma warning disable MVVMTK0045
using CommunityToolkit.Mvvm.ComponentModel;
using Fader.Core.Services;

namespace Fader.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private bool _isInitializing = true;

    [ObservableProperty]
    private double _duckVolumePercentageVal;

    [ObservableProperty]
    private double _fadeDurationMsVal;

    [ObservableProperty]
    private double _restoreDelayMsVal;

    [ObservableProperty]
    private double _minPlaybackDurationMsVal;

    [ObservableProperty]
    private bool _duckingEnabled;

    [ObservableProperty]
    private bool _launchOnStartup;

    [ObservableProperty]
    private bool _startMinimized;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        LoadSettings();
    }

    private void LoadSettings()
    {
        _isInitializing = true;
        var settings = _settingsService.GetSettings();

        DuckVolumePercentageVal = (int)(settings.DuckVolume * 100);
        FadeDurationMsVal = settings.FadeDurationMs;
        RestoreDelayMsVal = settings.RestoreDelayMs;
        MinPlaybackDurationMsVal = settings.MinPlaybackDurationMs;
        DuckingEnabled = settings.DuckingEnabled;
        LaunchOnStartup = settings.LaunchOnStartup;
        StartMinimized = settings.StartMinimized;

        _isInitializing = false;
    }

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (_isInitializing) return;

        // Save immediately on any property change
        SaveSettings();
    }

    private void SaveSettings()
    {
        var settings = _settingsService.GetSettings();
        
        settings.DuckVolume = double.IsNaN(DuckVolumePercentageVal) ? 0f : (float)(DuckVolumePercentageVal / 100.0);
        settings.FadeDurationMs = double.IsNaN(FadeDurationMsVal) ? 0 : (int)FadeDurationMsVal;
        settings.RestoreDelayMs = double.IsNaN(RestoreDelayMsVal) ? 0 : (int)RestoreDelayMsVal;
        settings.MinPlaybackDurationMs = double.IsNaN(MinPlaybackDurationMsVal) ? 0 : (int)MinPlaybackDurationMsVal;
        settings.DuckingEnabled = DuckingEnabled;
        settings.LaunchOnStartup = LaunchOnStartup;
        settings.StartMinimized = StartMinimized;

        _settingsService.SaveSettings();
        
        ApplyStartupRegistry(LaunchOnStartup);
    }

    private void ApplyStartupRegistry(bool launchOnStartup)
    {
        try
        {
            const string runKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            const string appName = "Fader";
            
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(runKey, true);
            if (key == null) return;

            if (launchOnStartup)
            {
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(appName, $"\"{exePath}\"");
                }
            }
            else
            {
                key.DeleteValue(appName, false);
            }
        }
        catch (Exception ex)
        {
            // Fallback gracefully if registry access fails
            System.Diagnostics.Debug.WriteLine($"Failed to set startup registry key: {ex.Message}");
        }
    }
}
