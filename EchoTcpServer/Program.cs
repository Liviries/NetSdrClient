using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("EchoTcpServerTests")]

namespace EchoTcpServer
{
    /// <summary>
    /// This program was designed for test purposes only
    /// Not for a review
    /// </summary>
    public sealed class EchoServer : IDisposable
    {
        private readonly int _port;
        private TcpListener? _listener;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private bool _disposed;

        public EchoServer(int port)
        {
            _port = port;
        }

        public bool IsRunning => _listener != null;

        public int ListeningPort
        {
            get
            {
                if (_listener?.LocalEndpoint is IPEndPoint endPoint)
                {
                    return endPoint.Port;
                }

                return 0;
            }
        }

        public async Task StartAsync()
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            Console.WriteLine($"Server started on port {_port}.");

            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    Console.WriteLine("Client connected.");

                    _ = Task.Run(() => HandleClientAsync(client, _cancellationTokenSource.Token));
                }
                catch (ObjectDisposedException)
                {
                    // Listener has been closed
                    break;
                }
            }

            Console.WriteLine("Server shutdown.");
        }

        private static async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using (NetworkStream stream = client.GetStream())
            {
                try
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead;

                    while (!token.IsCancellationRequested &&
                           (bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), token)) > 0)
                    {
                        // Echo back the received message
                        await stream.WriteAsync(buffer.AsMemory(0, bytesRead), token);
                        Console.WriteLine($"Echoed {bytesRead} bytes to the client.");
                    }
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
                finally
                {
                    client.Close();
                    Console.WriteLine("Client disconnected.");
                }
            }
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _listener?.Stop();
            _listener = null;
            Console.WriteLine("Server stopped.");
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Stop();
            _cancellationTokenSource.Dispose();

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    internal static class Program
    {
        public static async Task Main(string[] args)
        {
            using var server = new EchoServer(5000);

            var serverTask = server.StartAsync();

            string host = "127.0.0.1"; // Target IP
            int port = 60000;          // Target Port
            int intervalMilliseconds = 5000; // Send every 3 seconds

            using (var sender = new UdpTimedSender(host, port))
            {
                Console.WriteLine("Press any key to stop sending...");
                sender.StartSending(intervalMilliseconds);

                Console.WriteLine("Press 'q' to quit...");
                while (Console.ReadKey(intercept: true).Key != ConsoleKey.Q)
                {
                    // Just wait until 'q' is pressed
                }

                sender.StopSending();
                server.Stop();
                await serverTask;
                Console.WriteLine("Sender stopped.");
            }
        }
    }


    public class UdpTimedSender : IDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private readonly IUdpClientWrapper _udpClient;
        private Timer? _timer;
        private bool _disposed;
        private ushort _sequence;

        public UdpTimedSender(string host, int port)
            : this(host, port, new UdpClientWrapper())
        {
        }

        public UdpTimedSender(string host, int port, IUdpClientWrapper udpClient)
        {
            _host = host;
            _port = port;
            _udpClient = udpClient;
        }

        public void StartSending(int intervalMilliseconds)
        {
            if (_timer != null)
                throw new InvalidOperationException("Sender is already running.");

            _timer = new Timer(SendMessageCallback, null, 0, intervalMilliseconds);
        }

        internal void SendMessageCallback(object? state)
        {
            try
            {
                //dummy data
                byte[] samples = new byte[1024];
                RandomNumberGenerator.Fill(samples);
                _sequence++;

                byte[] msg = (new byte[] { 0x04, 0x84 }).Concat(BitConverter.GetBytes(_sequence)).Concat(samples).ToArray();
                var endpoint = new IPEndPoint(IPAddress.Parse(_host), _port);

                _udpClient.Send(msg, msg.Length, endpoint);
                Console.WriteLine($"Message sent to {_host}:{_port} ");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
            }
        }

        public void StopSending()
        {
            _timer?.Dispose();
            _timer = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    StopSending();
                    _udpClient.Dispose();
                }

                _disposed = true;
            }
        }
    }

    public interface IUdpClientWrapper : IDisposable
    {
        int Send(byte[] datagram, int bytes, IPEndPoint endPoint);
    }

    public sealed class UdpClientWrapper : IUdpClientWrapper
    {
        private readonly UdpClient _udpClient = new UdpClient();

        public int Send(byte[] datagram, int bytes, IPEndPoint endPoint)
        {
            return _udpClient.Send(datagram, bytes, endPoint);
        }

        public void Dispose()
        {
            _udpClient.Dispose();
        }
    }
}