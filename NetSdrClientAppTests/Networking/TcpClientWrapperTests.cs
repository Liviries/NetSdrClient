using NetSdrClientApp.Networking;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetSdrClientAppTests.Networking;

[TestFixture]
public class TcpClientWrapperTests
{
    [Test]
    public async Task Connect_Send_Receive_Works()
    {
        int port = GetFreeTcpPort();
        using var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();

        using var wrapper = new TcpClientWrapper(IPAddress.Loopback.ToString(), port);
        var receivedFromServer = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        wrapper.MessageReceived += (_, data) => receivedFromServer.TrySetResult(data);

        wrapper.Connect();

        using var serverClient = await listener.AcceptTcpClientAsync();
        using NetworkStream serverStream = serverClient.GetStream();

        var outbound = new byte[] { 0x01, 0x02, 0x03 };
        await wrapper.SendMessageAsync(outbound);

        var buffer = new byte[outbound.Length];
        int bytesRead = await serverStream.ReadAsync(buffer, 0, buffer.Length);
        Assert.That(buffer.Take(bytesRead), Is.EqualTo(outbound));

        var response = new byte[] { 0x0A, 0x0B, 0x0C };
        await serverStream.WriteAsync(response);

        var received = await receivedFromServer.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.That(received, Is.EqualTo(response));

        const string textMessage = "Hello Tcp";
        await wrapper.SendMessageAsync(textMessage);
        var textBuffer = new byte[textMessage.Length];
        bytesRead = await serverStream.ReadAsync(textBuffer, 0, textBuffer.Length);
        Assert.That(Encoding.UTF8.GetString(textBuffer, 0, bytesRead), Is.EqualTo(textMessage));

        wrapper.Disconnect();
    }

    [Test]
    public void SendMessageAsync_WithoutConnection_Throws()
    {
        using var wrapper = new TcpClientWrapper(IPAddress.Loopback.ToString(), GetFreeTcpPort());

        Assert.ThrowsAsync<InvalidOperationException>(() => wrapper.SendMessageAsync(new byte[] { 0x01 }));
        Assert.ThrowsAsync<InvalidOperationException>(() => wrapper.SendMessageAsync("fail"));
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}

