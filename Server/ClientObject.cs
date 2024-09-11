namespace Chat;

class ClientObject
{
    internal string Id {get;} = Guid.NewGuid().ToString();
    internal string? Nickname;
    internal StreamWriter Writer {get;}
    internal StreamReader Reader {get;}
    private readonly TcpClient _client;
    private readonly ServerObject _server;
    private bool clientIsLive;

    public ClientObject(TcpClient client, ServerObject server)
    {
        clientIsLive = true;

        _client = client;
        _server = server;

        var stream = client.GetStream();
        
        Writer = new StreamWriter(stream);
        Reader = new StreamReader(stream);
    }

    public async Task StartAsync()
    {
        try
        {
            await SendCommands(_server._commands);

            Nickname = await Reader.ReadLineAsync();
            string? message = $"{Nickname} join to chat";

            Print(message);
            await _server.BroadcastMessageAsync(message, Id);
            
            while (clientIsLive)
            {
                try
                {
                    message = await Reader.ReadLineAsync();
                    
                    if (message == null) continue;
                    if (message.Equals(_server._commands[3])) throw new Exception();
                    
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