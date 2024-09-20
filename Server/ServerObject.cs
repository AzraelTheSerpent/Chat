namespace Chat;

class ServerObject
{
    private readonly TcpListener _listener = new(IPAddress.Any, 8888);
    private readonly List<ClientObject> _clients = [];
    private readonly Dictionary<IPAddress, string> _bannedClients = [];
    internal Dictionary<IPAddress, string> BannedClient => new(_bannedClients);
    internal readonly string[] commands =
    [
        "/stop" ,
        "/kick" ,
        "/msg" ,
        "/exit",
        "/ban",
        "/unban",
        "/clist",
        "/blist"
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

                if (command.Equals(commands[7]))
                {
                    HandleBannedClientsListCommand();
                    continue;
                }
                if (command.Equals(commands[6]))
                {
                    HandleConnectedClientsListCommand();
                    continue;
                }
                if (command.Equals(commands[5]))
                {
                    HandleUnbanCommand();
                    continue;
                }
                if (command.Equals(commands[4]))
                {
                    await HandleBanCommand();
                    continue;
                }
                if (command.Equals(commands[2]))
                {
                    await HandleMessageCommand();
                    continue;
                }
                if (command.Equals(commands[1]))
                {
                    await HandleKickCommand();
                    continue;
                }
                if (command.Equals(commands[0]))
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
    internal string GetClientsList() 
    {
        StringBuilder builder = new();
        var clients = from client in _clients orderby client.Nickname, client.Id select client;

        foreach (var c in clients)
            builder.Append($"NickName:\t{c.Nickname}\tId:\t{c.Id}\n");

        return builder.ToString();
    }
    protected internal async Task BroadcastMessageAsync(string message, string? id = null)
    {
        var disconnectedClients = new List<ClientObject>();
        foreach (var client in _clients)
            try
            {
                if (client.Id != id)
                    await client.Writer.WriteLineAsync(message);
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
    private void HandleConnectedClientsListCommand() => Console.WriteLine(GetClientsList());

    private void HandleBannedClientsListCommand()
    {
        var bannedClients = from client in BannedClient orderby client.Value select client;
        foreach(var bannedClient in bannedClients)
            Console.WriteLine($"NickName:\t{bannedClient.Value}\tIp:\t{bannedClient.Key}");
    }

    private async Task HandleBanCommand() 
    {
        HandleInput("Id of user to ban: ", out string? id);

        if (string.IsNullOrEmpty(id)) return;

        await BanClient(id);
    }

    private void HandleUnbanCommand()
    {
        HandleInput("IP of user to unban: ", out string? ip);

        if (string.IsNullOrEmpty(ip)) return;

        if(IPAddress.TryParse(ip, out IPAddress? address))
            UnbanClient(address);
    }

    private void UnbanClient(IPAddress? ipAddress)
    {
        if (ipAddress is null) return;
        _bannedClients.Remove(ipAddress);
    }

    private async Task BanClient(string id)
    {
        ClientObject? client = _clients.FirstOrDefault(c => c.Id == id);
        if (client is null || 
            client.IP is null || 
            client.Nickname is null) return;

        _bannedClients.Add(client.IP, client.Nickname);
        await client.Writer.WriteLineAsync(commands[4]);

        RemoveConnection(client);
    }

    private async Task HandleMessageCommand()
    {
        HandleInput("Enter the message: ", out string? message);

        if (string.IsNullOrEmpty(message)) return;

        message = $"Admin: {message}";
        await BroadcastMessageAsync(message);
    }

    private async Task HandleKickCommand()
    {
        HandleInput("Id of user to kick: ", out string? id);

        if (string.IsNullOrEmpty(id)) return;

        await KickClient(id);
    }
    private void HandleInput(string message, out string? input)
    {
        Console.Write(message);
        input = Console.ReadLine();
    }

    private async Task Disconnect()
    {
        foreach(ClientObject client in _clients)
        {
            await client.Writer.WriteLineAsync(commands[0]);
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

        await client.Writer.WriteLineAsync(commands[1]);

        RemoveConnection(client);
    }
}