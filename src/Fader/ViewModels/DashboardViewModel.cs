#pragma warning disable MVVMTK0045
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fader.Core.Audio;
using Fader.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace Fader.ViewModels;

/// <summary>
/// ViewModel for the Dashboard page.
/// 
/// Exposes the list of active audio sessions to the UI and handles
/// the periodic refresh command. All observable state changes are
/// marshalled to the UI thread via DispatcherQueue.
/// </summary>
public sealed partial class DashboardViewModel : ObservableObject, IDisposable
{
    // ─── Dependencies ─────────────────────────────────────────────────────────

    private readonly AudioSessionManager _audioSessionManager;
    private readonly ILogger<DashboardViewModel> _logger;

    // ─── Observable State ─────────────────────────────────────────────────────

    /// <summary>
    /// Live list of audio sessions displayed in the dashboard card list.
    /// Bound to the ListView in DashboardPage.xaml.
    /// </summary>
    public ObservableCollection<AudioSession> Sessions { get; } = [];

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private string _statusText = "Initializing...";

    [ObservableProperty]
    private bool _isDuckingActive;

    [ObservableProperty]
    private bool _hasSessions;

    [ObservableProperty]
    private bool _isEmpty = true;

    // ─── Constructor ──────────────────────────────────────────────────────────

    public DashboardViewModel(
        AudioSessionManager audioSessionManager,
        ILogger<DashboardViewModel> logger)
    {
        _audioSessionManager = audioSessionManager;
        _logger = logger;

        // Subscribe to session manager events
        _audioSessionManager.SessionAdded += OnSessionAdded;
        _audioSessionManager.SessionRemoved += OnSessionRemoved;
        _audioSessionManager.SessionsRefreshed += OnSessionsRefreshed;
    }

    // ─── Commands ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Triggers a full refresh of the audio session list.
    /// Bound to the Refresh button in the UI.
    /// </summary>
    [RelayCommand(IncludeCancelCommand = true)]
    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        if (IsRefreshing) return;

        IsRefreshing = true;
        StatusText = "Scanning audio sessions…";

        try
        {
            await _audioSessionManager.RefreshSessionsAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Session refresh cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh sessions");
            StatusText = "Error refreshing sessions";
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    // ─── Event Handlers ───────────────────────────────────────────────────────

    private void OnSessionAdded(object? sender, AudioSession session)
    {
        // Always dispatch to UI thread — WASAPI events come on audio engine threads
        DispatchToUiThread(() =>
        {
            if (!Sessions.Any(s => s.SessionId == session.SessionId))
            {
                Sessions.Add(session);
                UpdateEmptyState();
            }
        });
    }

    private void OnSessionRemoved(object? sender, AudioSession session)
    {
        DispatchToUiThread(() =>
        {
            var existing = Sessions.FirstOrDefault(s => s.SessionId == session.SessionId);
            if (existing is not null)
            {
                Sessions.Remove(existing);
                UpdateEmptyState();
            }
        });
    }

    private void OnSessionsRefreshed(object? sender, IReadOnlyList<AudioSession> sessions)
    {
        DispatchToUiThread(() =>
        {
            // Rebuild the list on a full refresh
            Sessions.Clear();
            foreach (var session in sessions)
                Sessions.Add(session);

            UpdateEmptyState();

            StatusText = sessions.Count > 0
                ? $"Running — {sessions.Count} app{(sessions.Count == 1 ? "" : "s")} detected"
                : "Running — No active audio sessions";
        });
    }

    private void UpdateEmptyState()
    {
        HasSessions = Sessions.Count > 0;
        IsEmpty = Sessions.Count == 0;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Dispatches an action to the UI dispatcher queue.
    /// Uses Microsoft.UI.Dispatching if available, otherwise falls back to direct invocation.
    /// </summary>
    private static void DispatchToUiThread(Action action)
    {
        if (Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread() is { } queue)
        {
            queue.TryEnqueue(() => action());
        }
        else
        {
            // Already on UI thread (e.g. during testing or direct calls)
            action();
        }
    }

    // ─── IDisposable ──────────────────────────────────────────────────────────

    public void Dispose()
    {
        _audioSessionManager.SessionAdded -= OnSessionAdded;
        _audioSessionManager.SessionRemoved -= OnSessionRemoved;
        _audioSessionManager.SessionsRefreshed -= OnSessionsRefreshed;
    }
}
