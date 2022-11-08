namespace Server;

using System.Net;
using System.Net.Sockets;

public class Server
{
    private readonly int _port;

    public Server(int port)
    {
        _port = port;
    }

    public int Port => _port;

    public async Task RunServer()
    {
        var listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();

        while (true)
        {
            var socket = await listener.AcceptSocketAsync();
            await Task.Run(async () =>
            {
                var stream = new NetworkStream(socket);
                var reader = new StreamReader(stream);
                try
                {
                    while (true)
                    {
                        var data = await reader.ReadLineAsync();
                        // if (string.IsNullOrEmpty(data))
                        // {
                        //     Console.WriteLine("oqnronqwr");
                        //     continue;
                        // }

                        var query = data.Split(" ");
                        Console.WriteLine($"Received query: {query[0]} {query[1]}");

                        switch (query[0])
                        {
                            case "1":
                            {
                                var response = List(query[1]);
                                var writer = new StreamWriter(stream);
                                await writer.WriteAsync(response);
                                await writer.FlushAsync();
                                break;
                            }

                            case "2":
                            {
                                await GetAsync(query[1], stream);
                                break;
                            }

                            default:
                            {
                                Console.WriteLine("Wrong query");
                                socket.Close();
                                return;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                    socket.Dispose();
                }
            });
        }
    }

    public string List(string path)
    {
        var files = Directory.GetFiles(path).Select(Path.GetFileName).ToArray();
        var directories = Directory.GetDirectories(path).Select(Path.GetFileName).ToArray();
        var response = (files.Length + directories.Length).ToString();

        foreach (var file in files)
        {
            response += " " + file + " false";
        }

        foreach (var file in directories)
        {
            response += " " + file + " true";
        }

        return response;
    }

    public async Task GetAsync(string path, NetworkStream stream)
    {
        if (!File.Exists(path))
        {
            await stream.WriteAsync(BitConverter.GetBytes(-1L).ToArray().Reverse().ToArray());
            return;
        }

        var sizeInBytes = BitConverter.GetBytes(new FileInfo(path).Length).ToArray();
        await using var file = File.Open(path, FileMode.Open);
        await stream.WriteAsync(sizeInBytes);
        await file.CopyToAsync(stream);
    }
}