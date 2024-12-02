using System;
using System.Threading;

namespace Client;

internal static class Program
{
    private static Client? _client;

    public static void Main()
    {
        try
        {
            #if DEBUG
                Console.WriteLine("DEBUG MODE");
            #endif
            _client = new("Client.config.json");
            _client.Start();
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
            Exit();
        }

    }

    internal static void Exit(int exitCode = 0, string? message = null)
    {
        if (message is not null)
            Console.WriteLine(message);

        Console.WriteLine("The app will close in: ");
        for (var i = 5; i != 0; i--)
        {
            Console.Write($"{i}...\t");
            Thread.Sleep(1000);
        }

        _client?.Writer?.Close();
        _client?.Reader?.Close();
        _client?.TcpClient.Close();

        Environment.Exit(exitCode);
    }
}