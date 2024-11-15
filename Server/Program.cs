namespace Server;

class Program
{
    static void Main(string[] args)
    {
        ServerObject server = new();
#if DEBUG
        Console.WriteLine("DEBUG MODE");
#endif
        Task.WaitAny(server.ListenAsync(), server.ManageAsync());
    }
}