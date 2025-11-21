using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Messages;
using NetSdrClientApp.Networking;
using NetSdrClientApp.Samples;
using System.Linq;

namespace NetSdrClientAppTests;

public class NetSdrClientTests
{
    NetSdrClient _client;
    Mock<ITcpClient> _tcpMock;
    Mock<IUdpClient> _updMock;
    Mock<ISampleSink> _sampleSinkMock;

    public NetSdrClientTests() { }

    [SetUp]
    public void Setup()
    {
        _tcpMock = new Mock<ITcpClient>();
        _tcpMock.Setup(tcp => tcp.Connect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
        });

        _tcpMock.Setup(tcp => tcp.Disconnect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
        });

        _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>())).Callback<byte[]>((bytes) =>
        {
            _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, bytes);
        });

        _updMock = new Mock<IUdpClient>();
        _sampleSinkMock = new Mock<ISampleSink>();

        _client = new NetSdrClient(_tcpMock.Object, _updMock.Object, _sampleSinkMock.Object);
    }

    [Test]
    public async Task ConnectAsyncTest()
    {
        //act
        await _client.ConnectAsync();

        //assert
        _tcpMock.Verify(tcp => tcp.Connect(), Times.Once);
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
    }

    [Test]
    public async Task DisconnectWithNoConnectionTest()
    {
        //act
        _client.Disconect();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task DisconnectTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        _client.Disconect();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task StartIQNoConnectionTest()
    {

        //act
        await _client.StartIQAsync();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        _tcpMock.VerifyGet(tcp => tcp.Connected, Times.AtLeastOnce);
    }

    [Test]
    public async Task StartIQTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        await _client.StartIQAsync();

        //assert
        //No exception thrown
        _updMock.Verify(udp => udp.StartListeningAsync(), Times.Once);
        Assert.That(_client.IQStarted, Is.True);
    }

    [Test]
    public async Task StopIQTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        await _client.StopIQAsync();

        //assert
        //No exception thrown
        _updMock.Verify(tcp => tcp.StopListening(), Times.Once);
        Assert.That(_client.IQStarted, Is.False);
    }

    [Test]
    public async Task ChangeFrequencyAsyncTest()
    {
        //Arrange 
        await ConnectAsyncTest();
        long frequency = 100000000; // 100 MHz
        int channel = 1;

        //act
        await _client.ChangeFrequencyAsync(frequency, channel);

        //assert
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.AtLeast(4));
    }

    [Test]
    public async Task ChangeFrequencyNoConnectionTest()
    {
        //Arrange
        long frequency = 100000000; // 100 MHz
        int channel = 1;

        //act & assert
        //Should not throw exception when not connected
        await _client.ChangeFrequencyAsync(frequency, channel);
        
        //The message should not be sent
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
    }

    [Test]
    public async Task StopIQNoConnectionTest()
    {
        //act
        await _client.StopIQAsync();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        _updMock.Verify(udp => udp.StopListening(), Times.Never);
    }

    [Test]
    public void UdpMessageReceived_StoresSamplesInSink()
    {
        var payload = new byte[] { 0x01, 0x00, 0x02, 0x00 };
        var udpMessage = BuildDataItemMessage(payload);

        _updMock.Raise(udp => udp.MessageReceived += null, _updMock.Object, udpMessage);

        _sampleSinkMock.Verify(
            sink => sink.StoreSamples(It.Is<IEnumerable<int>>(samples => samples.SequenceEqual(new[] { 1, 2 }))),
            Times.Once);
    }

    [Test]
    public void UdpMessageReceived_WithEmptyBody_DoesNotWrite()
    {
        var udpMessage = NetSdrMessageHelper.GetDataItemMessage(
            NetSdrMessageHelper.MsgTypes.DataItem0,
            BitConverter.GetBytes((ushort)1));

        _updMock.Raise(udp => udp.MessageReceived += null, _updMock.Object, udpMessage);

        _sampleSinkMock.Verify(sink => sink.StoreSamples(It.IsAny<IEnumerable<int>>()), Times.Never);
    }

    [Test]
    public void UdpMessageReceived_WithCorruptedMessage_IsIgnored()
    {
        var validMessage = BuildDataItemMessage(new byte[] { 0x01, 0x00 });
        var corrupted = validMessage.Take(validMessage.Length - 1).ToArray();

        _updMock.Raise(udp => udp.MessageReceived += null, _updMock.Object, corrupted);

        _sampleSinkMock.Verify(sink => sink.StoreSamples(It.IsAny<IEnumerable<int>>()), Times.Never);
    }

    private static byte[] BuildDataItemMessage(byte[] bodyPayload)
    {
        var parameters = BitConverter.GetBytes((ushort)42).Concat(bodyPayload).ToArray();
        return NetSdrMessageHelper.GetDataItemMessage(NetSdrMessageHelper.MsgTypes.DataItem0, parameters);
    }
}
