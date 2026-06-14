namespace ScreenSnap.Core.Abstractions;

/// <summary>
/// Controls whether ScreenSnap launches automatically when the user signs in.
/// Implementations differ between unpackaged (registry Run key) and packaged
/// (Windows <c>StartupTask</c>) deployments, so callers depend on this abstraction.
/// </summary>
public interface IAutostartService
{
    bool IsEnabled { get; }

    void Enable();

    void Disable();
}
