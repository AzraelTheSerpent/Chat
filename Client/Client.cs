using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CommandsLib;
using ConfigsLib;

namespace Client;

internal class Client : IDisposable
{
    private readonly string _nickname;
    private readonly TcpClient _tcpClient = new();
    private readonly NetworkStream _stream;

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
        _stream = _tcpClient.GetStream();
    }

    private async Task HandShake()
    {
        await WriteAsync(Encoding.UTF8.GetBytes(_nickname));
        await WriteAsync(Encoding.UTF8.GetBytes(_publicKey));

        _serverKey = Encoding.UTF8.GetString(await ReadAsync());
    }
    private async Task WriteAsync(byte[] data)
    {
        var dataLength = BitConverter.GetBytes(data.Length);
            
        if (BitConverter.IsLittleEndian) Array.Reverse(dataLength);

        await _stream.WriteAsync(dataLength.AsMemory(0, 4));
        await _stream.WriteAsync(data.AsMemory(0, 0 + data.Length));
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

                await WriteAsync(Encrypt(message));
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
                var responseData = await ReadAsync();

                var message = Decrypt(responseData);

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

    private async Task<byte[]> ReadAsync()
    {
        var lengthBuffer = new byte[4];
        try
        {
            await _stream.ReadExactlyAsync(lengthBuffer, 0, 4);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading length: {ex.Message}");
        }
        
        if (BitConverter.IsLittleEndian) Array.Reverse(lengthBuffer);
                
        var messageLength = BitConverter.ToInt32(lengthBuffer, 0);

        var responseData = new byte[messageLength];
        var bytesRead = 0;
        while (bytesRead < messageLength)
            bytesRead += await _stream.ReadAsync(responseData.AsMemory(
                bytesRead,
                messageLength - bytesRead)
            );
        return responseData;
    }

    private byte[] Encrypt(string data)
    {
        using RSACryptoServiceProvider rsa = new();
        rsa.FromXmlString(_serverKey);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        return rsa.Encrypt(dataBytes, RSAEncryptionPadding.Pkcs1).ToArray();
    }

    private string Decrypt(byte[] data)
    {
        using RSACryptoServiceProvider rsa = new();
        rsa.FromXmlString(_privateKey);
        var decryptedData = rsa.Decrypt(data, RSAEncryptionPadding.Pkcs1);
        return Encoding.UTF8.GetString(decryptedData);
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