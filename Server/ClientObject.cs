namespace Server;

internal class ClientObject : IDisposable
{
    private readonly TcpClient _client;
    private readonly CommandHandler _handler;
    private readonly ServerObject _server; 
    private readonly NetworkStream _stream;
    
    private bool _clientIsLive;
    private string _clientKey = null!;

    public ClientObject(TcpClient client, ServerObject server)
    {
        _clientIsLive = true;

        _client = client;
        _server = server;

        _handler = new(this);

        if (client.Client.RemoteEndPoint is IPEndPoint endPoint)
            Ip = endPoint.Address;

        _stream = client.GetStream();
    }

    internal string Id { get; } = Guid.NewGuid().ToString();
    internal string? Nickname { get; private set; }
    internal IPAddress? Ip { get; }

    public void Dispose()
    {
        _clientIsLive = false;
        _stream.Dispose();
        _client.Dispose();
    }

    public async Task StartAsync()
    {
        try
        {
            await HandShake();

            var message = $"{Nickname} join to chat";

            Print(message);
            await _server.BroadcastMessageAsync(message, Id);

            while (_clientIsLive)
                try
                {
                    if (Ip is null) return;
                    if (_server.BannedClient.Any(client => Ip.Equals(client.Key)))
                    {
                        await WriteAsync(Encrypt(Commands.Ban.GetCommandValue()));
                        throw new();
                    }

                    message = Decrypt(await ReadAsync());

                    if (message[0] == '/')
                    {
                        await _handler.HandleCommand(message.GetCommand());
                        continue;
                    }

                    Print(message);

                    message = $"{Nickname}: {message}";
                    await _server.BroadcastMessageAsync(message, Id);
                }
                catch
                {
                    message = $"{Nickname} left the chat";

                    Print(message);

                    await _server.BroadcastMessageAsync(message, Id);
                    break;
                }
        }
        catch (Exception ex)
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
        finally
        {
            _server.RemoveConnection(this);
        }
    }

    private async Task HandShake()
    {
        Nickname = Encoding.UTF8.GetString(await ReadAsync());
        _clientKey = Encoding.UTF8.GetString(await ReadAsync());

        var serverKeyBytes = Encoding.UTF8.GetBytes(_server.PublicKey);
        await WriteAsync(serverKeyBytes);
    }

    private async Task<byte[]> ReadAsync()
    {
        var lengthBuffer = new byte[4];
        await _stream.ReadExactlyAsync(lengthBuffer, 0, 4);
        
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

    public async Task WriteAsync(byte[] data)
    {
        var dataLength = data.Length;
        var lengthBytes = BitConverter.GetBytes(dataLength);
        
        if (BitConverter.IsLittleEndian) Array.Reverse(lengthBytes);
        
        await _stream.WriteAsync(lengthBytes.AsMemory(0, 4));
        await _stream.WriteAsync(data.AsMemory(0, 0 + dataLength));
    }

    internal byte[] Encrypt(string? data)
    {
        using RSACryptoServiceProvider rsa = new();
        rsa.FromXmlString(_clientKey);
        var dataBytes = Encoding.UTF8.GetBytes(data!);
        return rsa.Encrypt(dataBytes, RSAEncryptionPadding.Pkcs1).ToArray();
    }

    private string Decrypt(byte[] data)
    {
        using RSACryptoServiceProvider rsa = new();
        rsa.FromXmlString(_server.PrivateKey);
        var decryptedData = rsa.Decrypt(data, RSAEncryptionPadding.Pkcs1);
        return Encoding.UTF8.GetString(decryptedData);
    }

    public string GetServerClientsList() => _server.GetClientsList();

    private void Print(string message) =>
        Console.WriteLine($"User: {Nickname}\n" +
                          $"Id: {Id}\n" +
                          $"Message: {message}\n");
}