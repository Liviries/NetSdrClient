using NetArchTest.Rules;
using NetSdrClientApp.Messages;
using NetSdrClientApp.Networking;
using NUnit.Framework;

namespace NetSdrClientAppTests;

[TestFixture]
public class ArchitectureTests
{
    [Test]
    public void Networking_should_not_depend_on_Messages()
    {
        var result = Types.InAssembly(typeof(TcpClientWrapper).Assembly)
            .That()
            .ResideInNamespace("NetSdrClientApp.Networking")
            .Should()
            .NotHaveDependencyOn("NetSdrClientApp.Messages")
            .GetResult();

        Assert.That(
            result.IsSuccessful,
            Is.True,
            "Networking layer (`NetSdrClientApp.Networking`) must not depend on the Messages layer (`NetSdrClientApp.Messages`).");
    }

    [Test]
    public void Messages_should_not_depend_on_Networking()
    {
        var result = Types.InAssembly(typeof(NetSdrMessageHelper).Assembly)
            .That()
            .ResideInNamespace("NetSdrClientApp.Messages")
            .Should()
            .NotHaveDependencyOn("NetSdrClientApp.Networking")
            .GetResult();

        Assert.That(
            result.IsSuccessful,
            Is.True,
            "Messages layer (`NetSdrClientApp.Messages`) must not depend on the Networking layer (`NetSdrClientApp.Networking`).");
    }
}


