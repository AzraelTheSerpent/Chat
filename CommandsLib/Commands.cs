using System;

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

[AttributeUsage(AttributeTargets.Field)]
public class CommandsSettingsAttribute(string command) : Attribute
{
    public string Value { get; private set; } = command;
    public string? Annotation { get; private set; } = null;
    public CommandsSettingsAttribute(string command, string annotation) : 
        this(command) => Annotation = annotation;
}
public static class CommandEnumExtensions 
{
    public static string? GetCommandValue(this Commands command) 
    {
        var attributes = typeof(Commands)?
            .GetField(command.ToString())?
            .GetCustomAttributes(false);
        if (attributes?.Length > 0 && attributes[0] is CommandsSettingsAttribute commandAttribute)
            return commandAttribute.Value;
        return null;
    }
    public static string? GetCommandAnnotation(this Commands command) 
    {
        var attributes = typeof(Commands)?
            .GetField(command.ToString())?
            .GetCustomAttributes(false);
        if (attributes?.Length > 0 && attributes[0] is CommandsSettingsAttribute commandAttribute)
            return commandAttribute.Annotation;
        return null;
    }
    public static Commands GetCommand(this string input) 
    {
        foreach (Commands command in Enum.GetValues(typeof(Commands)))
        {
            var attributes = typeof(Commands).GetMember(command.ToString())[0].GetCustomAttributes(false);

            if (attributes.Length > 0 && attributes[0] is CommandsSettingsAttribute commandAttribute)
            if (commandAttribute.Value.Equals(input, StringComparison.OrdinalIgnoreCase))
                return command;
        }
        return Commands.Default;
    }
}