using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;


ServerObject server = new();
Task listen = server.ListenAsync();
Task manage = server.ManageAsync();

Task.WaitAny(listen, manage);

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
            await Disconnect();
        }
    }

    protected internal async Task ManageAsync()
    {
        try
        {
            while (true)
            {
                string? command = Console.ReadLine();

                if (string.IsNullOrEmpty(command)) continue;

                switch (command)
                {
                    case "/msg":
                        Console.Write("Enter the message: ");
                        string? message = Console.ReadLine();

                        if (string.IsNullOrEmpty(message)) continue;

                        message = $"Admin: {message}";
                        await BroadcastMessageAsync(message);
                        break;

                    case "/kick":
                        Console.Write("Id of user to kick: ");
                        string? id = Console.ReadLine();

                        if (string.IsNullOrEmpty(id)) continue;

                        await KickClient(id);
                        break;

                    case "/stop":
                        throw new Exception("Server was stopped");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            await Disconnect();
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

    protected internal async Task BroadcastMessageAsync(string message)
    {
        foreach (var client in clients)
        {
            await client.Writer.WriteLineAsync(message);
            await client.Writer.FlushAsync();
        }
    }

    private async Task Disconnect()
    {
        foreach(ClientObject client in clients)
        {
            await client.Writer.WriteAsync("/stop");
            await client.Writer.FlushAsync();
            client.Close();
        }  
        Listener.Stop();
    }

    protected internal void RemoveConnection(string id)
    {
        ClientObject? client = clients.FirstOrDefault(c => c.Id == id);

        if (client != null) clients.Remove(client);
        client?.Close();
    }
    protected internal void RemoveConnection(ClientObject client)
    {
        clients.Remove(client);
        client?.Close();
    }
    protected internal async Task KickClient(string id)
    {
        ClientObject? client = clients.FirstOrDefault(c => c.Id == id);
        if (client == null) return;

        await client.Writer.WriteAsync("/kick");
        await client.Writer.FlushAsync();

        RemoveConnection(client);
    }
}

class ClientObject
{
    protected internal string Id {get;} = Guid.NewGuid().ToString();
    protected internal string? UserName;
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
            UserName = await Reader.ReadLineAsync();
            string? message = $"{UserName} вошел в чат";
            
            Print(message);
            await _server.BroadcastMessageAsync(message, Id);

            while (true)
            {
                try
                {
                    message = await Reader.ReadLineAsync();
                    
                    if (message == null) continue;
                    if (message.Equals("/exit")) throw new Exception();
                    
                    Print(message);
                    
                    message = $"{UserName}: {message}";
                    await _server.BroadcastMessageAsync(message, Id);
                }
                catch
                {
                    message = $"{UserName} покинул чат";

                    Print(message);

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

    private void Print(string message) => Console.WriteLine($"User: {UserName}\nId: {Id}\nMessage: {message}\n");

    protected internal void Close()
    {
        Writer.Close();
        Reader.Close();
        _client.Close();
    }
}