using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConfigsLib;

public record ClientInfo(string Name, ServerInfo Host) : IInfo
{
    public void Deconstruct(out string name, out string ip, out int port)
    {
        name = Name;
        (ip, port) = Host;
    }
}
public record ServerInfo(string IPAddress, int Port) : IInfo
{
    [JsonInclude]
    public string IPAddress { get; init; } = IPAddress;
    public int Port { get; init; } = Port;

    public void Deconstruct(out string ip, out int port)
    {
        ip = IPAddress;
        port = Port;
    }
}