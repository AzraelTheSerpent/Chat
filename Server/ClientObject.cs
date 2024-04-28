namespace Chat;

class ClientObject
{
    protected internal string Id {get;} = Guid.NewGuid().ToString();
    protected internal string? UserName;
    protected internal StreamWriter Writer {get;}
    protected internal StreamReader Reader {get;}
    private TcpClient _client;
    private ServerObject _server;

    public ClientObject(TcpClient client, ServerObject server)
    {
        _client = client;
        _server = server;

        var steam = client.GetStream();
        
        Writer = new StreamWriter(steam);
        Reader = new StreamReader(steam);
    }

    public async Task StartAsync()
    {
        try
        {
            await SendCommands(_server.commands);

            UserName = await Reader.ReadLineAsync();
            string? message = $"{UserName} join to chat";

            Print(message);
            await _server.BroadcastMessageAsync(message, Id);
            
            while (true)
            {
                try
                {
                    message = await Reader.ReadLineAsync();
                    
                    if (message == null) continue;
                    if (message.Equals(_server.commands[3])) throw new Exception();
                    
                    Print(message);
                    
                    message = $"{UserName}: {message}";
                    await _server.BroadcastMessageAsync(message, Id);
                }
                catch
                {
                    message = $"{UserName} left the chat";

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
        {
            data += command;
            data += '\\';
        }

        await Writer.WriteLineAndFlushAsync(data);
    }

    private void Print(string message) => Console.WriteLine($"User: {UserName}\n" +
                                                            $"Id: {Id}\n" +
                                                            $"Message: {message}\n");

    protected internal void Close()
    {
        Writer.Close();
        Reader.Close();
        _client.Close();
    }
}