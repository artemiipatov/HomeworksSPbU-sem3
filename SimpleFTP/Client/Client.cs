namespace Client;

using System.Net.Sockets;

public class Client
{
    private readonly TcpClient _client;

    public Client(int port, string host)
    {
        _client = new TcpClient(host, port);
    }

    public async Task RunClientAsync()
    {
        await using var stream = _client.GetStream();
        var writer = new StreamWriter(stream);

        while (true)
        {
            var query = Console.ReadLine();
            await writer.WriteLineAsync(query);
            await writer.FlushAsync();

            switch (query[0])
            {
                case '1':
                {
                    await ListAsync(stream);
                    break;
                }

                case '2':
                {
                    await GetAsync(stream);
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

    public async Task ListAsync(NetworkStream stream)
    {
        using var reader = new StreamReader(stream);
        Console.WriteLine(await reader.ReadLineAsync());
    }

    public async Task GetAsync(NetworkStream stream)
    {
        var sizeInBytes = new byte[8];
        if (await stream.ReadAsync(sizeInBytes.AsMemory(0, 8)) != 8)
        {
            throw new Exception("Some bytes were lost for some reason."); // заменить исключение
        }

        var size = BitConverter.ToInt64(sizeInBytes);
        Console.WriteLine(size);

        var fileInBytes = new byte[size];
        if (await stream.ReadAsync(fileInBytes.AsMemory(0, (int)size)) != size)
        {
            throw new Exception("Some bytes were lost for some reason."); // заменить исключение
        }

        await using var newFile = new BinaryWriter(File.Open("someData.txt", FileMode.Create));
        newFile.Write(fileInBytes);
    }
}