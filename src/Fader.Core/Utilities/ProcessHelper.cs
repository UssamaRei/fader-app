using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Fader.Core.Utilities;

/// <summary>
/// Provides helpers to resolve human-readable information from a Win32 process.
/// All methods gracefully handle access-denied or process-not-found scenarios.
/// </summary>
public static class ProcessHelper
{
    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a human-friendly display name for a process.
    /// Resolution order:
    ///   1. FileDescription from the executable's version info (e.g. "Spotify")
    ///   2. ProductName from version info
    ///   3. Process.MainWindowTitle
    ///   4. Executable file name without extension (e.g. "chrome")
    ///   5. "Unknown" if the process cannot be accessed
    /// </summary>
    public static string GetDisplayName(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);

            // Try version info for the most user-friendly name
            var executablePath = GetExecutablePath(processId);
            if (!string.IsNullOrEmpty(executablePath))
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(executablePath);

                if (!string.IsNullOrWhiteSpace(versionInfo.FileDescription))
                    return versionInfo.FileDescription.Trim();

                if (!string.IsNullOrWhiteSpace(versionInfo.ProductName))
                    return versionInfo.ProductName.Trim();
            }

            // Fallback: main window title
            if (!string.IsNullOrWhiteSpace(process.MainWindowTitle))
                return process.MainWindowTitle.Trim();

            // Fallback: process name (executable without extension)
            return CapitalizeName(process.ProcessName);
        }
        catch (ArgumentException)   { /* Process not found */ }
        catch (InvalidOperationException) { /* Process has exited */ }
        catch (UnauthorizedAccessException) { /* Access denied (e.g. svchost) */ }
        catch (Exception ex) when (!ex.GetType().IsSubclassOf(typeof(Exception)))
        { /* Unexpected — swallow to avoid crashing the audio monitor */ }

        return TryGetProcessNameFromId(processId) ?? "Unknown";
    }

    /// <summary>
    /// Returns the full path to the executable for a given process ID.
    /// Returns null if the path cannot be determined.
    /// </summary>
    public static string? GetExecutablePath(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);

            // MainModule can throw for system processes — use native API as fallback
            try
            {
                return process.MainModule?.FileName;
            }
            catch
            {
                return QueryFullProcessImageName(processId);
            }
        }
        catch { return null; }
    }

    // ─── Private Helpers ──────────────────────────────────────────────────────

    private static string? TryGetProcessNameFromId(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return CapitalizeName(process.ProcessName);
        }
        catch { return null; }
    }

    /// <summary>Capitalizes the first letter of a process name ("spotify" → "Spotify").</summary>
    private static string CapitalizeName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToUpperInvariant(name[0]) + name[1..];
    }

    /// <summary>
    /// Native API fallback for getting the full executable path.
    /// Required for system processes where Process.MainModule is restricted.
    /// </summary>
    private static string? QueryFullProcessImageName(int processId)
    {
        const int PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        var handle = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
        if (handle == IntPtr.Zero)
            return null;

        try
        {
            int capacity = 1024;
            var sb = new System.Text.StringBuilder(capacity);
            if (QueryFullProcessImageNameW(handle, 0, sb, ref capacity))
                return sb.ToString();
            return null;
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    // ─── P/Invoke ─────────────────────────────────────────────────────────────

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryFullProcessImageNameW(
        IntPtr hProcess,
        int dwFlags,
        System.Text.StringBuilder lpExeName,
        ref int lpdwSize);
}
