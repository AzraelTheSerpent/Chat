using System;
using System.Text;
using CommandsLib;

namespace Client;

internal static class CommandHandler
{
    internal static void CommandHandling(Commands command)
    {
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (command)
        {
            case Commands.Stop:
                Program.Exit(message: "Server was stopped");
                break;
            case Commands.Kick:
                Program.Exit(message: "You've been kicked by an admin");
                break;
            case Commands.Exit:
                Program.Exit();
                break;
            case Commands.Ban:
                Program.Exit(message: "You've been banned by an admin");
                break;
            case Commands.CommandsList:
                StringBuilder builder = new();
                foreach (Commands commands in Enum.GetValues(typeof(Commands)))
                    builder.Append('|' + commands.GetCommandValue() + "\t\t" + commands.GetCommandAnnotation() + '\n');
                Console.WriteLine(builder.ToString());
                break;
        }
    }
}