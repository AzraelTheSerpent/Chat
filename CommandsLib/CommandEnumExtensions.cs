using System;

namespace CommandsLib;

public static class CommandEnumExtensions
{
    public static string? GetCommandValue(this Commands command)
    {
        var attributes = typeof(Commands)
            .GetField(command.ToString())?
            .GetCustomAttributes(false);
        if (attributes?.Length > 0 && attributes[0] is CommandsSettingsAttribute commandAttribute)
            return commandAttribute.Value;
        return null;
    }

    public static string? GetCommandAnnotation(this Commands command)
    {
        var attributes = typeof(Commands)
            .GetField(command.ToString())?
            .GetCustomAttributes(false);
        if (attributes?.Length > 0 && attributes[0] is CommandsSettingsAttribute commandAttribute)
            return commandAttribute.Annotation;
        return null;
    }

    public static Commands GetCommand(this string input)
    {
        foreach (var command in Enum.GetValues<Commands>())
        {
            var attributes = typeof(Commands).GetMember(command.ToString())[0].GetCustomAttributes(false);

            if (attributes.Length <= 0 || attributes[0] is not CommandsSettingsAttribute commandAttribute) continue;
            if (commandAttribute.Value.Equals(input, StringComparison.OrdinalIgnoreCase))
                return command;
        }

        return Commands.Default;
    }
}