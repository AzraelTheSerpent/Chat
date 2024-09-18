namespace Chat;

class ClientObject
{
    internal string Id {get;} = Guid.NewGuid().ToString();
    internal string? Nickname { get => _nickname; }
    internal IPAddress? IP {get; init;}
    internal StreamWriter Writer {get;}
    internal StreamReader Reader {get;}
    private string? _nickname;
    private readonly TcpClient _client;
    private readonly ServerObject _server;
    private bool clientIsLive;

    public ClientObject(TcpClient client, ServerObject server)
    {
        clientIsLive = true;

        _client = client;
        _server = server;

        if (client.Client.RemoteEndPoint is IPEndPoint endPoint)
            IP = endPoint.Address;

        var stream = client.GetStream();
        
        Writer = new StreamWriter(stream);
        Reader = new StreamReader(stream);
    }

    public async Task StartAsync()
    {
        try
        {
            await SendCommands(_server.commands);

            _nickname = await Reader.ReadLineAsync();
            string? message = $"{Nickname} join to chat";

            Print(message);
            await _server.BroadcastMessageAsync(message, Id);

            while (clientIsLive)
            {
                try
                {
                    if (IP is null) return;
                    foreach (IPAddress ip in _server.BannedClient)
                        if (IP.Equals(ip))
                        {
                            await Writer.WriteLineAndFlushAsync(_server.commands[4]);
                            throw new Exception();
                        }

                    message = await Reader.ReadLineAsync();
                    
                    if (message == null) continue;
                    if (message.Equals(_server.commands[3]))
                    {
                        await Writer.WriteLineAndFlushAsync(message);
                        throw new Exception();
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
            Console.WriteLine(ex.Message);
        }
        finally
        {
            _server.RemoveConnection(Id);
        }
    }

    private async Task SendCommands(string[] commands)
    {
        string? data = null;
        foreach (var command in commands)
            data += $"{command}\\";

        await Writer.WriteLineAndFlushAsync(data);
    }

    private void Print(string message) => Console.WriteLine($"User: {Nickname}\n" +
                                                            $"Id: {Id}\n" +
                                                            $"Message: {message}\n");

    internal void Close()
    {
        clientIsLive = false;
        Writer.Close();
        Reader.Close();
        _client.Close();
    }
}