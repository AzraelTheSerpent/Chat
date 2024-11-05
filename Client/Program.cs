using CommandsLib;
using ConfigsLib;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client;

internal class Program
{
    private static readonly TcpClient _client = new();
    private static StreamReader? _reader = null;
    private static StreamWriter? _writer = null;
    private static string? nickname;

    public static void Main(string[] args)
    {
        try
        {
            ClientInfo? clientInfo;

            using (FileStream fs = new("Client.config.json", FileMode.Open))
            {
                clientInfo = IInfo.FromJson<ClientInfo>(fs);
            }

            (nickname, string host, int port) = clientInfo;

            if (string.IsNullOrEmpty(nickname)
            || string.IsNullOrWhiteSpace(nickname)
            || nickname.Equals("Admin"))
                throw new Exception("Name can't take the values: null, “Admin”, empty string or consist only of spaces.");

            _client.Connect(host, port);

            _reader = new(_client.GetStream());
            _writer = new(_client.GetStream());

            if (_reader is null || _writer is null) return;

            Task.WaitAny(ReceiveMessageAsync(_reader), SendMassageAsync(_writer));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            Exit();
        }
    }

    private static async Task SendMassageAsync(StreamWriter writer)
    {
        try
        {
            writer.AutoFlush = true;
            await writer.WriteLineAsync(nickname);

            Console.Clear();
            Console.WriteLine(new string('#', Console.WindowWidth) + $"\nWelcome, {nickname}");
            
            while (true)
            {
                string? message = Console.ReadLine();

                if (string.IsNullOrEmpty(message) || string.IsNullOrWhiteSpace(message))
                {
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                    continue;
                }

                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.WriteLine($"You: {message}");

                await writer.WriteLineAsync(message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private static async Task ReceiveMessageAsync(StreamReader reader)
    {
        try
        {
            while (true)
            {
                string? message = await reader.ReadLineAsync();

                if (string.IsNullOrEmpty(message)) continue;

                if (message[0] == '/')
                    CommandHandling(message.GetCommand());
                else if(OperatingSystem.IsWindows())
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
            Console.WriteLine(ex.Message);
        }
    }
    
    private static void CommandHandling(Commands command)
    {
        switch (command) 
        {
            case Commands.Stop:
                Exit(message: "Server was stopped");
                break;
            case Commands.Kick:
                Exit(message: "You've been kicked by an admin");
                break;
            case Commands.Exit:
                Exit();
                break;
            case Commands.Ban:
                Exit(message: "You've been banned by an admin");
                break;
            case Commands.CommandsList:
                StringBuilder builder = new();
                foreach (Commands commands in Enum.GetValues(typeof(Commands)))
                    builder.Append('|' + commands.GetCommandValue() + "\t\t" + commands.GetCommandAnnotation() + '\n');
                Console.WriteLine(builder.ToString());
                break;
        }
    }

    private static void Exit(int exitCode = 0, string? message = null)
    {
        if (message is not null)
            Console.WriteLine(message);

        Console.WriteLine("The app will close in: ");
        for (int i = 5; i != 0; i--)
        {
            Console.Write($"{i}...\t");
            Thread.Sleep(1000);
        }
        
        _writer?.Close();
        _reader?.Close();
        _client.Close();

        Environment.Exit(exitCode);
    }
}