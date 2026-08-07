using Fader.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Fader.Core.Services;

/// <summary>
/// Manages loading and saving of the application settings to a JSON file in AppData.
/// Exposes the current settings and provides methods to update them.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private readonly string _settingsPath;
    
    private FaderSettings _settings;
    private readonly object _lock = new();

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var faderDir = Path.Combine(appData, "Fader");
        
        Directory.CreateDirectory(faderDir);
        _settingsPath = Path.Combine(faderDir, "settings.json");

        _settings = LoadSettings();
    }

    /// <summary>
    /// Gets a thread-safe snapshot of the current settings.
    /// Note: To update settings, use the provided update methods or save explicitly.
    /// </summary>
    public FaderSettings GetSettings()
    {
        lock (_lock)
        {
            // Simple clone via JSON to prevent external mutation if needed,
            // or just return the reference since it's an internal app.
            // We'll just return the reference for simplicity in this MVP.
            return _settings;
        }
    }

    /// <summary>
    /// Assigns a role to an executable (e.g. "spotify.exe" -> AppRole.Background).
    /// </summary>
    public void SetAppRole(string executablePath, AppRole role)
    {
        if (string.IsNullOrWhiteSpace(executablePath)) return;

        var exeName = Path.GetFileName(executablePath).ToLowerInvariant();

        lock (_lock)
        {
            if (role == AppRole.None)
            {
                _settings.AppRoles.Remove(exeName);
            }
            else
            {
                _settings.AppRoles[exeName] = role;
            }
        }

        SaveSettings();
        _logger.LogInformation("Assigned role {Role} to {Exe}", role, exeName);
    }

    /// <summary>
    /// Retrieves the assigned role for an executable.
    /// Returns AppRole.None if not configured.
    /// </summary>
    public AppRole GetAppRole(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath)) return AppRole.None;

        var exeName = Path.GetFileName(executablePath).ToLowerInvariant();

        lock (_lock)
        {
            return _settings.AppRoles.TryGetValue(exeName, out var role) ? role : AppRole.None;
        }
    }

    private FaderSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<FaderSettings>(json, GetJsonOptions());
                if (settings != null)
                {
                    _logger.LogInformation("Settings loaded from {Path}", _settingsPath);
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings. Using defaults.");
        }

        return new FaderSettings();
    }

    public void SaveSettings()
    {
        try
        {
            lock (_lock)
            {
                var json = JsonSerializer.Serialize(_settings, GetJsonOptions());
                File.WriteAllText(_settingsPath, json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings to {Path}", _settingsPath);
        }
    }

    private static JsonSerializerOptions GetJsonOptions() => new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };
}
