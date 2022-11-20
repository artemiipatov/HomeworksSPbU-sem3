namespace Server;

using System.Net;
using System.Net.Sockets;

public class Server : IDisposable
{
    private readonly string _path;

    private readonly TcpListener _listener;

    private readonly CancellationTokenSource _cts = new ();

    public Server(int port)
    {
        _path = ".";
        _listener = new TcpListener(IPAddress.Any, port);
    }

    public Server(int port, string pathToTheServer)
    {
        _path = pathToTheServer;
        _listener = new TcpListener(IPAddress.Any, port);
    }

    public bool IsDisposed { get; private set; }

    public async Task RunAsync()
    {
        _listener.Start();

        try
        {
            var token = _cts.Token;
            while (true)
            {
                await Task.Run(async () => await ProcessQueriesFromSpecificSocket(token), token);
            }
        }
        finally
        {
            _listener.Stop();
        }
    }

    public async Task StopAsync() => _cts.Cancel();

    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        if (!_cts.IsCancellationRequested)
        {
            StopAsync();
        }

        _cts.Dispose();
        IsDisposed = true;
    }

    private async Task ProcessQueriesFromSpecificSocket(CancellationToken token)
    {
        using var socket = await _listener.AcceptSocketAsync(token);
        await using var stream = new NetworkStream(socket);

        await ProcessQuery(stream);
    }

    private async Task ProcessQuery(NetworkStream stream)
    {
        var query = Array.Empty<string>();
        var reader = new StreamReader(stream);
        query = (await reader.ReadLineAsync())?.Split(" ") ?? Array.Empty<string>();

        if (query.Length != 2)
        {
            try
            {
                await stream.WriteAsync(BitConverter.GetBytes(-1L).Reverse().ToArray());
            }
            catch (IOException)
            {
                _cts.Cancel();
            }

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