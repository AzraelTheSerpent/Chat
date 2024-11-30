using System.Text.Json.Serialization;

namespace ConfigsLib;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ClientInfo))]
[JsonSerializable(typeof(ServerInfo))]
public partial class SourceGenerationContext : JsonSerializerContext;