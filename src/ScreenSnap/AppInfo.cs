using System.Reflection;

namespace ScreenSnap;

/// <summary>
/// Static, app-wide metadata surfaced in places like the About dialog. The version is
/// resolved from the assembly so it tracks whatever <c>-p:Version=</c> the build stamped in.
/// </summary>
internal static class AppInfo
{
    public const string Name = "ScreenSnap";

    public const string Author = "Alex Oswald";

    public const string Description =
        "A lightweight Windows tray utility that switches between saved display-configuration " +
        "presets \u2014 which monitors are enabled, which one is primary, and how they extend \u2014 " +
        "from a taskbar menu or a global keyboard shortcut.";

    public const string GitHubUrl = "https://github.com/alex-oswald/ScreenSnap";

    /// <summary>The app version (e.g. "1.2.3"), resolved from assembly metadata.</summary>
    public static string Version { get; } = ResolveVersion();

    private static string ResolveVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();

        // InformationalVersion carries the full "x.y.z" (plus any prerelease suffix) stamped at
        // build time; trim the "+<commit>" source-revision metadata the SDK appends.
        string? informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            int plus = informational.IndexOf('+');
            return plus >= 0 ? informational[..plus] : informational;
        }

        return assembly.GetName().Version?.ToString(3) ?? "1.0.0";
    }
}
