using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

string host = "127.0.0.1";
int port = 8888;

TcpClient client = new();
StreamReader? reader = null;
StreamWriter? writer = null;
string? userName = null;

try
{
    client.Connect(host, port);
    
    reader = new StreamReader(client.GetStream());
    writer = new StreamWriter(client.GetStream());
    if (reader is null || writer is null) return;
    
    Console.Write("Ведите свое имя: ");
    userName = Console.ReadLine();

    Task ReceiveMsg = ReceiveMessageAsync(reader);
    Task SendMsg = SendMassageAsync(writer);

    Task.WaitAny(ReceiveMsg, SendMsg);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
client.Close();
writer?.Close();
reader?.Close();
    
async Task SendMassageAsync(StreamWriter writer)
{
    await writer.WriteLineAsync(userName);
    await writer.FlushAsync();

    Console.Clear();
    Console.WriteLine(new String('#', Console.WindowWidth));
    
    while (true)
    {
        string? message = Console.ReadLine();

        if (string.IsNullOrEmpty(message)) continue;

        await writer.WriteLineAsync(message);
        await writer.FlushAsync();
        
        if (message.Equals("/exit")) break;
    }
}

async Task ReceiveMessageAsync(StreamReader reader)
{
    while (true)
    {
        try
        {
            string? message = await reader.ReadLineAsync();

            if (string.IsNullOrEmpty(message)) continue;
            Console.WriteLine(message);
        }
        catch
        {
            break;
        }
    }
}
