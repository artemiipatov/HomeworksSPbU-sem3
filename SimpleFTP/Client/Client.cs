namespace Client;

using Exceptions;
using System.Net.Sockets;

/// <summary>
/// FTP client, that can process get and list queries.
/// </summary>
public class Client : IDisposable
{
    private readonly TcpClient _client;

    private readonly NetworkStream _networkStream;
    private readonly StreamWriter _writer;
    private readonly StreamReader _reader;

    /// <summary>
    /// Initializes a new instance of the <see cref="Client"/> class.
    /// </summary>
    /// <param name="port">The port number of the remote host to which you intend to connect.</param>
    /// <param name="host">The DNS name of the remote host to which you intend to connect.</param>
    public Client(int port, string host)
    {
        _client = new TcpClient(host, port);

        _networkStream = _client.GetStream();
        _writer = new StreamWriter(_networkStream);
        _reader = new StreamReader(_networkStream);
    }

    /// <summary>
    /// Gets a value indicating whether <see cref="Client"/> is disposed.
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

        _reader.Dispose();
        _writer.Dispose();
        _networkStream.Dispose();
        _client.Dispose();

        IsDisposed = true;
    }

    /// <summary>
    /// Gets list of files containing in the specific directory.
    /// </summary>
    /// <param name="pathToDirectory">Path to the needed directory.</param>
    /// <returns>
    /// Returns pair where first item is the number of elements contained in the directory,
    /// second item is the list of elements contained in the directory.
    /// List also consists of pairs, where first item is the name of the element,
    /// second item is a boolean value indicating, whether the element is folder or not.
    /// </returns>
    public async Task<(int, List<(string, bool)>)> ListAsync(string pathToDirectory)
    {
        var query = $"1 {pathToDirectory}";
        await _writer.WriteLineAsync(query);
        await _writer.FlushAsync();

        var response = await _reader.ReadLineAsync();

        return response is null or "-1" ? (-1, new List<(string, bool)>()) : ParseResponse(response);
    }

    /// <summary>
    /// Downloads specific file from the server.
    /// </summary>
    /// <param name="pathToFile">Path to the needed file.</param>
    /// <param name="destinationStream">Stream to which file bytes will be moved.</param>
    /// <returns>Size of downloaded file.</returns>
    /// <exception cref="DataLossException">Throws if some bytes were lost while downloading.</exception>
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
            var readBytesCount = await _networkStream.ReadAsync(chunkBuffer, 0, (int)chunkSize);

            if (readBytesCount != chunkSize)
            {
                throw new DataLossException("Data loss during transmission and reception.");
            }

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