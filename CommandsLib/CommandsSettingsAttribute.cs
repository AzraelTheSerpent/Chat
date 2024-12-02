using System;

namespace CommandsLib;

[AttributeUsage(AttributeTargets.Field)]
public class CommandsSettingsAttribute(string command) : Attribute
{
    public CommandsSettingsAttribute(string command, string annotation) :
        this(command) => Annotation = annotation;

    public string Value { get; } = command;
    public string? Annotation { get; }
}