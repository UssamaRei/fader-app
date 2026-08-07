using Fader.Core.Models;

namespace Fader.Core.Services;

public interface ISettingsService
{
    FaderSettings GetSettings();
    void SetAppRole(string executablePath, AppRole role);
    AppRole GetAppRole(string executablePath);
    void SaveSettings();
}
