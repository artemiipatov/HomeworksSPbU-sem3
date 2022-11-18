namespace Client;

using System.Net.Sockets;

public class Client
{
    private readonly TcpClient _client;

    public Client(int port, string host)
    {
        _client = new TcpClient(host, port);
    }

    public async Task RunAsync()
    {
        await using var networkStream = _client.GetStream();
        await Task.Run(async () => await Request(networkStream));
    }

    private async Task Request(NetworkStream networkStream)
    {
        var writer = new StreamWriter(networkStream);

        while (true)
        {
            var query = Console.ReadLine();
            await writer.WriteLineAsync(query);
            await writer.FlushAsync();

            switch (query[0])
            {
                case '1':
                {
                    await ListAsync(networkStream);
                    break;
                }

                case '2':
                {
                    await GetAsync(networkStream);
                    break;
                }

                default:
                {
                    Console.WriteLine("Wrong query.");
                    break;
                }
            }
        }
    }

    private async Task ListAsync(NetworkStream stream)
    {
        using var reader = new StreamReader(stream);
        Console.WriteLine(await reader.ReadLineAsync());
    }

    private async Task GetAsync(NetworkStream stream)
    {
        var sizeInBytes = new byte[8];
        if (await stream.ReadAsync(sizeInBytes.AsMemory(0, 8)) != 8)
        {
            throw new Exception("Some bytes were lost for some reason."); // заменить исключение
        }

        var size = BitConverter.ToInt64(sizeInBytes);
        Console.WriteLine(size);

        await CopyStreamBytesToFile("someData.pdf", stream, size);
    }

    private async Task CopyStreamBytesToFile(string path, NetworkStream networkStream, long size)
    {
        await using var newFile = new FileStream(path, FileMode.Create);

        var bytesLeft = size;
        var chunkSize = Math.Min(1024, bytesLeft);
        var chunkBuffer = new byte[chunkSize];

        while (bytesLeft > 0)
        {
            await networkStream.ReadAsync(chunkBuffer, 0, (int)chunkSize);
            await newFile.WriteAsync(chunkBuffer, 0, (int)chunkSize);
            await newFile.FlushAsync();
            bytesLeft -= chunkSize;
            chunkSize = Math.Min(chunkSize, bytesLeft);
        }
    }
}