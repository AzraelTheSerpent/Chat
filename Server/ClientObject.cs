namespace Server;

class ClientObject : IDisposable
{
    internal string Id {get;} = Guid.NewGuid().ToString();
    internal string? Nickname { get => _nickname; }
    internal IPAddress? IP {get; init;}
    internal StreamWriter Writer {get;}
    internal StreamReader Reader {get;}
    private string? _nickname;
    private readonly TcpClient _client;
    private readonly ServerObject _server;
    private readonly CommandHandler _handler;
    private bool clientIsLive;

    public ClientObject(TcpClient client, ServerObject server)
    {
        clientIsLive = true;

        _client = client;
        _server = server;

        _handler = new(this);

        if (client.Client.RemoteEndPoint is IPEndPoint endPoint)
            IP = endPoint.Address;

        var stream = client.GetStream();

        Writer = new(stream)
        {
            AutoFlush = true
        };
        Reader = new(stream);
    }

    public async Task StartAsync()
    {
        try
        {
            _nickname = await Reader.ReadLineAsync();
            string? message = $"{Nickname} join to chat";

            Print(message);
            await _server.BroadcastMessageAsync(message, Id);

            while (clientIsLive)
            {
                try
                {
                    if (IP is null) return;
                    foreach (var client in _server.BannedClient)
                        if (IP.Equals(client.Key))
                        {
                            await Writer.WriteLineAsync(Commands.Ban.GetCommandValue());
                            throw new Exception();
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
        }
        catch (Exception ex)
        {
#if DEBUG
            Console.WriteLine($"Source: {ex.Source}");
            Console.WriteLine($"Exception: {ex.Message}");
            Console.WriteLine($"Method: {ex.TargetSite}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
#else
            Console.WriteLine(ex.Message);
#endif
        }
        finally
        {
            _server.RemoveConnection(Id);
        }
    }

    public string GetServerClientsList() => _server.GetClientsList();

    private void Print(string message) => Console.WriteLine($"User: {Nickname}\n" +
                                                            $"Id: {Id}\n" +
                                                            $"Message: {message}\n");

    public void Dispose()
    {   
        clientIsLive = false;
        Writer.Dispose();
        Reader.Dispose();
        _client.Dispose();
    }
}