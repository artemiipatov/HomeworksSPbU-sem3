namespace WebChat;

using System.Net;
using System.Net.Sockets;

/// <summary>
/// Chat server.
/// </summary>
public class Server : IDisposable
{
    private readonly TcpListener _listener;

    private readonly CancellationTokenSource _cts = new ();

    /// <summary>
    /// Initializes a new instance of the <see cref="Server"/> class.
    /// </summary>
    public Server(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
    }

    /// <summary>
    /// Gets a value indicating whether the server is disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    private void Stop() => _cts.Cancel();

    /// <summary>
    /// Releases all resources used by the current instance of the <see cref="Server"/> class.
    /// </summary>
    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        if (!_cts.IsCancellationRequested)
        {
            Stop();
        }

        _cts.Dispose();
        IsDisposed = true;
    }

    /// <summary>
    /// Runs server.
    /// </summary>
    public async Task RunAsync()
    {
        _listener.Start();

        try
        {
            var token = _cts.Token;
            using var socket = await _listener.AcceptSocketAsync(token);
            await using var stream = new NetworkStream(socket);
            await Processing.ProcessQuery(stream);
        }
        finally
        {
            _listener.Stop();
        }
    }
}
