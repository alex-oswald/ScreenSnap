using System.Text.Json.Serialization;

namespace ScreenSnap.Core.Settings;

/// <summary>
/// Source-generated <see cref="JsonSerializerContext"/> for <see cref="AppSettings"/>. Using
/// compile-time metadata (instead of reflection) is what makes settings persistence work under
/// Native AOT. The options mirror the previous reflection-based configuration exactly
/// (indented, camelCase, enums as strings) so existing <c>settings.json</c> files round-trip
/// unchanged.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(AppSettings))]
internal sealed partial class SettingsJsonContext : JsonSerializerContext
{
}
