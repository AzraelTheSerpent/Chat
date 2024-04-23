using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Chat;

class ServerObject
{
    protected TcpListener Listener = new(IPAddress.Any, 8888);
    protected List<ClientObject> clients = new();

    protected internal async Task ListenAsync()
    {
        try
        {
            Listener.Start();
            Console.WriteLine("Server is running. Expect connections...");

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
                //TODO: enum commands
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
        //TODO: recall

        foreach (var client in clients)
            if (client.Id != id)
            {
                //TODO: WriteAndFlushAsync
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