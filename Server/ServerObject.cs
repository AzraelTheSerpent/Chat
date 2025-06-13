namespace Server;

internal class ServerObject : IDisposable
{
    private readonly Dictionary<IPAddress, string> _bannedClients = [];
    private readonly List<ClientObject> _clients = [];
    private readonly TcpListener _listener;

    public string PrivateKey { get; }
    public string PublicKey { get; }

    public ServerObject(string pathToConfigFile)
    {
        var port = GetPortFromConfig(pathToConfigFile);

        _listener = new(IPAddress.Any, port);

        using RSACryptoServiceProvider rsa = new();
        PrivateKey = rsa.ToXmlString(true);
        PublicKey = rsa.ToXmlString(false);
    }

    internal Dictionary<IPAddress, string> BannedClient => new(_bannedClients);

    public void Dispose()
    {
        foreach (var client in _clients)
        {
            client.WriteAsync(client.Encrypt(Commands.Stop.GetCommandValue())).Wait();
            client.Dispose();
        }

        _listener.Dispose();
    }

    private static int GetPortFromConfig(string pathToConfig)
    {
        using FileStream fs = new(pathToConfig, FileMode.OpenOrCreate);
        return IInfo.FromJson<ServerInfo>(fs).Port;
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
            ExceptionMessage(ex);
        }
        finally
        {
            Dispose();
        }
    }

    private static void ExceptionMessage(Exception ex)
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
            ExceptionMessage(ex);
        }
        finally
        {
            Dispose();
        }
    }

    internal string GetClientsList()
    {
        StringBuilder builder = new();
        var clients = 
            from client in _clients 
            orderby client.Nickname, client.Id 
            select client;

        foreach (var c in clients)
            builder.Append($"NickName:\t{c.Nickname}\tId:\t{c.Id}\n");

        return builder.ToString();
    }
    
    protected internal async Task BroadcastMessageAsync(string message, string? id = null)
    {
        var disconnectedClients = new ConcurrentBag<ClientObject>();
        await Parallel.ForEachAsync(_clients, async (client, _) =>
        {
            try
            {
                if (client.Id != id)
                    await client.WriteAsync(client.Encrypt(message));
            }
            catch
            {
                disconnectedClients.Add(client);
            }
        });
        
        var disconnectedClientsNickname = 
            from client in disconnectedClients select client.Nickname;
        
        foreach (var client in disconnectedClients)
            RemoveConnection(client);
            
        await Parallel.ForEachAsync(disconnectedClientsNickname, async (nickname, _) =>
        {
            await BroadcastMessageAsync($"{nickname} left the chat");
        });
    }

    public async Task KickClient(string id)
    {
        var client = _clients.FirstOrDefault(c => c.Id == id);
        if (client is null) return;

        await client.WriteAsync(client.Encrypt(Commands.Kick.GetCommandValue()));

        RemoveConnection(client);
    }

    public async Task BanClient(string id)
    {
        var client = _clients.FirstOrDefault(c => c.Id == id);
        if (client?.Ip is null ||
            client.Nickname is null) return;
        _bannedClients.Add(client.Ip, client.Nickname);
        
        await client.WriteAsync(client.Encrypt(Commands.Ban.GetCommandValue()));

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