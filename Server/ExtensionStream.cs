namespace Chat;

public static class ExtensionStream
{
    public static async Task WriteLineAndFlushAsync(this StreamWriter writer, string? message)
    {
        if (message is null) return;

        await writer.WriteLineAsync(message);
        await writer.FlushAsync();
    }
    internal static async Task WriteLineAndFlushAsync(this ClientObject client, string message)
    {
        await client.Writer.WriteLineAsync(message);
        await client.Writer.FlushAsync();
    }
}