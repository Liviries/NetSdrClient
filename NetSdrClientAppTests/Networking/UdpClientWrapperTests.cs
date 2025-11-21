using NetSdrClientApp.Networking;
using System.Net;
using System.Net.Sockets;

namespace NetSdrClientAppTests.Networking;

[TestFixture]
public class UdpClientWrapperTests
{
    [Test]
    public async Task StartListeningAsync_ReceivesMessage()
    {
        int port = GetFreeUdpPort();
        using var wrapper = new UdpClientWrapper(port);

        var receivedTcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        wrapper.MessageReceived += (_, data) => receivedTcs.TrySetResult(data);

        var listenTask = wrapper.StartListeningAsync();

        using var sender = new UdpClient();
        var payload = new byte[] { 0xAA, 0xBB, 0xCC };
        await sender.SendAsync(payload, payload.Length, new IPEndPoint(IPAddress.Loopback, port));

        var received = await receivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.That(received, Is.EqualTo(payload));

        wrapper.StopListening();
        await Task.WhenAny(listenTask, Task.Delay(1000));
    }

    [Test]
    public void Equality_DependsOnEndpoint()
    {
        var first = new UdpClientWrapper(10001);
        var same = new UdpClientWrapper(10001);
        var different = new UdpClientWrapper(10002);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(same));
            Assert.That(first.GetHashCode(), Is.EqualTo(same.GetHashCode()));
            Assert.That(first, Is.Not.EqualTo(different));
        });
    }

    private static int GetFreeUdpPort()
    {
        var listener = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        var port = ((IPEndPoint)listener.Client.LocalEndPoint!).Port;
        listener.Close();
        return port;
    }
}

