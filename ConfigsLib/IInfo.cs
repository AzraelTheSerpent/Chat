using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;

namespace ConfigsLib;

public interface IInfo
{
    public static string ToJson<T>(T info, JsonSerializerOptions? options = null)
        where T : class, IInfo
    {
        options ??= new();
        options.TypeInfoResolver = new SourceGenerationContext();

        return JsonSerializer.Serialize(info, typeof(T), options);
    }

    [RequiresUnreferencedCode("Use 'MethodFriendlyToTrimming' instead")]
    public static T FromJson<T>(string json, JsonSerializerOptions? options = null)
        where T : class, IInfo
    {
        options ??= new();
        options.TypeInfoResolver = new SourceGenerationContext();

        return JsonSerializer.Deserialize<T>(json, options) ?? throw new JsonException("Deserialization error");
    }

    [RequiresUnreferencedCode("Use 'MethodFriendlyToTrimming' instead")]
    public static T FromJson<T>(FileStream fs, JsonSerializerOptions? options = null)
        where T : class, IInfo
    {
        options ??= new();
        options.TypeInfoResolver = new SourceGenerationContext();

        return JsonSerializer.Deserialize<T>(fs, options) ?? throw new JsonException("Deserialization error");
    }
}