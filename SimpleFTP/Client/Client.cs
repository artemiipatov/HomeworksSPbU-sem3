namespace Client;

using Exceptions;
using System.Net.Sockets;

/// <summary>
/// FTP client, that can process get and list queries.
/// </summary>
public class Client
{
    /// <summary>
    /// Gets list of files containing in the specific directory.
    /// </summary>
    /// <param name="host">The DNS name of the remote host to which you intend to connect.</param>
    /// <param name="port">The port number of the remote host to which you intend to connect.</param>
    /// <param name="pathToDirectory">Path to the needed directory.</param>
    /// <returns>
    /// Returns list of elements contained in the directory.
    /// List also consists of pairs, where first item is the name of the element,
    /// second item is a boolean value indicating, whether the element is folder or not.
    /// </returns>
    public async Task<List<(string, bool)>> ListAsync(string host, int port, string pathToDirectory)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(host, port);

        await using var networkStream = client.GetStream();
        await using var writer = new StreamWriter(networkStream);
        using var reader = new StreamReader(networkStream);

        var query = $"1 {pathToDirectory}";
        await writer.WriteLineAsync(query);
        await writer.FlushAsync();

        var response = await reader.ReadLineAsync();
        return response is null or "-1" ? new List<(string, bool)>() : ParseResponse(response);
    }

    /// <summary>
    /// Downloads specific file from the server.
    /// </summary>
    /// <param name="host">The DNS name of the remote host to which you intend to connect.</param>
    /// <param name="port">The port number of the remote host to which you intend to connect.</param>
    /// <param name="pathToFile">Path to the needed file.</param>
    /// <param name="destinationStream">Stream to which file bytes will be moved.</param>
    /// <returns>Size of downloaded file.</returns>
    /// <exception cref="DataLossException">Throws if some bytes were lost while downloading.</exception>
    public async Task<long> GetAsync(string host, int port, string pathToFile, Stream destinationStream)
    {
        var client = new TcpClient();
        await client.ConnectAsync(host, port);

        try
        {
            await using var networkStream = client.GetStream();
            await using var writer = new StreamWriter(networkStream);

            var query = $"2 {pathToFile}";
            await writer.WriteLineAsync(query);
            await writer.FlushAsync();

            var sizeInBytes = new byte[8];
            if (await networkStream.ReadAsync(sizeInBytes.AsMemory(0, 8)) != 8)
            {
                throw new DataLossException("Some bytes were lost.");
            }

            var size = BitConverter.ToInt64(sizeInBytes);
            if (size == -1)
            {
                return -1;
            }

            await CopyStream(destinationStream, networkStream, size);

            return size;
        }
        finally
        {
            client.Close();
        }
    }

    private async Task CopyStream(Stream destinationStream, NetworkStream sourceStream, long size)
    {
        var bytesLeft = size;
        var chunkSize = Math.Min(1024, bytesLeft);
        var chunkBuffer = new byte[chunkSize];

        while (bytesLeft > 0)
        {
            var readBytesCount = await sourceStream.ReadAsync(chunkBuffer, 0, (int)chunkSize);

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

    private List<(string, bool)> ParseResponse(string response)
    {
        var splitResponse = response.Split(" ");
        var numberOfElements = int.Parse(splitResponse[0]);
        var listOfElements = new List<(string, bool)>();

        for (var i = 1; i < numberOfElements * 2; i += 2)
        {
            var pair = (splitResponse[i], bool.Parse(splitResponse[i + 1]));
            listOfElements.Add(pair);
        }

        return listOfElements;
    }
}