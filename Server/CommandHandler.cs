using Chat;

namespace Server;

internal class CommandHandler
{
    private readonly object _sender;
    internal readonly static string[] commands =
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
    public CommandHandler(object sender) => _sender = sender;

    public async Task HandleCommand(string command) 
    {
        if (_sender is ServerObject server)
            await HandleServerCommand(command, server);
        else if(_sender is ClientObject client)
            await HandleClientCommand(command, client);
    }

    private static async Task HandleClientCommand(string command, ClientObject client)
    {
        if (command.Equals(commands[3]))
        {
            await client.Writer.WriteLineAsync(command);
            throw new Exception();
        }
        if (command.Equals(commands[6]))
        {
            await client.Writer.WriteLineAsync(client.GetServerClientsList());
            return;
        }
    }

    private static async Task HandleServerCommand(string command, ServerObject server)
    {
        if (command.Equals(commands[7]))
        {
            HandleBannedClientsListCommand(server);
            return;
        }
        if (command.Equals(commands[6]))
        {
            HandleConnectedClientsListCommand(server);
            return;
        }
        if (command.Equals(commands[5]))
        {
            HandleUnbanCommand(server);
            return;
        }
        if (command.Equals(commands[4]))
        {
            await HandleBanCommand(server);
            return;
        }
        if (command.Equals(commands[2]))
        {
            await HandleMessageCommand(server);
            return;
        }
        if (command.Equals(commands[1]))
        {
            await HandleKickCommand(server);
            return;
        }
        if (command.Equals(commands[0]))
            throw new Exception("Server was stopped");
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
