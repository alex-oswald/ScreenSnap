using Microsoft.Win32;
using ScreenSnap.Core.Abstractions;

namespace ScreenSnap.Services;

/// <summary>
/// Unpackaged autostart via the per-user <c>Run</c> key
/// (<c>HKCU\Software\Microsoft\Windows\CurrentVersion\Run</c>). A future MSIX build would
/// swap this for the Windows <c>StartupTask</c> API behind the same <see cref="IAutostartService"/>.
/// </summary>
internal sealed class RegistryAutostartService : IAutostartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ScreenSnap";

    /// <inheritdoc />
    public bool IsEnabled
    {
        get
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(ValueName) is string value &&
                   string.Equals(value.Trim('"'), ExecutablePath, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <inheritdoc />
    public void Enable()
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        // Quote the path so a space in the install location doesn't break the command line.
        key.SetValue(ValueName, $"\"{ExecutablePath}\"");
    }

    /// <inheritdoc />
    public void Disable()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        if (key?.GetValue(ValueName) is not null)
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    private static string ExecutablePath =>
        Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;
}
