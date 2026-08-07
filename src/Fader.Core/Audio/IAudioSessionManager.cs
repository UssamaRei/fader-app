using Fader.Core.Models;

namespace Fader.Core.Audio;

public interface IAudioSessionManager
{
    event EventHandler<AudioSession>? SessionAdded;
    event EventHandler<AudioSession>? SessionRemoved;
    event EventHandler<IReadOnlyList<AudioSession>>? SessionsRefreshed;

    IReadOnlyList<AudioSession> GetSessions();
    Task RefreshSessionsAsync(CancellationToken cancellationToken = default);
    float? GetVolume(string sessionId);
    bool SetVolume(string sessionId, float volume);
}
