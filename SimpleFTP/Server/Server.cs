namespace Server;

using System.Net;
using System.Net.Sockets;

public class Server
{
    private readonly string _path;
    private readonly TcpListener _listener;

    public Server(int port)
    {
        _path = "./";
        _listener = new TcpListener(IPAddress.Any, port);
    }

    public Server(int port, string pathToTheServer)
    {
        _path = pathToTheServer;
        _listener = new TcpListener(IPAddress.Any, port);
    }

    public async Task RunAsync()
    {
        _listener.Start();

        while (true)
        {
             await Task.Run(async () => await ProcessQueriesFromSpecificSocket());
        }
    }

    private async Task ProcessQueriesFromSpecificSocket()
    {
        using var socket = await _listener.AcceptSocketAsync();
        await using var stream = new NetworkStream(socket);

        while (true)
        {
            await ProcessQuery(stream);
        }
    }

    private async Task ProcessQuery(NetworkStream stream)
    {
        var query = Array.Empty<string>();
        var reader = new StreamReader(stream);
        query = (await reader.ReadLineAsync())?.Split(" ") ?? Array.Empty<string>();

        if (query.Length != 2)
        {
            // throw new InvalidOperationException("Incorrect query.");
            return;
        }

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
                throw new InvalidOperationException("Incorrect query.");
            }
        }
    }

    private async Task ListAsync(NetworkStream stream, string path)
    {
        path = Path.Combine(_path, path);

        if (!File.Exists(path))
        {
            await stream.WriteAsync(BitConverter.GetBytes(-1L).ToArray().Reverse().ToArray());
            await stream.FlushAsync();
            return;
        }

        var files = Directory.GetFiles(path).Select(Path.GetFileName).ToArray();
        var directories = Directory.GetDirectories(path).Select(Path.GetFileName).ToArray();
        var response = (files.Length + directories.Length).ToString();

        response = files.Aggregate(response, (current, file) => current + (" " + file + " false"));
        response = directories.Aggregate(response, (current, file) => current + (" " + file + " true"));

        var writer = new StreamWriter(stream);
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