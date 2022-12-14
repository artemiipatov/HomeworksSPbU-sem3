namespace WebChat;

using System.Net.Sockets;

/// <summary>
/// Chat client.
/// </summary>
public class Client : IDisposable
{
    private readonly TcpClient _client;

    private readonly NetworkStream _networkStream;

    /// <summary>
    /// Initializes a new instance of the <see cref="Client"/> class.
    /// </summary>
    public Client(string host, int port)
    {
        _client = new TcpClient(host, port);

        _networkStream = _client.GetStream();
    }

    /// <summary>
    /// Gets a value indicating whether the client is disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Releases all resources used by the current instance of the <see cref="Client"/> class.
    /// </summary>
    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        _networkStream.Dispose();
        _client.Dispose();

        IsDisposed = true;
    }
    
    /// <summary>
    /// Runs client.
    /// </summary>
    public async Task RunAsync()
    {
        await Processing.ProcessQuery(_networkStream);
    }

}
