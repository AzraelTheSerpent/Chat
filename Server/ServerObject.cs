namespace Chat;

class ServerObject
{
    private TcpListener _listener = new(IPAddress.Any, 8888);
    private List<ClientObject> _clients = [];
    internal readonly string[] _commands =
    [
        "/stop" ,
        "/kick" ,
        "/msg" ,
        "/exit" 
    ];

    internal async Task ListenAsync()
    {
        try
        {
            _listener.Start();
            Console.WriteLine("Server is running. Expect connections...");

            while (true)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync();

                ClientObject clientObject = new(client, this);
                _clients.Add(clientObject);
                Task tmpTask = clientObject.StartAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            await Disconnect();
        }
    }

    internal async Task ManageAsync()
    {
        try
        {
            while (true)
            {
                string? command = Console.ReadLine();

                if (string.IsNullOrEmpty(command)) continue;
                
                if (command.Equals(_commands[2]))
                {
                    await HandleMessageCommand();
                    continue;
                }
                if (command.Equals(_commands[1]))
                {
                    await HandleKickCommand();
                    continue;
                }
                if (command.Equals(_commands[0]))
                    throw new Exception("Server was stopped");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            await Disconnect();
        }
    }
    private async Task HandleMessageCommand()
    {
        string? message;
        HandleInput("Enter the message: ", out message);

        if (string.IsNullOrEmpty(message)) return;

        message = $"Admin: {message}";
        await BroadcastMessageAsync(message);
    }

    private async Task HandleKickCommand()
    {
        string? id;
        HandleInput("Id of user to kick: ", out id);

        if (string.IsNullOrEmpty(id)) return;

        await KickClient(id);
    }
    private void HandleInput(string message, out string? input)
    {
        Console.Write(message);
        input = Console.ReadLine();
    }

    protected internal async Task BroadcastMessageAsync(string message, string? id = null)
    {
        var disconnectedClients = new List<ClientObject>();
        foreach (var client in _clients)
            try
            {
                if (client.Id != id)
                    await client.Writer.WriteLineAndFlushAsync(message);
            }
            catch
            {
                disconnectedClients.Add(client);
            }

        foreach (var client in disconnectedClients)
        {
            _clients.Remove(client);
            var nickname = client.Nickname;
            RemoveConnection(client);
            await BroadcastMessageAsync($"{nickname} left the chat");
        }
    }

    private async Task Disconnect()
    {
        foreach(ClientObject client in _clients)
        {
            await client.Writer.WriteLineAndFlushAsync(_commands[0]);
            client.Close();
        }  
        _listener.Stop();
    }

    protected internal void RemoveConnection(string id)
    {
        ClientObject? client = _clients.FirstOrDefault(c => c.Id == id);

        if (client is not null) 
            _clients.Remove(client);
        client?.Close();
    }
    protected internal void RemoveConnection(ClientObject client)
    {
        _clients.Remove(client);
        client?.Close();
    }
    private async Task KickClient(string id)
    {
        ClientObject? client = _clients.FirstOrDefault(c => c.Id == id);
        if (client is null) return;

        await client.Writer.WriteLineAndFlushAsync(_commands[1]);

        RemoveConnection(client);
    }
}