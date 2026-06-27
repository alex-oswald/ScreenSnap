using System.Text.Json.Serialization;

namespace ScreenSnap.Core.Presets;

/// <summary>
/// Source-generated <see cref="JsonSerializerContext"/> for <see cref="PresetsDocument"/>. Using
/// compile-time metadata (instead of reflection) is what makes preset persistence work under
/// Native AOT. The options mirror the previous reflection-based configuration exactly (indented,
/// camelCase, enums as their numeric values) so existing <c>presets.json</c> files round-trip
/// unchanged.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(PresetsDocument))]
internal sealed partial class PresetsJsonContext : JsonSerializerContext
{
}
