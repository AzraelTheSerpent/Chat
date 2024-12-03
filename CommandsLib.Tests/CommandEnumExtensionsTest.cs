using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommandsLib.Tests;

[TestClass]
[TestSubject(typeof(CommandEnumExtensions))]
public class CommandEnumExtensionsTest
{
    [TestMethod]
    [DataRow(Commands.Exit, "/exit")]
    [DataRow(Commands.Stop, "/stop")]
    [DataRow(Commands.Kick, "/kick")]
    [DataRow(Commands.Message, "/msg")]
    public void GetCommandValueTest(Commands command, string expectedValue)
    {
        var actual = command.GetCommandValue();
        Assert.AreEqual(expectedValue, actual);
    }

    [TestMethod]
    [DataRow(Commands.Exit, "Exit from the chat")]
    [DataRow(Commands.ClientList, "Show the list of users")]
    [DataRow(Commands.CommandsList, "Show the list of commands")]
    [DataRow(Commands.Stop, "Shutdown the server [For server admin only]")]
    public void GetCommandAnnotationTest(Commands command, string expectedValue)
    {
        var actual = command.GetCommandAnnotation();
        Assert.AreEqual(expectedValue, actual);
    }

    [TestMethod]
    [DataRow("/exit", Commands.Exit)]
    [DataRow("/stop", Commands.Stop)]
    [DataRow("/kick", Commands.Kick)]
    [DataRow("/msg", Commands.Message)]
    public void GetCommandTest(string input, Commands expected)
    {
        var actual = input.GetCommand();
        Assert.AreEqual(expected, actual);
    }
}