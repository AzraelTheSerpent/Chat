namespace Chat;

class Program
{
    static void Main(string[] args)
    {
        ServerObject server = new();
        Task.WaitAny(server.ListenAsync(), server.ManageAsync());
    }
}