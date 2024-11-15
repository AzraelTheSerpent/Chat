namespace Server;

internal class CommandHandler(object sender)
{
    private readonly object _sender = sender;

    public async Task HandleCommand(Commands command) 
    {
        if (_sender is ServerObject server)
            await HandleServerCommand(command, server);
        else if(_sender is ClientObject client)
            await HandleClientCommand(command, client);
    }

    private static async Task HandleClientCommand(Commands command, ClientObject client)
    {
        switch (command) 
        {
            case Commands.Exit:
                await client.Writer.WriteLineAsync(command.GetCommandValue());
                throw new Exception();
            case Commands.ClientList:
                await client.Writer.WriteLineAsync(client.GetServerClientsList());
                break;
            case Commands.CommandsList:
                await client.Writer.WriteLineAsync(command.GetCommandValue());
                break;
        };
    }

    private static async Task HandleServerCommand(Commands command, ServerObject server)
    {
        switch (command)
        {
            case Commands.CommandsList:
                HandleCommandsListCommandAsync();
                break;
            case Commands.Stop:
                throw new Exception("Server was stopped");
            case Commands.Kick:
                await HandleKickCommand(server);
                break;
            case Commands.Message:
                await HandleMessageCommand(server);
                break;
            case Commands.Ban:
                await HandleBanCommand(server);
                break;
            case Commands.Unban:
                HandleUnbanCommand(server);
                break;
            case Commands.ClientList:
                HandleConnectedClientsListCommand(server);
                break;
            case Commands.BannedClientList:
                HandleBannedClientsListCommand(server);
                break;
        };
    }

    private static void HandleCommandsListCommandAsync()
    {
        StringBuilder builder = new();
        foreach (Commands commands in Enum.GetValues(typeof(Commands)))
            builder.Append('|'+commands.GetCommandValue()+"\t\t"+commands.GetCommandAnnotation()+'\n');
        Console.WriteLine(builder.ToString());
    }

    private static void HandleConnectedClientsListCommand(ServerObject server) => Console.WriteLine(server.GetClientsList());

    private static void HandleBannedClientsListCommand(ServerObject server)
    {
        StringBuilder builder = new();

        var bannedClients = from client in server.BannedClient orderby client.Value select client;
        foreach (var bannedClient in bannedClients)
            builder.Append($"NickName:\t{bannedClient.Value}\tIp:\t{bannedClient.Key}");

        Console.WriteLine(builder.ToString());
    }

    private static async Task HandleBanCommand(ServerObject server)
    {
        HandleInput("Id of user to ban: ", out string? id);

        if (string.IsNullOrEmpty(id)) return;

        await server.BanClient(id);
    }

    private static void HandleUnbanCommand(ServerObject server)
    {
        HandleInput("IP of user to unban: ", out string? ip);

        if (string.IsNullOrEmpty(ip)) return;

        if (IPAddress.TryParse(ip, out IPAddress? address))
            server.UnbanClient(address);
    }

    private static async Task HandleMessageCommand(ServerObject server)
    {
        HandleInput("Enter the message: ", out string? message);

        if (string.IsNullOrEmpty(message)) return;

        message = $"Admin: {message}";
        await server.BroadcastMessageAsync(message);
    }

    private static async Task HandleKickCommand(ServerObject server)
    {
        HandleInput("Id of user to kick: ", out string? id);

        if (string.IsNullOrEmpty(id)) return;

        await server.KickClient(id);
    }
    private static void HandleInput(string message, out string? input)
    {
        Console.Write(message);
        input = Console.ReadLine();
    }
}
