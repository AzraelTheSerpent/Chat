using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;


ServerObject server = new();
await server.ListenAsync();

class ServerObject
{
    protected internal TcpListener Listener = new(IPAddress.Any, 8888);
    protected internal List<ClientObject> clients = new();

    protected internal async Task ListenAsync()
    {
        try
        {
            Listener.Start();
            Console.WriteLine("Сервер запущен. Ожидание подключений...");

            while (true)
            {
                TcpClient client = await Listener.AcceptTcpClientAsync();

                ClientObject clientObject = new(client, this);
                clients.Add(clientObject);
                Task tmpTask = clientObject.StartAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            Disconnect();
        }
    }

    protected internal async Task BroadcastMessageAsync(string message, string id)
    {
        foreach (var client in clients)
            if (client.Id != id)
            {
                await client.Writer.WriteLineAsync(message);
                await client.Writer.FlushAsync();
            }
    }

    private void Disconnect()
    {
        foreach(ClientObject client in clients)
            client.Close();
        Listener.Stop();
    }

    protected internal void RemoveConnection(string id)
    {
        ClientObject? client = clients.FirstOrDefault(c => c.Id == id);

        if (client != null) clients.Remove(client);
        client?.Close();
    }
}

class ClientObject
{
    protected internal string Id {get;} = Guid.NewGuid().ToString();
    protected internal StreamWriter Writer {get;}
    protected internal StreamReader Reader {get;}
    private TcpClient _client;
    private ServerObject _server;

    public ClientObject(TcpClient client, ServerObject server)
    {
        _client = client;
        _server = server;

        var steam = client.GetStream();
        
        Writer = new StreamWriter(steam);
        Reader = new StreamReader(steam);
    }

    public async Task StartAsync()
    {
        try
        {
            string? userName = await Reader.ReadLineAsync();
            string? message = $"{userName} вошел в чат";
            
            Console.WriteLine($"User: {userName}\nId: {Id}\nMessage: {message}\n");
            await _server.BroadcastMessageAsync(message, Id);

            while (true)
            {
                try
                {
                    message = await Reader.ReadLineAsync();
                    
                    if (message == null) continue;
                    if (message.Equals("/exit")) throw new Exception();
                    
                    Console.WriteLine($"User: {userName}\nId: {Id}\nMessage: {message}\n");
                    
                    message = $"{userName}: {message}";
                    await _server.BroadcastMessageAsync(message, Id);
                }
                catch
                {
                    message = $"{userName} покинул чат";

                    Console.WriteLine($"User: {userName}\nId: {Id}\nMessage: {message}\n");

                    await _server.BroadcastMessageAsync(message, Id);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            _server.RemoveConnection(Id);
        }
    }

    protected internal void Close()
    {
        Writer.Close();
        Reader.Close();
        _client.Close();
    }
}