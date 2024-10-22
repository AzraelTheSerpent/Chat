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
};
public record ServerInfo : IInfo
{
    [JsonInclude]
    public string IPAddress { get; init; }
    public int Port { get; init; }

    [JsonConstructor]
    public ServerInfo(string IPAddress, int Port)
    {
        this.IPAddress = IPAddress;
        this.Port = Port;
    }
    public void Deconstruct(out string ip, out int port)
    {
        ip = IPAddress;
        port = Port;
    }
}
public interface IInfo
{
    public static string ToJson<T>(T info, JsonSerializerOptions? options = null) 
        where T : class, IInfo
    {
        options ??= new();
        options.TypeInfoResolver = new SourceGenerationContext();

        return JsonSerializer.Serialize(info, typeof(T), options);
    }

    public static T FromJson<T>(string json, JsonSerializerOptions? options = null)
        where T : class, IInfo
    {
        options ??= new();
        options.TypeInfoResolver = new SourceGenerationContext();

        return JsonSerializer.Deserialize(json, typeof(T), options) as T ?? throw new JsonException("Deserialization error");
    }

    public static T FromJson<T>(FileStream fs, JsonSerializerOptions? options = null)
        where T : class, IInfo
    {
        options ??= new();
        options.TypeInfoResolver = new SourceGenerationContext();

        return JsonSerializer.Deserialize(fs, typeof(T), options) as T ?? throw new JsonException("Deserialization error");
    }
}