namespace Client;

using System.Net.Sockets;

public class Client : IDisposable
{
    private readonly TcpClient _client;

    private readonly NetworkStream _networkStream;
    private readonly StreamWriter _writer;
    private readonly StreamReader _reader;

    public Client(int port, string host)
    {
        _client = new TcpClient(host, port);

        _networkStream = _client.GetStream();
        _writer = new StreamWriter(_networkStream);
        _reader = new StreamReader(_networkStream);
    }

    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        _reader.Dispose();
        _writer.Dispose();
        _networkStream.Dispose();
        _client.Dispose();

        IsDisposed = true;
    }

    public async Task<(int, List<(string, bool)>)> ListAsync(string pathToDirectory)
    {
        var query = $"1 {pathToDirectory}";
        await _writer.WriteLineAsync(query);
        await _writer.FlushAsync();

        var response = await _reader.ReadLineAsync();

        return response is null or "-1" ? (-1, new List<(string, bool)>()) : ParseResponse(response);
    }

    public async Task<long> GetAsync(string pathToFile, Stream destinationStream)
    {
        var query = $"2 {pathToFile}";
        await _writer.WriteLineAsync(query);
        await _writer.FlushAsync();

        var sizeInBytes = new byte[8];
        if (await _networkStream.ReadAsync(sizeInBytes.AsMemory(0, 8)) != 8)
        {
            throw new DataLossException("Some bytes were lost.");
        }

        var size = BitConverter.ToInt64(sizeInBytes);
        if (size == -1)
        {
            // throw new FileNotFoundException("File with given path does not exist.");
            return -1;
        }

        await CopyStream(destinationStream, size);
        return size;
    }

    private async Task CopyStream(Stream destinationStream, long size)
    {
        var bytesLeft = size;
        var chunkSize = Math.Min(1024, bytesLeft);
        var chunkBuffer = new byte[chunkSize];

        while (bytesLeft > 0)
        {
            await _networkStream.ReadAsync(chunkBuffer, 0, (int)chunkSize);
            await destinationStream.WriteAsync(chunkBuffer, 0, (int)chunkSize);
            await destinationStream.FlushAsync();

            bytesLeft -= chunkSize;
            chunkSize = Math.Min(chunkSize, bytesLeft);
        }
    }

    private (int, List<(string, bool)>) ParseResponse(string response)
    {
        var splitResponse = response.Split(" ");
        var numberOfElements = int.Parse(splitResponse[0]);
        var listOfElements = new List<(string, bool)>();

        for (var i = 1; i < numberOfElements * 2; i += 2)
        {
            var pair = (splitResponse[i], bool.Parse(splitResponse[i + 1]));
            listOfElements.Add(pair);
        }

        return (numberOfElements, listOfElements);
    }
}