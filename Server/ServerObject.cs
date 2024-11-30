namespace Server;

internal class ServerObject : IDisposable
{
    private readonly Dictionary<IPAddress, string> _bannedClients = [];
    private readonly List<ClientObject> _clients = [];
    private readonly TcpListener _listener;
    internal Dictionary<IPAddress, string> BannedClient => new(_bannedClients);
    
    public ServerObject()
    {
        int port;
        using (FileStream fs = new("Server.config.json", FileMode.Open))
        {
            (_, port) = IInfo.FromJson<ServerInfo>(fs);
        }

        _listener = new(IPAddress.Any, port);
    }
    
    public void Dispose()
    {
        foreach (var client in _clients)
        {
            client.Writer.WriteLine(Commands.Stop.GetCommandValue());
            client.Dispose();
        }

        _listener.Dispose();
    }

    internal async Task ListenAsync()
    {
        try
        {
            _listener.Start();
            Console.WriteLine("Server is running. Expect connections...");

            while (true)
            {
                var client = await _listener.AcceptTcpClientAsync();

                ClientObject clientObject = new(client, this);
                _clients.Add(clientObject);

                _ = clientObject.StartAsync();
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
            Dispose();
        }
    }

    internal async Task ManageAsync()
    {
        try
        {
            CommandHandler handler = new(this);
            while (true)
            {
                var command = Console.ReadLine();

                if (string.IsNullOrEmpty(command)) continue;

                await handler.HandleCommand(command.GetCommand());
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
            Dispose();
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

    public async Task KickClient(string id)
    {
        var client = _clients.FirstOrDefault(c => c.Id == id);
        if (client is null) return;

        await client.Writer.WriteLineAsync(Commands.Kick.GetCommandValue());

        RemoveConnection(client);
    }

    public async Task BanClient(string id)
    {
        var client = _clients.FirstOrDefault(c => c.Id == id);
        if (client?.Ip is null ||
            client.Nickname is null) return;

        _bannedClients.Add(client.Ip, client.Nickname);
        await client.Writer.WriteLineAsync(Commands.Ban.GetCommandValue());

        RemoveConnection(client);
    }

    public void UnbanClient(IPAddress? ipAddress)
    {
        if (ipAddress is null) return;
        _bannedClients.Remove(ipAddress);
    }

    protected internal void RemoveConnection(ClientObject client)
    {
        _clients.Remove(client);
        client.Dispose();
    }
}