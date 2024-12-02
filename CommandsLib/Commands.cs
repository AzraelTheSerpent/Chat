namespace CommandsLib;

public enum Commands
{
    Default,
    [CommandsSettings("/cmd", "Show the list of commands")]
    CommandsList,
    [CommandsSettings("/stop", "Shutdown the server [For server admin only]")]
    Stop,
    [CommandsSettings("/kick", "Kick the user [For server admin only]")]
    Kick,
    [CommandsSettings("/msg", "Send message to users [For server admin only]")]
    Message,
    [CommandsSettings("/exit", "Exit from the chat")]
    Exit,
    [CommandsSettings("/ban", "Ban the user [For server admin only]")]
    Ban,
    [CommandsSettings("/unban", "Unban the user [For server admin only]")]
    Unban,
    [CommandsSettings("/clist", "Show the list of users")]
    ClientList,
    [CommandsSettings("/blist", "Show the list of banned users [For server admin only]")]
    BannedClientList
}