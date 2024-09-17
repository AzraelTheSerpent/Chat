using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Chat;

internal class Program
{
    private static readonly TcpClient _client = new();
    private static StreamReader? _reader = null;
    private static StreamWriter? _writer = null;
    private static string? nickname;
    private static string[]? commands;

    public static async Task Main(string[] args)
    {
        string? host;
        int port = 8888;

        try
        {
            host = GetHostInput();
           
            nickname = GetNicknameInput();

            _client.Connect(host, port);

            _reader = new StreamReader(_client.GetStream());
            _writer = new StreamWriter(_client.GetStream());

            if (_reader is null || _writer is null) return;

            commands = await ReceiveCommands(_reader);

            Task ReceiveMsg = ReceiveMessageAsync(_reader);
            Task SendMsg = SendMassageAsync(_writer);

            Task.WaitAny(ReceiveMsg, SendMsg);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            Exit(0);
        }
    }
    private static string GetHostInput() 
    {
        string? host;
        do
        {
            Console.Write("Enter the host IP: ");
            host = Console.ReadLine();
            Console.Clear();
        } while (string.IsNullOrEmpty(host) || string.IsNullOrWhiteSpace(host));
        return host;
    }
    private static string GetNicknameInput() 
    {
        string? nickname;
        do
        {
            Console.Clear();
            Console.Write("Enter your nickname: ");
            nickname = Console.ReadLine();
        } while (string.IsNullOrEmpty(nickname) || string.IsNullOrWhiteSpace(nickname) || nickname.Equals("Admin"));
        return nickname;
    }

    private static async Task SendMassageAsync(StreamWriter writer)
    {
        try
        {
            await writer.WriteLineAndFlushAsync(nickname);

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

                await writer.WriteLineAndFlushAsync(message);

                if (message[0] == '/')
                    CommandHandling(message);
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
                    CommandHandling(message);
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

    private static async Task<string[]?> ReceiveCommands(StreamReader? reader)
    {
        if (reader is null) return null;
        string response = await reader.ReadLineAsync() ?? throw new Exception("Failed to receive data");
        
        string? buffer = null;
        List<string?> tmpCommands = [];
        foreach (var chr in response)
        {
            if (chr == '\\')
            {
                tmpCommands.Add(buffer);
                buffer = null;
            }
            else
                buffer += chr;
        }
        
        return [.. tmpCommands];
    }

    private static void CommandHandling(string message)
    {
        if (commands is null) return;

        if (message.Equals(commands[0]))
            Exit(0, "Server was stopped");

        if (message.Equals(commands[1]))
            Exit(0, "You've been kicked by an admin");

        if (message.Equals(commands[3]))
            Exit(0);
    }

    private static void Exit(int exitCode, string? message = null)
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