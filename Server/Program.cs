namespace Chat;

class Program
{
    static void Main(string[] args)
    {
        ServerObject server = new();
        
        Task listen = server.ListenAsync();
        Task manage = server.ManageAsync();
        
        Task.WaitAny(listen, manage);
    }
}