namespace Server;

using System.Net;
using System.Net.Sockets;

/// <summary>
/// FTP server, that can process get and list queries.
/// </summary>
public class Server : IDisposable
{
    private readonly string _path;

    private readonly TcpListener _listener;

    private readonly List<Task> _tasks = new ();

    private readonly CancellationTokenSource _cts = new ();

    /// <summary>
    /// Initializes a new instance of the <see cref="Server"/> class.
    /// </summary>
    /// <param name="port">The port that is listened for incoming connection attempts.</param>
    public Server(int port)
    {
        _path = ".";
        _listener = new TcpListener(IPAddress.Any, port);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Server"/> class.
    /// </summary>
    /// <param name="port">The port that is listened for incoming connection attempts.</param>
    /// <param name="pathToTheServer">Path to directory where the <see cref="Server"/> will be running.</param>
    public Server(int port, string pathToTheServer)
    {
        _path = pathToTheServer;
        _listener = new TcpListener(IPAddress.Any, port);
    }

    /// <summary>
    /// Gets a value indicating whether <see cref="Server"/> is disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Starts the <see cref="Server"/>.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task RunAsync()
    {
        _listener.Start();

        try
        {
            var token = _cts.Token;

            while (!token.IsCancellationRequested)
            {
                var socket = await _listener.AcceptSocketAsync(token);
                _tasks.Add(Task.Run(async () => await ProcessQueriesFromSpecificSocket(socket), token));
            }
        }
        finally
        {
            Task.WaitAll(_tasks.ToArray());
            _listener.Stop();
        }
    }

    /// <summary>
    /// Stops the <see cref="Server"/>.
    /// </summary>
    public void Stop() => _cts.Cancel();

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

    private async Task ProcessQueriesFromSpecificSocket(Socket socket)
    {
        using (socket)
        {
            await using var stream = new NetworkStream(socket);
            await ProcessQuery(stream);
        }
    }

    private async Task ProcessQuery(NetworkStream stream)
    {
        var reader = new StreamReader(stream);
        var query = (await reader.ReadLineAsync())?.Split(" ") ?? Array.Empty<string>();

        if (query.Length != 2)
        {
            await stream.WriteAsync(BitConverter.GetBytes(-1L).Reverse().ToArray());

            return;
        }

        Console.WriteLine($"Received query: {query[0]} {query[1]}");

        switch (query[0])
        {
            case "1":
            {
                await ListAsync(stream, query[1]);
                break;
            }

            case "2":
            {
                await GetAsync(stream, query[1]);
                break;
            }

            default:
            {
                await stream.WriteAsync(BitConverter.GetBytes(-1L).Reverse().ToArray());
                break;
            }
        }
    }

    private async Task ListAsync(NetworkStream stream, string path)
    {
        path = Path.Combine(_path, path);
        var writer = new StreamWriter(stream);

        if (!Directory.Exists(path))
        {
            await writer.WriteAsync((-1).ToString());
            await writer.FlushAsync();
            return;
        }

        var files = Directory.GetFiles(path).Select(Path.GetFileName).ToArray();
        var directories = Directory.GetDirectories(path).Select(Path.GetFileName).ToArray();
        var response = (files.Length + directories.Length).ToString();

        response = files.Aggregate(response, (current, file) => current + (" " + file + " false"));
        response = directories.Aggregate(response, (current, file) => current + (" " + file + " true"));

        await writer.WriteLineAsync(response);
        await writer.FlushAsync();
        await stream.FlushAsync();

        writer = null;
    }

    private async Task GetAsync(NetworkStream stream, string path)
    {
        path = Path.Combine(_path, path);

        if (!File.Exists(path))
        {
            await stream.WriteAsync(BitConverter.GetBytes(-1L).Reverse().ToArray());
            return;
        }

        var sizeInBytes = BitConverter.GetBytes(new FileInfo(path).Length);
        await stream.WriteAsync(sizeInBytes);

        await using var fileStream = new FileStream(path, FileMode.Open);
        await fileStream.CopyToAsync(stream);
        await stream.FlushAsync();
    }
}