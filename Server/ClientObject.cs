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

        Stream = new(client.GetStream(), RSAEncryptionPadding.OaepSHA1);
    }

    public string ClientKey { get; private set; } = null!;

    internal string Id { get; } = Guid.NewGuid().ToString();
    internal string? Nickname { get; private set; }
    internal IPAddress? Ip { get; }
    internal EncryptedStream Stream { get; }

    public void Dispose()
    {
        _clientIsLive = false;
        Stream.Dispose();
        _client.Dispose();
    }

    public async Task StartAsync()
    {
        try
        {
            await HandShake();

            var message = $"{Nickname} join to chat";

            Print(message);
            await _server.BroadcastMessageAsync(message, Id);

            while (_clientIsLive)
                try
                {
                    if (Ip is null) return;
                    if (_server.BannedClient.Any(client => Ip.Equals(client.Key)))
                    {
                        await Stream.EncryptedWriteAsync(Commands.Ban.GetCommandValue()!, ClientKey);
                        throw new();
                    }

                    message = await Stream.DecryptedReadAsync(_server.PrivateKey);

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
            Console.WriteLine($"Source: {ex.Source}\n" +
                              $"Exception: {ex.Message}\n" +
                              $"Method: {ex.TargetSite}\n" +
                              $"StackTrace: {ex.StackTrace}\n");
        #else
            Console.WriteLine(ex.Message);
        #endif
        }
        finally
        {
            _server.RemoveConnection(this);
        }
    }

    private async Task HandShake()
    {
        Nickname = Encoding.UTF8.GetString(await Stream.ReadAsync());
        ClientKey = Encoding.UTF8.GetString(await Stream.ReadAsync());

        var serverKeyBytes = Encoding.UTF8.GetBytes(_server.PublicKey);
        await Stream.WriteAsync(serverKeyBytes);
    }

    public string GetServerClientsList() => _server.GetClientsList();

    private void Print(string message) =>
        Console.WriteLine($"User: {Nickname}\n" +
                          $"Id: {Id}\n" +
                          $"Message: {message}\n");
}