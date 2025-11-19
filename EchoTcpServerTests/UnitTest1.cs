using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NUnit.Framework;

namespace EchoTcpServerTests;

public class FakeUdpClientWrapper : IUdpClientWrapper
{
    public byte[]? LastDatagram { get; private set; }
    public IPEndPoint? LastEndpoint { get; private set; }
    public int SendCallCount { get; private set; }

    public int Send(byte[] datagram, int bytes, IPEndPoint endPoint)
    {
        LastDatagram = datagram[..bytes];
        LastEndpoint = endPoint;
        SendCallCount++;
        return bytes;
    }

    public void Dispose()
    {
        // Nothing to dispose in the fake
    }
}

public class EchoServerTests
{
    [Test]
    public async Task EchoServer_EchoesBackReceivedData()
    {
        // Arrange
        var server = new EchoServer(0);
        var serverTask = Task.Run(() => server.StartAsync());

        // Wait until the listener has started and a port has been assigned
        await WaitForServerToStartAsync(server, TimeSpan.FromSeconds(5));
        int port = server.ListeningPort;

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);

        var stream = client.GetStream();
        byte[] message = { 1, 2, 3, 4 };

        // Act
        await stream.WriteAsync(message, 0, message.Length);

        byte[] buffer = new byte[message.Length];
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

        server.Stop();
        await Task.WhenAny(serverTask, Task.Delay(TimeSpan.FromSeconds(5)));

        // Assert
        Assert.That(bytesRead, Is.EqualTo(message.Length));
        Assert.That(buffer, Is.EqualTo(message));
    }

    private static async Task WaitForServerToStartAsync(EchoServer server, TimeSpan timeout)
    {
        var start = DateTime.UtcNow;
        while (!server.IsRunning || server.ListeningPort == 0)
        {
            if (DateTime.UtcNow - start > timeout)
            {
                Assert.Fail("EchoServer did not start listening within the expected time.");
            }

            await Task.Delay(50);
        }
    }
}

public class UdpTimedSenderTests
{
    [Test]
    public void StartSending_Twice_ThrowsInvalidOperationException()
    {
        var sender = new UdpTimedSender("127.0.0.1", 60000, new FakeUdpClientWrapper());

        sender.StartSending(1000);

        Assert.Throws<InvalidOperationException>(() => sender.StartSending(1000));

        sender.StopSending();
        sender.Dispose();
    }

    [Test]
    public void SendMessageCallback_BuildsMessageWithExpectedStructure()
    {
        // Arrange
        var fakeClient = new FakeUdpClientWrapper();
        var sender = new UdpTimedSender("127.0.0.1", 60000, fakeClient);

        // Act - call the callback directly to avoid waiting on timers
        sender.SendMessageCallback(null!);

        // Assert
        Assert.That(fakeClient.SendCallCount, Is.EqualTo(1));
        Assert.That(fakeClient.LastDatagram, Is.Not.Null);
        Assert.That(fakeClient.LastEndpoint, Is.Not.Null);

        var datagram = fakeClient.LastDatagram!;

        // Header (2 bytes), sequence (2 bytes), payload (1024 bytes)
        Assert.That(datagram.Length, Is.EqualTo(2 + 2 + 1024));

        // Check header bytes
        Assert.That(datagram[0], Is.EqualTo(0x04));
        Assert.That(datagram[1], Is.EqualTo(0x84));

        // Sequence number should start from 1
        ushort sequence = BitConverter.ToUInt16(datagram, 2);
        Assert.That(sequence, Is.EqualTo(1));
    }
}