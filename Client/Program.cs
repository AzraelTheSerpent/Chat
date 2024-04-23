using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Chat;

internal class Program
{
    private static readonly TcpClient client = new();
    private static StreamReader? reader = null;
    private static StreamWriter? writer = null;

    static string? userName;

    public static void Main(string[] args)
    {
        string host = "127.0.0.1";
        int port = 8888;

        try
        {
            client.Connect(host, port);
            
            reader = new StreamReader(client.GetStream());
            writer = new StreamWriter(client.GetStream());

            if (reader is null || writer is null) return;
            
            do
            {   
                Console.Clear();
                Console.Write("Enter your nickname: ");
                userName = Console.ReadLine();
            } while (string.IsNullOrEmpty(userName) || string.IsNullOrWhiteSpace(userName) || userName.Equals("Admin"));
            

            Task ReceiveMsg = ReceiveMessageAsync(reader);
            Task SendMsg = SendMassageAsync(writer);

            Task.WaitAny(ReceiveMsg, SendMsg);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            client.Close();
            writer?.Close();
            reader?.Close();
        }
    }

    private static async Task SendMassageAsync(StreamWriter writer)
    {
        await writer.WriteLineAsync(userName);
        await writer.FlushAsync();

        Console.Clear();
        Console.WriteLine(new string('#', Console.WindowWidth) + $"\nWelcome, {userName}");
        
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
            await writer.FlushAsync();
            
            if (message.Equals("/exit")) break;
        }
    }

    private static async Task ReceiveMessageAsync(StreamReader reader)
    {
        while (true)
        {
            try
            {
                string? message = await reader.ReadLineAsync();

                if (string.IsNullOrEmpty(message)) continue;

                if (message[0] == '/')
                    CommandHandling(message);

                Console.WriteLine(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

    //TODO: GetCommands

    private static void CommandHandling(string message)
    {
        switch (message)
        {
            case "/stop":
                Console.WriteLine("Server was stopped");
                Exit(0);
                break;
            case "/kick":
                Console.WriteLine("You've been kicked by an admin");
                Exit(0);
                break;
        }
    }

    private static void Exit(int exitCode)
    {
        client.Close();
        writer?.Close();
        reader?.Close();

        Console.WriteLine("The app will close in: ");
        for (int i = 5; i != 0; i--)
        {
            Console.Write($"{i}...\t");
            Thread.Sleep(1000);
        }

        Environment.Exit(exitCode);
    }
}