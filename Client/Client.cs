using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CommandsLib;
using ConfigsLib;
using EncryptedStreamLib;

namespace Client;

internal class Client : IDisposable
{
    private readonly string _nickname;
    private readonly TcpClient _tcpClient = new();
    private readonly EncryptedStream _stream;

    private readonly string _privateKey;
    private readonly string _publicKey;
    private string _serverKey = null!;

    public Client(string configPath)
    {
        var clientInfo = GetClientInfoFromConfig(configPath);

        (_nickname, var (host, port)) = clientInfo;
        using (RSACryptoServiceProvider rsa = new())
        {
            _privateKey = rsa.ToXmlString(true);
            _publicKey = rsa.ToXmlString(false);
        }

        if (string.IsNullOrEmpty(_nickname)
            || string.IsNullOrWhiteSpace(_nickname)
            || _nickname.Equals("Admin"))
            throw new("Name can't take the values: null, “Admin”, empty string or consist only of spaces.");

        _tcpClient.Connect(host, port);
        _stream = new(_tcpClient.GetStream(), RSAEncryptionPadding.Pkcs1);
    }

    private async Task HandShake()
    {
        await _stream.WriteAsync(Encoding.UTF8.GetBytes(_nickname));
        await _stream.WriteAsync(Encoding.UTF8.GetBytes(_publicKey));

        _serverKey = Encoding.UTF8.GetString(await _stream.ReadAsync());
    }
    
    private static ClientInfo GetClientInfoFromConfig(string configPath)
    {
        using FileStream fs = new(configPath, FileMode.OpenOrCreate);
        var clientInfo = IInfo.FromJson<ClientInfo>(fs);

        return clientInfo;
    }

    internal async Task Start()
    {
        await HandShake();
        Task.WaitAny(ReceiveMessageAsync(), SendMassageAsync());
    }

    private async Task SendMassageAsync()
    {
        try
        {
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

                await _stream.EncryptedWriteAsync(message, _serverKey);
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
               var message = await _stream.DecryptedReadAsync(_privateKey);

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
        _tcpClient.Dispose();
        _stream.Dispose();
    }
}