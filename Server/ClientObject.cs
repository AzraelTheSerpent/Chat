namespace Server;

internal class ClientObject : IDisposable
{
    private readonly TcpClient _client;
    private readonly CommandHandler _handler;
    private readonly ServerObject _server;
    private bool _clientIsLive;

    public ClientObject(TcpClient client, ServerObject server)
    {
        _clientIsLive = true;

        _client = client;
        _server = server;

        _handler = new(this);

        if (client.Client.RemoteEndPoint is IPEndPoint endPoint)
            Ip = endPoint.Address;

        var stream = client.GetStream();

        Writer = new(stream)
        {
            AutoFlush = true
        };
        Reader = new(stream);
    }

    internal string Id { get; } = Guid.NewGuid().ToString();
    internal string? Nickname { get; private set; }
    internal IPAddress? Ip { get; }
    internal StreamWriter Writer { get; }
    private StreamReader Reader { get; }

    public void Dispose()
    {
        _clientIsLive = false;
        Writer.Dispose();
        Reader.Dispose();
        _client.Dispose();
    }

    public async Task StartAsync()
    {
        try
        {
            Nickname = await Reader.ReadLineAsync();
            var message = $"{Nickname} join to chat";

            Print(message);
            await _server.BroadcastMessageAsync(message, Id);

            while (_clientIsLive)
                try
                {
                    if (Ip is null) return;
                    if (_server.BannedClient.Any(client => Ip.Equals(client.Key)))
                    {
                        await Writer.WriteLineAsync(Commands.Ban.GetCommandValue());
                        throw new();
                    }

                    message = await Reader.ReadLineAsync();

                    if (message == null) continue;
                    if (message[0] == '/')
                    {
                        await _handler.HandleCommand(message.GetCommand());
                        continue;
                    }

                    Print(message);

                    message = $"{Nickname}: {message}";
                    await _server.BroadcastMessageAsync(message, Id);
                }
                catch
                {
                    message = $"{Nickname} left the chat";

                    Print(message);

                    await _server.BroadcastMessageAsync(message, Id);
                    break;
                }
        }
        catch (Exception ex)
        {
#if DEBUG
            Console.WriteLine($"Source: {ex.Source}" +
                              $"Exception: {ex.Message}" +
                              $"Method: {ex.TargetSite}" +
                              $"StackTrace: {ex.StackTrace}");
#else
            Console.WriteLine(ex.Message);
#endif
        }
        finally
        {
            _server.RemoveConnection(this);
        }
    }

    public string GetServerClientsList() => _server.GetClientsList();

    private void Print(string message) =>
        Console.WriteLine($"User: {Nickname}\n" +
                          $"Id: {Id}\n" +
                          $"Message: {message}\n");
}