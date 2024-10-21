using System;

namespace CommandsLib;

public enum Commands
{
    Default,
    [CommandValue("/cmd", "Show the list of commands")]
    CommandsList,
    [CommandValue("/stop", "Shutdown the server [For server admin only]")]
    Stop,
    [CommandValue("/kick", "Kick the user [For server admin only]")]
    Kick,
    [CommandValue("/msg", "Send message to users [For server admin only]")]
    Massage,
    [CommandValue("/exit", "Exit from the chat")]
    Exit,
    [CommandValue("/ban", "Ban the user [For server admin only]")]
    Ban,
    [CommandValue("/unban", "Unban the user [For server admin only]")]
    Unban,
    [CommandValue("/clist", "Show the list of users")]
    ClientList,
    [CommandValue("/blist", "Show the list of banned users [For server admin only]")]
    BannedClientList
}

[AttributeUsage(AttributeTargets.Field)]
public class CommandValueAttribute : Attribute
{
    public string Value { get; private set; }
    public string? Annotation { get; private set; } = null;
    public CommandValueAttribute(string command) => Value = command;
    public CommandValueAttribute(string command, string annotation) : 
        this(command) => Annotation = annotation;
}

public static class CommandEnumExtensions 
{
    public static string? GetCommandValue(this Commands command) 
    {
        var attributes = command.GetType()?
            .GetField(command.ToString())?
            .GetCustomAttributes(false);
        if (attributes?.Length > 0 && attributes[0] is CommandValueAttribute commandAttribute)
            return commandAttribute.Value;
        return null;
    }
    public static string? GetCommandAnnotation(this Commands command) 
    {
        var attributes = command.GetType()?
            .GetField(command.ToString())?
            .GetCustomAttributes(false);
        if (attributes?.Length > 0 && attributes[0] is CommandValueAttribute commandAttribute)
            return commandAttribute.Annotation;
        return null;
    }
    public static Commands GetCommand(this string input) 
    {
        foreach (Commands command in Enum.GetValues(typeof(Commands)))
        {
            var attributes = typeof(Commands).GetMember(command.ToString())[0].GetCustomAttributes(false);

            if (attributes.Length > 0 && attributes[0] is CommandValueAttribute commandAttribute)
            if (commandAttribute.Value.Equals(input, StringComparison.OrdinalIgnoreCase))
                return command;
        }
        return Commands.Default;
    }
}