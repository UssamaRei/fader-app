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
        
        settings.DuckVolume = (float)(DuckVolumePercentageVal / 100.0);
        settings.FadeDurationMs = (int)FadeDurationMsVal;
        settings.RestoreDelayMs = (int)RestoreDelayMsVal;
        settings.MinPlaybackDurationMs = (int)MinPlaybackDurationMsVal;
        settings.DuckingEnabled = DuckingEnabled;
        settings.LaunchOnStartup = LaunchOnStartup;
        settings.StartMinimized = StartMinimized;

        _settingsService.SaveSettings();
    }
}
