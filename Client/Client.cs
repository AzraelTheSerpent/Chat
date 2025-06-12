using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using CommandsLib;
using ConfigsLib;

namespace Client;

internal class Client : IDisposable
{
    private readonly string _nickname;
    internal readonly TcpClient TcpClient = new();

    public Client(string configPath, bool start = false)
    {
        var clientInfo = GetClientInfoFromConfig(configPath);

        (_nickname, var (host, port)) = clientInfo;

        if (string.IsNullOrEmpty(_nickname)
            || string.IsNullOrWhiteSpace(_nickname)
            || _nickname.Equals("Admin"))
            throw new("Name can't take the values: null, “Admin”, empty string or consist only of spaces.");

        TcpClient.Connect(host, port);

        Writer = new(TcpClient.GetStream())
        {
            AutoFlush = true
        };
        Reader = new(TcpClient.GetStream());

        if (start) Start();
    }

    internal StreamReader Reader { get; }
    internal StreamWriter Writer { get; }

    private static ClientInfo GetClientInfoFromConfig(string configPath)
    {
        using FileStream fs = new(configPath, FileMode.OpenOrCreate);
        var clientInfo = IInfo.FromJson<ClientInfo>(fs);

        return clientInfo;
    }

    internal void Start() => Task.WaitAny(ReceiveMessageAsync(), SendMassageAsync());

    private async Task SendMassageAsync()
    {
        try
        {
            await Writer.WriteLineAsync(_nickname);

            Console.WriteLine(new string('#', Console.WindowWidth) + $"\nWelcome, {_nickname}");

            while (true)
            {
                var message = Console.ReadLine();

                if (string.IsNullOrEmpty(message) || string.IsNullOrWhiteSpace(message))
                {
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                    continue;
                }

                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.WriteLine($"You: {message}");

                await Writer.WriteLineAsync(message);
            }
        }
        catch (Exception ex)
        {
            ExceptionMessage(ex);
        }
    }

    private async Task ReceiveMessageAsync()
    {
        try
        {
            while (true)
            {
                var message = await Reader.ReadLineAsync();

                if (string.IsNullOrEmpty(message)) continue;

                if (message[0] == '/')
                    CommandHandler.CommandHandling(message.GetCommand());
                else if (OperatingSystem.IsWindows())
                {
                    var (left, top) = Console.GetCursorPosition();

                    Console.MoveBufferArea(0, top, left, 1, 0, top + 1);

                    Console.SetCursorPosition(0, top);
                    Console.WriteLine(message);

                    Console.SetCursorPosition(left, top + 1);
                }
                else
                    Console.WriteLine(message);
            }
        }
        catch (Exception ex)
        {
            ExceptionMessage(ex);
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

    public void Dispose()
    {
        TcpClient.Dispose();
        Reader.Dispose();
        Writer.Dispose();
    }
}