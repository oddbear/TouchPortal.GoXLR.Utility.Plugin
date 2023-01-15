using System.Text.Json;
using System.Text.Json.Serialization;
using TouchPortal.GoXLR.Utility.Plugin.Enums;

namespace TouchPortal.GoXLR.Utility.Plugin.Client;

public class Patch
{
    [JsonPropertyName("op")]
    public required OpPatchEnum Op { get; init; }

    [JsonPropertyName("path")]
    public required string Path { get; init; }

    [JsonPropertyName("value")]
    public required JsonElement Value { get; init; }
}