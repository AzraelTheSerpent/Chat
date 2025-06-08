using System.Text.Json.Serialization;

namespace ConfigsLib;

public record ClientInfo(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("host")] ServerInfo ServerInfo
) : IInfo;

public record ServerInfo(
    [property: JsonInclude]
    [property: JsonPropertyName("ipAddress")]
    string IpAddress,
    [property: JsonPropertyName("port")] int Port
) : IInfo;