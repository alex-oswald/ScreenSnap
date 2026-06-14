namespace ScreenSnap.Core.Displays;

/// <summary>
/// The outcome of applying a <see cref="DisplayConfiguration"/>.
/// </summary>
public sealed class DisplayApplyResult
{
    /// <summary>True when the configuration was applied successfully.</summary>
    public bool Success { get; init; }

    /// <summary>A human readable error message when <see cref="Success"/> is false.</summary>
    public string? Error { get; init; }

    /// <summary>
    /// Friendly names of monitors referenced by the preset that are not currently
    /// attached and were therefore skipped.
    /// </summary>
    public IReadOnlyList<string> MissingMonitors { get; init; } = Array.Empty<string>();
}
