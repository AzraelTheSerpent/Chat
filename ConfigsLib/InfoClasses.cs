using System.Text.Json.Serialization;

namespace ConfigsLib;

public record ClientInfo(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("host")] ServerInfo ServerInfo
) : IInfo
{
    public void Deconstruct(out string name, out string ip, out int port)
    {
        name = Name;
        (ip, port) = ServerInfo;
    }
}

public record ServerInfo(
    [property: JsonInclude]
    [property: JsonPropertyName("ipAddress")]
    string IpAddress,
    [property: JsonPropertyName("port")] int Port
) : IInfo
{
    public void Deconstruct(out string ip, out int port)
    {
        ip = IpAddress;
        port = Port;
    }
}