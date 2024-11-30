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

public record ServerInfo([property: JsonInclude] string IpAddress, int Port) : IInfo
{
    public void Deconstruct(out string ip, out int port)
    {
        ip = IpAddress;
        port = Port;
    }
}