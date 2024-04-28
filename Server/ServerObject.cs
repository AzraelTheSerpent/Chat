namespace Chat;

class ServerObject
{
    protected TcpListener listener = new(IPAddress.Any, 8888);
    protected List<ClientObject> clients = [];
    protected internal string[] commands =
    [
        "/stop" ,
        "/kick" ,
        "/msg" ,
        "/exit" 
    ];

    protected internal async Task ListenAsync()
    {
        try
        {
            listener.Start();
            Console.WriteLine("Server is running. Expect connections...");

            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();

                ClientObject clientObject = new(client, this);
                clients.Add(clientObject);
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

    protected internal async Task ManageAsync()
    {
        try
        {
            while (true)
            {
                string? command = Console.ReadLine();

                if (string.IsNullOrEmpty(command)) continue;
                
                if (command.Equals(commands[2]))
                {
                    Console.Write("Enter the message: ");
                    string? message = Console.ReadLine();

                    if (string.IsNullOrEmpty(message)) continue;

                    message = $"Admin: {message}";
                    await BroadcastMessageAsync(message);
                    continue;
                }
                if (command.Equals(commands[1]))
                {
                    Console.Write("Id of user to kick: ");
                    string? id = Console.ReadLine();

                    if (string.IsNullOrEmpty(id)) continue;

                    await KickClient(id);
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

    protected internal async Task BroadcastMessageAsync(string message, string id)
    {
        //TODO: recall
        foreach (var client in clients)
            if (client.Id != id)
                await client.WriteLineAndFlushAsync(message);
    }

    protected internal async Task BroadcastMessageAsync(string message)
    {
        foreach (var client in clients)
            await client.WriteLineAndFlushAsync(message);
    }

    private async Task Disconnect()
    {
        foreach(ClientObject client in clients)
        {
            await client.WriteLineAndFlushAsync(commands[0]);
            client.Close();
        }  
        listener.Stop();
    }

    protected internal void RemoveConnection(string id)
    {
        ClientObject? client = clients.FirstOrDefault(c => c.Id == id);

        if (client != null) 
            clients.Remove(client);
        client?.Close();
    }
    protected internal void RemoveConnection(ClientObject client)
    {
        clients.Remove(client);
        client?.Close();
    }
    protected internal async Task KickClient(string id)
    {
        ClientObject? client = clients.FirstOrDefault(c => c.Id == id);
        if (client is null) return;

        await client.WriteLineAndFlushAsync(commands[1]);

        RemoveConnection(client);
    }
}