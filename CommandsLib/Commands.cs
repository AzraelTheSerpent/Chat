using System;
using System.Xml.Linq;

namespace Chat;

public enum Commands
{
    Default,
    [CommandValue("/stop")]
    Stop,
    [CommandValue("/kick")]
    Kick,
    [CommandValue("/msg")]
    Massage,
    [CommandValue("/exit")]
    Exit,
    [CommandValue("/ban")]
    Ban,
    [CommandValue("/unban")]
    Unban,
    [CommandValue("/clist")]
    ClientList,
    [CommandValue("/blist")]
    BannedClientList
}

[AttributeUsage(AttributeTargets.Field)]
public class CommandValueAttribute : Attribute
{
    public string Value { get; private set; }

    public CommandValueAttribute(string command) => Value = command;
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