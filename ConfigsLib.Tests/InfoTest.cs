using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigsLib.Tests;

[TestClass]
[TestSubject(typeof(IInfo))]
public class InfoTest
{
    private const string ClientJson = """
                                      {
                                        "name": "Anon",
                                        "host": {
                                          "ipAddress": "127.0.0.1",
                                          "port": 8888
                                        }
                                      }
                                      """;
    private const string ServerJson = """
                                      {
                                        "ipAddress": "127.0.0.1",
                                        "port": 8888
                                      }
                                      """;
    private readonly ClientInfo _clientInfo = new("Anon", new("127.0.0.1", 8888));
    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true
    };
    private readonly ServerInfo _serverInfo = new("127.0.0.1", 8888);

    [TestMethod]
    public void ClientInfoToJson()
    {
        var actual = IInfo.ToJson(_clientInfo, _options);
        Assert.AreEqual(ClientJson, actual);
    }

    [TestMethod]
    public void ServerInfoToJson()
    {
        var actual = IInfo.ToJson(_serverInfo, _options);
        Assert.AreEqual(ServerJson, actual);
    }

    [TestMethod]
    public void ClientInfoFromJson()
    {
        var actual = IInfo.FromJson<ClientInfo>(ClientJson);
        Assert.AreEqual(_clientInfo, actual);
    }

    [TestMethod]
    public void ServerInfoFromJson()
    {
        var actual = IInfo.FromJson<ServerInfo>(ServerJson);
        Assert.AreEqual(_serverInfo, actual);
    }
}